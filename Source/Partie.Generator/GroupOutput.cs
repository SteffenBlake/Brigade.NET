using System;
using System.Collections.Immutable;
using System.Linq;
using Microsoft.CodeAnalysis;

namespace Brigade.Net.Partie.Generator;

internal sealed class GroupOutput(
    string stubs,
    GroupSource source,
    ImmutableArray<Diagnostic> diagnostics
) : IEquatable<GroupOutput>
{
    public string Stubs { get; } = stubs;

    public GroupSource Source { get; } = source;

    public ImmutableArray<Diagnostic> Diagnostics { get; } = diagnostics;

    public bool Equals(GroupOutput? other)
    {
        return other is not null
            && Stubs == other.Stubs
            && Source.Equals(other.Source)
            && Diagnostics.SequenceEqual(other.Diagnostics);
    }

    public override bool Equals(object? obj)
    {
        return obj is GroupOutput other && Equals(other);
    }

    public override int GetHashCode()
    {
        return Source.GetHashCode();
    }
}
