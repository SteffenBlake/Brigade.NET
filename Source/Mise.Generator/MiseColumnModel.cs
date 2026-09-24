using Microsoft.CodeAnalysis;

namespace Brigade.Net.Mise.Generator;

internal sealed record ColumnModel(string Name, IPropertySymbol Property);
