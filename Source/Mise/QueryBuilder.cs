using System.Data;

namespace Brigade.Net.Mise;

/// <summary>Collects read clauses and renders them in SQL order. Do not mutate while compiling.</summary>
public class QueryBuilder : IQueryBuilder
{
    private readonly List<(string Name, IQueryBuilder Query, IQueryBuilder? Recursive)> _ctes = [];
    private readonly List<Action<SqlRenderContext>> _select = [];
    private readonly List<(string Kind, Action<SqlRenderContext> Source)> _joins = [];
    private readonly List<Action<SqlRenderContext>> _where = [];
    private readonly List<Action<SqlRenderContext>> _group = [];
    private readonly List<Action<SqlRenderContext>> _having = [];
    private readonly List<Action<SqlRenderContext>> _order = [];
    private readonly List<(string Kind, IQueryBuilder Query)> _sets = [];
    private Action<SqlRenderContext>? _from;
    private FormattableString? _custom;
    private int? _limit;
    private int? _offset;
    private bool _distinct;
    private CommandBehavior? _behavior;

    /// <summary>Creates a query for the default ANSI dialect.</summary>
    public QueryBuilder() : this(new AnsiSqlDialect())
    {
    }

    /// <summary>Creates a query for an engine dialect.</summary>
    public QueryBuilder(SqlDialect dialect)
    {
        Dialect = dialect ?? throw new ArgumentNullException(nameof(dialect));
    }

    /// <summary>Gets the dialect selected for this query.</summary>
    public SqlDialect Dialect { get; }

    /// <summary>Adds a named common table expression.</summary>
    public QueryBuilder With(string name, IQueryBuilder query)
    {
        AddCte(name, query, null);
        return this;
    }

    /// <summary>Adds a recursive common table expression with anchor and recursive members.</summary>
    public QueryBuilder WithRecursive(
        string name,
        IQueryBuilder anchor,
        IQueryBuilder recursive
    )
    {
        ArgumentNullException.ThrowIfNull(recursive);
        AddCte(name, anchor, recursive);
        return this;
    }

    /// <summary>Adds a projection term.</summary>
    public QueryBuilder Select(FormattableString term)
    {
        _select.Add(context => context.Fragment(term));
        return this;
    }

    /// <summary>Adds a scalar subquery projection.</summary>
    public QueryBuilder Select(IQueryBuilder query)
    {
        _select.Add(context => RenderNested(context, query));
        return this;
    }

    /// <summary>Selects distinct rows.</summary>
    public QueryBuilder Distinct()
    {
        _distinct = true;
        return this;
    }

    /// <summary>Sets the single source clause.</summary>
    public QueryBuilder From(FormattableString source)
    {
        SetFrom(context => context.Fragment(source));
        return this;
    }

    /// <summary>Sets a derived table as the source.</summary>
    public QueryBuilder From(IQueryBuilder query, string alias)
    {
        SetFrom(context =>
        {
            RenderNested(context, query);
            context.Text.Append(" AS ").Append(Dialect.QuoteIdentifier(alias));
        });
        return this;
    }

    /// <summary>Adds an inner join using a relationship fragment.</summary>
    public QueryBuilder InnerJoin(FormattableString relationship) => AddJoinFragment("INNER JOIN", relationship);

    /// <summary>Adds a left join using a relationship fragment.</summary>
    public QueryBuilder LeftJoin(FormattableString relationship) => AddJoinFragment("LEFT JOIN", relationship);

    /// <summary>Adds a right join using a relationship fragment.</summary>
    public QueryBuilder RightJoin(FormattableString relationship) => AddJoinFragment("RIGHT JOIN", relationship);

    /// <summary>Adds a full join using a relationship fragment.</summary>
    public QueryBuilder FullJoin(FormattableString relationship) => AddJoinFragment("FULL JOIN", relationship);

    /// <summary>Adds a cross join with no ON predicate.</summary>
    public QueryBuilder CrossJoin(FormattableString source) => AddJoinFragment("CROSS JOIN", source);

    /// <summary>Adds a derived cross join with no ON predicate.</summary>
    public QueryBuilder CrossJoin(IQueryBuilder query, string alias) => AddJoin("CROSS JOIN", context =>
    {
        RenderNested(context, query);
        context.Text.Append(" AS ").Append(Dialect.QuoteIdentifier(alias));
    });

    /// <summary>Adds an AND predicate.</summary>
    public QueryBuilder Where(FormattableString predicate)
    {
        _where.Add(context => context.Fragment(predicate));
        return this;
    }

    /// <summary>Adds an EXISTS predicate.</summary>
    public QueryBuilder WhereExists(IQueryBuilder query)
    {
        _where.Add(context =>
        {
            context.Text.Append("EXISTS ");
            RenderNested(context, query);
        });
        return this;
    }

    /// <summary>Adds an IN predicate. An empty list renders <c>1 = 0</c> without parameters.</summary>
    public QueryBuilder WhereIn<T>(FormattableString expression, IReadOnlyCollection<T> values)
    {
        ArgumentNullException.ThrowIfNull(values);
        var snapshot = values.ToArray();
        _where.Add(context =>
        {
            if (snapshot.Length == 0)
            {
                context.Text.Append("1 = 0");
                return;
            }
            context.Fragment(expression);
            context.Text.Append(" IN (");
            var first = true;
            foreach (var value in snapshot)
            {
                if (!first)
                {
                    context.Text.Append(", ");
                }
                first = false;
                context.Parameter(value);
            }
            context.Text.Append(')');
        });
        return this;
    }

    /// <summary>Adds an IN subquery predicate.</summary>
    public QueryBuilder WhereIn(FormattableString expression, IQueryBuilder query)
    {
        _where.Add(context =>
        {
            context.Fragment(expression);
            context.Text.Append(" IN ");
            RenderNested(context, query);
        });
        return this;
    }

    /// <summary>Adds a grouping term.</summary>
    public QueryBuilder GroupBy(FormattableString term)
    {
        _group.Add(context => context.Fragment(term));
        return this;
    }

    /// <summary>Adds an AND predicate on grouped rows.</summary>
    public QueryBuilder Having(FormattableString predicate)
    {
        _having.Add(context => context.Fragment(predicate));
        return this;
    }

    /// <summary>Adds an ordering term.</summary>
    public QueryBuilder OrderBy(FormattableString term)
    {
        _order.Add(context => context.Fragment(term));
        return this;
    }

    /// <summary>Sets the maximum row count once.</summary>
    public QueryBuilder Limit(int count)
    {
        SetPaging(ref _limit, count);
        return this;
    }

    /// <summary>Sets the row offset once.</summary>
    public QueryBuilder Offset(int count)
    {
        SetPaging(ref _offset, count);
        return this;
    }

    /// <summary>Sets supported ADO.NET reader behavior flags once.</summary>
    public QueryBuilder Behavior(CommandBehavior behavior)
    {
        if (_behavior is not null)
        {
            throw new InvalidOperationException("Reader behavior is already set.");
        }
        _behavior = behavior;
        return this;
    }

    /// <summary>Appends a UNION query.</summary>
    public QueryBuilder Union(IQueryBuilder query) => AddSet("UNION", query);

    /// <summary>Appends a UNION ALL query.</summary>
    public QueryBuilder UnionAll(IQueryBuilder query) => AddSet("UNION ALL", query);

    /// <summary>Appends an INTERSECT query.</summary>
    public QueryBuilder Intersect(IQueryBuilder query) => AddSet("INTERSECT", query);

    /// <summary>Appends an EXCEPT query.</summary>
    public QueryBuilder Except(IQueryBuilder query) => AddSet("EXCEPT", query);

    /// <summary>Sets trusted custom read SQL as the whole query.</summary>
    public QueryBuilder Sql(FormattableString sql)
    {
        if (_custom is not null)
        {
            throw new InvalidOperationException("Custom SQL is already set.");
        }
        _custom = sql;
        return this;
    }

    /// <summary>Compiles a fresh SQL snapshot.</summary>
    public CompiledSql Compile()
    {
        var context = new SqlRenderContext(Dialect);
        Render(context);
        var snapshot = context.Snapshot();
        return new CompiledSql(
            snapshot.Text,
            snapshot.Parameters,
            behavior: _behavior ?? CommandBehavior.Default
        );
    }

    internal void Render(SqlRenderContext context)
    {
        if (context.Dialect.GetType() != Dialect.GetType())
        {
            throw new InvalidOperationException("A query child uses an incompatible SQL engine.");
        }
        context.Enter(this);
        try
        {
            RenderBody(context);
        }
        finally
        {
            context.Exit(this);
        }
    }

    private void RenderBody(SqlRenderContext context)
    {
        if (_custom is not null)
        {
            if (_ctes.Count != 0 || _select.Count != 0 || _from is not null || _joins.Count != 0
                || _where.Count != 0 || _group.Count != 0 || _having.Count != 0 || _order.Count != 0
                || _sets.Count != 0 || _limit is not null || _offset is not null || _distinct)
            {
                throw new InvalidOperationException("Custom SQL cannot be mixed with fluent clauses.");
            }
            context.Fragment(_custom);
            return;
        }
        if (_select.Count == 0)
        {
            throw new InvalidOperationException("SELECT requires a projection.");
        }
        if (_joins.Count != 0 && _from is null)
        {
            throw new InvalidOperationException("JOIN requires FROM.");
        }
        if (_ctes.Count != 0)
        {
            var recursive = _ctes.Any(cte => cte.Recursive is not null);
            context.Text.Append(recursive && Dialect.UsesRecursiveKeyword ? "WITH RECURSIVE " : "WITH ");
            for (var index = 0; index < _ctes.Count; index++)
            {
                if (index != 0)
                {
                    context.Text.Append(", ");
                }
                var cte = _ctes[index];
                context.Text.Append(Dialect.QuoteIdentifier(cte.Name)).Append(" AS (");
                RenderChild(context, cte.Query);
                if (cte.Recursive is not null)
                {
                    context.Text.Append(" UNION ALL ");
                    RenderChild(context, cte.Recursive);
                }
                context.Text.Append(')');
            }
            context.Text.Append(' ');
        }
        context.Text.Append("SELECT ");
        if (_distinct)
        {
            context.Text.Append("DISTINCT ");
        }
        RenderTerms(context, _select, "", ", ");
        if (_from is not null)
        {
            context.Text.Append(" FROM ");
            _from(context);
        }
        foreach (var join in _joins)
        {
            if ((join.Kind == "RIGHT JOIN" && !Dialect.SupportsRightJoin)
                || (join.Kind == "FULL JOIN" && !Dialect.SupportsFullJoin))
            {
                throw new NotSupportedException($"{Dialect.Name} does not support {join.Kind}.");
            }
            context.Text.Append(' ').Append(join.Kind).Append(' ');
            join.Source(context);
        }
        RenderTerms(context, _where, " WHERE ", " AND ");
        RenderTerms(context, _group, " GROUP BY ", ", ");
        RenderTerms(context, _having, " HAVING ", " AND ");
        foreach (var set in _sets)
        {
            context.Text.Append(' ').Append(set.Kind).Append(' ');
            RenderChild(context, set.Query);
        }
        RenderTerms(context, _order, " ORDER BY ", ", ");
        if ((_limit is not null || _offset is not null) && Dialect.RequiresOrderByForPaging && _order.Count == 0)
        {
            throw new InvalidOperationException($"{Dialect.Name} paging requires ORDER BY.");
        }
        Dialect.AppendPaging(context.Text, _limit, _offset);
        if (TrailingSql() is string suffix)
        {
            context.Text.Append(' ').Append(suffix);
        }
    }

    /// <summary>Gets engine-specific SQL at the end of this query.</summary>
    protected virtual string? TrailingSql() => null;

    private void AddCte(
        string name,
        IQueryBuilder query,
        IQueryBuilder? recursive
    )
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(name);
        ArgumentNullException.ThrowIfNull(query);
        if (_ctes.Any(cte => cte.Name == name))
        {
            throw new InvalidOperationException("CTE name is already set.");
        }
        _ctes.Add((name, query, recursive));
    }

    private void SetFrom(Action<SqlRenderContext> source)
    {
        if (_from is not null)
        {
            throw new InvalidOperationException("FROM is already set.");
        }
        _from = source;
    }

    private QueryBuilder AddJoin(string kind, Action<SqlRenderContext> source)
    {
        _joins.Add((kind, source));
        return this;
    }

    private QueryBuilder AddJoinFragment(string kind, FormattableString source)
    {
        ArgumentNullException.ThrowIfNull(source);
        return AddJoin(kind, context =>
        {
            var start = context.Text.Length;
            context.Fragment(source);
            var rendered = context.Text.ToString(start, context.Text.Length - start);
            if (string.IsNullOrWhiteSpace(rendered))
            {
                throw new ArgumentException("Join source must not be empty.", nameof(source));
            }
            var hasPredicate = rendered.Contains(" ON ", StringComparison.OrdinalIgnoreCase);
            if (kind == "CROSS JOIN" && hasPredicate)
            {
                throw new ArgumentException("CROSS JOIN source must not contain an ON predicate.", nameof(source));
            }
            if (kind != "CROSS JOIN" && !hasPredicate)
            {
                throw new ArgumentException("Relationship join must contain an ON predicate.", nameof(source));
            }
        });
    }

    private QueryBuilder AddSet(string kind, IQueryBuilder query)
    {
        _sets.Add((kind, query ?? throw new ArgumentNullException(nameof(query))));
        return this;
    }

    private void RenderNested(SqlRenderContext context, IQueryBuilder query)
    {
        context.Text.Append('(');
        RenderChild(context, query);
        context.Text.Append(')');
    }

    private static void RenderChild(SqlRenderContext context, IQueryBuilder child)
    {
        if (child is not QueryBuilder query)
        {
            throw new ArgumentException("A query child must be a Mise query builder.");
        }
        query.Render(context);
    }

    private static void RenderTerms(
        SqlRenderContext context,
        List<Action<SqlRenderContext>> terms,
        string prefix,
        string separator
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
            terms[index](context);
        }
    }

    private static void SetPaging(ref int? slot, int value)
    {
        if (value < 0 || slot is not null)
        {
            throw new ArgumentOutOfRangeException(nameof(value), "Paging must be non-negative and set once.");
        }
        slot = value;
    }
}
