namespace Brigade.Net.Mise;

/// <summary>Reports an invalid generated mapping detected while binding a result set.</summary>
/// <param name="resultType">The result type whose generated mapping is invalid.</param>
/// <param name="reason">Safe structural detail about the invalid mapping.</param>
public sealed class InvalidMappingException(Type resultType, string reason) :
    DatabaseException($"Invalid generated mapping for '{resultType.FullName}': {reason}")
{
    /// <summary>Gets the result type whose mapping is invalid.</summary>
    public Type ResultType { get; } = resultType;
}
