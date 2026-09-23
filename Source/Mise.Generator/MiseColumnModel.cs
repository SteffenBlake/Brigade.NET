using Microsoft.CodeAnalysis;

namespace Brigade.Net.Mise.Generator;

internal sealed record MiseColumnModel(string Name, IPropertySymbol Property);
