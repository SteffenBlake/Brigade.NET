using System.Data;
using System.Globalization;
using System.Text;

namespace Brigade.Net.Mise;

internal sealed class SqlRenderContext
{
    private readonly List<SqlParameterSpec> _parameters = [];
    private readonly HashSet<QueryBuilder> _active = [];

    internal SqlRenderContext(SqlDialect dialect)
    {
        Dialect = dialect;
    }

    internal SqlDialect Dialect { get; }

    internal StringBuilder Text { get; } = new();

    internal void Fragment(FormattableString fragment)
    {
        var format = fragment.Format;
        var args = fragment.GetArguments();
        for (var index = 0; index < format.Length; index++)
        {
            var character = format[index];
            if (character == '}' && index + 1 < format.Length && format[index + 1] == '}')
            {
                Text.Append('}');
                index++;
                continue;
            }
            if (character == '{' && index + 1 < format.Length && format[index + 1] == '{')
            {
                Text.Append('{');
                index++;
                continue;
            }
            if (character != '{')
            {
                if (character == '}')
                {
                    throw new FormatException("Unescaped closing brace in SQL fragment.");
                }
                Text.Append(character);
                continue;
            }

            var start = ++index;
            while (index < format.Length && char.IsDigit(format[index]))
            {
                index++;
            }
            if (start == index
                || !int.TryParse(
                    format[start..index],
                    NumberStyles.None,
                    CultureInfo.InvariantCulture,
                    out var argument
                )
                || argument >= args.Length)
            {
                throw new FormatException("Invalid SQL interpolation index.");
            }
            while (index < format.Length && format[index] == ' ')
            {
                index++;
            }
            if (index < format.Length && format[index] == ',')
            {
                index++;
                while (index < format.Length && format[index] == ' ')
                {
                    index++;
                }
                if (index < format.Length && format[index] is '-' or '+')
                {
                    index++;
                }
                start = index;
                while (index < format.Length && char.IsDigit(format[index]))
                {
                    index++;
                }
                if (start == index)
                {
                    throw new FormatException("Invalid SQL interpolation alignment.");
                }
                while (index < format.Length && format[index] == ' ')
                {
                    index++;
                }
            }
            var specifier = string.Empty;
            if (index < format.Length && format[index] == ':')
            {
                start = ++index;
                while (index < format.Length && format[index] != '}')
                {
                    index++;
                }
                specifier = format[start..index];
            }
            if (index >= format.Length || format[index] != '}' || (specifier.Length != 0 && specifier != "raw"))
            {
                throw new FormatException("Invalid or unsupported SQL interpolation format.");
            }
            if (specifier == "raw")
            {
                if (args[argument] is not string raw)
                {
                    throw new ArgumentException("Raw SQL interpolation requires a string constant.");
                }
                Text.Append(raw);
            }
            else
            {
                Parameter(args[argument]);
            }
        }
    }

    internal void Parameter(object? value, DbType? dbType = null)
    {
        if (value is SqlParameterSpec specification)
        {
            value = specification.Value;
            dbType ??= specification.DbType;
        }
        var name = "@p" + _parameters.Count.ToString(CultureInfo.InvariantCulture);
        Text.Append(name);
        _parameters.Add(new SqlParameterSpec(name, value, dbType));
    }

    internal void Enter(QueryBuilder query)
    {
        if (!_active.Add(query))
        {
            throw new InvalidOperationException("A query cannot contain itself.");
        }
    }

    internal void Exit(QueryBuilder query) => _active.Remove(query);

    internal CompiledSql Snapshot() => new(Text.ToString(), _parameters);
}
