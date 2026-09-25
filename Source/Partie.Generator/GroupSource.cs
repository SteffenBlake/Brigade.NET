using System;
using System.Collections.Immutable;
using System.Linq;
using Microsoft.CodeAnalysis;

namespace Brigade.Net.Partie.Generator;

internal sealed class GroupSource(
    string key,
    string registrations,
    string members,
    string adapter,
    ImmutableArray<RouteGroupEmission> groups,
    ImmutableArray<GeneratedDeclaration> declarations
) : IEquatable<GroupSource>
{
    public string Key { get; } = key;

    public string Registrations { get; } = registrations;

    public string Members { get; } = members;

    public string Adapter { get; } = adapter;

    public ImmutableArray<RouteGroupEmission> Groups { get; } = groups;

    public ImmutableArray<GeneratedDeclaration> Declarations { get; } = declarations;

    public bool Equals(GroupSource? other)
    {
        return other is not null
            && Key == other.Key
            && Registrations == other.Registrations
            && Members == other.Members
            && Adapter == other.Adapter
            && Groups.SequenceEqual(other.Groups)
            && Declarations.SequenceEqual(other.Declarations);
    }

    public override bool Equals(object? obj)
    {
        return obj is GroupSource other && Equals(other);
    }

    public override int GetHashCode()
    {
        return Key.GetHashCode();
    }
}
