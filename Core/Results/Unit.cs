namespace Brigade.Net.Core.Results;

/// <summary>
/// A type with exactly one value, used as the success payload for operations that have no meaningful result.
/// </summary>
public readonly struct Unit
{
    /// <summary>
    /// The single instance of <see cref="Unit" />.
    /// </summary>
    public static readonly Unit Default = new();

    /// <inheritdoc />
    public override string ToString() => "()";
}
