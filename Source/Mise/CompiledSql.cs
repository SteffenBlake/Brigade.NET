using System.Data;

namespace Brigade.Net.Mise;

/// <summary>An immutable SQL snapshot ready for ADO.NET binding.</summary>
/// <param name="text">SQL text.</param>
/// <param name="parameters">Parameters in SQL order.</param>
/// <param name="commandType">ADO.NET command type.</param>
/// <param name="timeout">Optional timeout in seconds.</param>
/// <param name="behavior">Supported reader behavior flags.</param>
public sealed class CompiledSql(
    string text,
    IEnumerable<SqlParameterSpec>? parameters = null,
    CommandType commandType = CommandType.Text,
    int? timeout = null,
    CommandBehavior behavior = CommandBehavior.Default
)
{
    /// <summary>Gets the SQL text.</summary>
    public string Text { get; } = text;

    /// <summary>Gets an immutable copy of parameters in binding order.</summary>
    public IReadOnlyList<SqlParameterSpec> Parameters { get; } = Array.AsReadOnly((parameters ?? []).ToArray());

    /// <summary>Gets the ADO.NET command type.</summary>
    public CommandType CommandType { get; } = commandType;

    /// <summary>Gets the optional timeout in seconds.</summary>
    public int? Timeout { get; } = timeout;

    /// <summary>Gets reader behavior flags. CloseConnection and schema-only modes are rejected.</summary>
    public CommandBehavior Behavior { get; } = ValidateBehavior(behavior);

    private static CommandBehavior ValidateBehavior(CommandBehavior behavior)
    {
        const CommandBehavior allowed = CommandBehavior.SingleResult
            | CommandBehavior.SequentialAccess
            | CommandBehavior.SingleRow;
        if ((behavior & ~allowed) != 0)
        {
            throw new ArgumentOutOfRangeException(nameof(behavior), "Unsupported reader behavior flag.");
        }
        return behavior;
    }
}
