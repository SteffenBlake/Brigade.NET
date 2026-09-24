using System.Data;

namespace Brigade.Net.Mise;

/// <summary>Builds one SQL write command. Instances are not safe to mutate concurrently.</summary>
public class CommandBuilder : ICommandBuilder
{
    private readonly List<Action<SqlRenderContext>> _assignments = [];
    private readonly List<FormattableString> _predicates = [];
    private readonly List<FormattableString> _values = [];
    private readonly List<SqlParameterSpec> _procedureParameters = [];
    private FormattableString? _target;
    private FormattableString? _columns;
    private FormattableString? _custom;
    private string? _procedureName;
    private IQueryBuilder? _source;
    private string? _kind;
    private int? _timeout;

    /// <summary>Creates a command for the default ANSI dialect.</summary>
    public CommandBuilder() : this(new AnsiSqlDialect())
    {
    }

    /// <summary>Creates a command for an engine dialect.</summary>
    public CommandBuilder(SqlDialect dialect)
    {
        Dialect = dialect ?? throw new ArgumentNullException(nameof(dialect));
    }

    /// <summary>Gets the engine dialect selected for this command.</summary>
    public SqlDialect Dialect { get; }

    /// <summary>Gets the selected command kind for engine validation.</summary>
    protected string? Kind => _kind;

    /// <summary>Starts an INSERT into one table.</summary>
    public CommandBuilder InsertInto(FormattableString target) => SetTarget("INSERT INTO", target);

    /// <summary>Starts an UPDATE of one table.</summary>
    public CommandBuilder Update(FormattableString target) => SetTarget("UPDATE", target);

    /// <summary>Starts a DELETE from one table.</summary>
    public CommandBuilder DeleteFrom(FormattableString target) => SetTarget("DELETE FROM", target);

    /// <summary>Adds a SET assignment to an UPDATE.</summary>
    public CommandBuilder Set(FormattableString assignment)
    {
        _assignments.Add(context => context.Fragment(assignment));
        return this;
    }

    /// <summary>Adds a scalar subquery assignment to an UPDATE.</summary>
    public CommandBuilder Set(string column, IQueryBuilder query)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(column);
        _assignments.Add(context =>
        {
            context.Text.Append(Dialect.QuoteIdentifier(column)).Append(" = (");
            RenderChild(context, query);
            context.Text.Append(')');
        });
        return this;
    }

    /// <summary>Adds an AND predicate to an UPDATE or DELETE.</summary>
    public CommandBuilder Where(FormattableString predicate)
    {
        _predicates.Add(predicate);
        return this;
    }

    /// <summary>Uses a read query as the INSERT source.</summary>
    public CommandBuilder FromQuery(IQueryBuilder source)
    {
        if (_source is not null)
        {
            throw new InvalidOperationException("INSERT source query is already set.");
        }
        _source = source ?? throw new ArgumentNullException(nameof(source));
        return this;
    }

    /// <summary>Sets the INSERT column list once.</summary>
    public CommandBuilder Columns(FormattableString columns)
    {
        if (_columns is not null)
        {
            throw new InvalidOperationException("INSERT columns are already set.");
        }
        _columns = columns;
        return this;
    }

    /// <summary>Adds one VALUES row to an INSERT.</summary>
    public CommandBuilder Values(FormattableString row)
    {
        _values.Add(row);
        return this;
    }

    /// <summary>Sets trusted custom write SQL.</summary>
    public CommandBuilder Sql(FormattableString sql)
    {
        if (_kind is not null)
        {
            throw new InvalidOperationException("Command kind is already set.");
        }
        _kind = "SQL";
        _custom = sql;
        return this;
    }

    /// <summary>Sets a stored procedure name.</summary>
    public CommandBuilder Procedure(string name)
    {
        if (_kind is not null)
        {
            throw new InvalidOperationException("Command kind is already set.");
        }
        ArgumentException.ThrowIfNullOrWhiteSpace(name);
        _kind = "PROCEDURE";
        _procedureName = name;
        return this;
    }

    /// <summary>Adds one named stored procedure parameter.</summary>
    public CommandBuilder ProcedureParameter(
        string name,
        object? value,
        DbType? dbType = null
    )
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(name);
        if (_procedureParameters.Any(parameter => parameter.Name == name))
        {
            throw new InvalidOperationException("Stored procedure parameter name is already set.");
        }
        _procedureParameters.Add(new SqlParameterSpec(name, value, dbType));
        return this;
    }

    /// <summary>Sets the ADO.NET timeout in seconds.</summary>
    public CommandBuilder Timeout(int seconds)
    {
        if (seconds < 0 || _timeout is not null)
        {
            throw new ArgumentOutOfRangeException(nameof(seconds));
        }
        _timeout = seconds;
        return this;
    }

    /// <summary>Compiles a fresh SQL snapshot.</summary>
    public CompiledSql Compile()
    {
        ValidateShape();
        var context = new SqlRenderContext(Dialect);
        switch (_kind)
        {
            case "SQL":
                context.Fragment(_custom!);
                break;
            case "PROCEDURE":
                context.Text.Append(_procedureName);
                break;
            case "UPDATE":
                Require(_assignments.Count > 0, "UPDATE requires SET.");
                context.Text.Append("UPDATE ");
                context.Fragment(_target!);
                RenderActions(context, _assignments, " SET ", ", ");
                AppendSql(context, UpdateAfterSetSql());
                RenderTerms(context, _predicates, " WHERE ", " AND ");
                break;
            case "DELETE FROM":
                context.Text.Append("DELETE FROM ");
                context.Fragment(_target!);
                AppendSql(context, DeleteAfterTargetSql());
                RenderTerms(context, _predicates, " WHERE ", " AND ");
                break;
            case "INSERT INTO":
                Require((_source is null) != (_values.Count == 0), "INSERT requires values or one source query.");
                context.Text.Append("INSERT INTO ");
                context.Fragment(_target!);
                if (_columns is not null)
                {
                    context.Text.Append(" (");
                    context.Fragment(_columns);
                    context.Text.Append(')');
                }
                AppendSql(context, InsertAfterTargetSql());
                context.Text.Append(' ');
                if (_source is not null)
                {
                    RenderChild(context, _source);
                }
                else
                {
                    RenderTerms(context, _values, "VALUES ", ", ", true);
                }
                break;
            default:
                throw new InvalidOperationException("Command kind is not set.");
        }
        AppendSql(context, TrailingSql());
        var result = context.Snapshot();
        return new CompiledSql(result.Text, _kind == "PROCEDURE" ? _procedureParameters : result.Parameters,
            _kind == "PROCEDURE" ? CommandType.StoredProcedure : CommandType.Text, _timeout);
    }

    /// <summary>Gets engine SQL after an INSERT target.</summary>
    protected virtual string? InsertAfterTargetSql() => null;

    /// <summary>Gets engine SQL after an UPDATE SET clause.</summary>
    protected virtual string? UpdateAfterSetSql() => null;

    /// <summary>Gets engine SQL after a DELETE target.</summary>
    protected virtual string? DeleteAfterTargetSql() => null;

    /// <summary>Gets engine SQL after the complete command.</summary>
    protected virtual string? TrailingSql() => null;

    private static void AppendSql(SqlRenderContext context, string? sql)
    {
        if (sql is not null)
        {
            context.Text.Append(' ').Append(sql);
        }
    }

    private void ValidateShape()
    {
        if (_kind != "INSERT INTO" && (_columns is not null || _values.Count != 0 || _source is not null))
        {
            throw new InvalidOperationException("INSERT clauses require INSERT INTO.");
        }
        if (_kind != "UPDATE" && _assignments.Count != 0)
        {
            throw new InvalidOperationException("SET requires UPDATE.");
        }
        if (_kind is not "UPDATE" and not "DELETE FROM" && _predicates.Count != 0)
        {
            throw new InvalidOperationException("WHERE requires UPDATE or DELETE.");
        }
        if (_kind != "PROCEDURE" && _procedureParameters.Count != 0)
        {
            throw new InvalidOperationException("Named procedure parameters require a stored procedure.");
        }
    }

    private static void RenderChild(SqlRenderContext context, IQueryBuilder child)
    {
        if (child is not QueryBuilder query)
        {
            throw new ArgumentException("A command child must be a Mise query builder.");
        }
        query.Render(context);
    }

    private static void RenderActions(
        SqlRenderContext context,
        List<Action<SqlRenderContext>> terms,
        string prefix,
        string separator
    )
    {
        context.Text.Append(prefix);
        for (var index = 0; index < terms.Count; index++)
        {
            if (index != 0)
            {
                context.Text.Append(separator);
            }
            terms[index](context);
        }
    }

    private CommandBuilder SetTarget(string kind, FormattableString target)
    {
        if (_kind is not null)
        {
            throw new InvalidOperationException("Command target is already set.");
        }
        _kind = kind;
        _target = target;
        return this;
    }

    private static void RenderTerms(
        SqlRenderContext context,
        List<FormattableString> terms,
        string prefix,
        string separator,
        bool parenthesize = false
    )
    {
        if (terms.Count == 0)
        {
            return;
        }
        context.Text.Append(prefix);
        for (var index = 0; index < terms.Count; index++)
        {
            if (index != 0)
            {
                context.Text.Append(separator);
            }
            if (parenthesize)
            {
                context.Text.Append('(');
            }
            context.Fragment(terms[index]);
            if (parenthesize)
            {
                context.Text.Append(')');
            }
        }
    }

    private static void Require(bool condition, string message)
    {
        if (!condition)
        {
            throw new InvalidOperationException(message);
        }
    }
}
