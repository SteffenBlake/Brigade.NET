using Brigade.Net.Core.Results;

namespace Brigade.Net.Partie;
/// <summary>Describes one constructor argument in a route's generated input type.</summary>
/// <param name = "Name">The external binding name.</param>
/// <param name = "MemberName">The corresponding generated input property and constructor parameter.</param>
/// <param name = "ValueType">The CLR value type.</param>
/// <param name = "Source">The binding source.</param>
public sealed record PartieInput(
    string Name,
    string MemberName,
    Type ValueType,
    PartieInputSource Source
);
