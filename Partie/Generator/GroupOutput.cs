using System;
using System.Collections.Immutable;
using Microsoft.CodeAnalysis;

namespace Brigade.Net.Partie.Generator;

internal sealed class GroupOutput(
    string stubs,
    GroupSource source,
    ImmutableArray<Diagnostic> diagnostics
)
{
    public string Stubs { get; } = stubs;
    public GroupSource Source { get; } = source;
    public ImmutableArray<Diagnostic> Diagnostics { get; } = diagnostics;
}
