using System.Data;

namespace Brigade.Net.Mise;

/// <summary>Describes one immutable command parameter.</summary>
/// <param name="Name">The provider parameter name.</param>
/// <param name="Value">The value, or <see langword="null"/> for database NULL.</param>
/// <param name="DbType">An optional provider-neutral type hint.</param>
public sealed record SqlParameterSpec(string Name, object? Value, DbType? DbType = null);
