using System.Data;

namespace Brigade.Net.Mise;

/// <summary>An immutable command snapshot ready for ADO.NET execution.</summary>
/// <param name="text">The command text.</param>
/// <param name="parameters">Parameters in provider binding order.</param>
/// <param name="commandType">The ADO.NET command type.</param>
/// <param name="timeout">An optional timeout in seconds.</param>
public sealed class MiseCommand(
    string text,
    IEnumerable<MiseParameter>? parameters = null,
    CommandType commandType = CommandType.Text,
    int? timeout = null
)
{
    /// <summary>Gets the command text.</summary>
    public string Text { get; } = text;

    /// <summary>Gets an immutable copy of parameters in binding order.</summary>
    public IReadOnlyList<MiseParameter> Parameters { get; } = Array.AsReadOnly((parameters ?? []).ToArray());

    /// <summary>Gets the ADO.NET command type.</summary>
    public CommandType CommandType { get; } = commandType;

    /// <summary>Gets the optional timeout in seconds.</summary>
    public int? Timeout { get; } = timeout;
}
