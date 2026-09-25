using System;
using System.Collections.Immutable;
using System.Linq;

namespace Brigade.Net.Partie.Generator;

/// <summary>A transport-neutral group. Path contains this group's local path components.</summary>
public sealed class RouteGroupEmission(
    string key,
    string name,
    string? parentKey,
    ImmutableArray<string> path
) : IEquatable<RouteGroupEmission>
{
    public string Key { get; } = key;

    public string Name { get; } = name;

    public string? ParentKey { get; } = parentKey;

    public ImmutableArray<string> Path { get; } = path;

    public bool Equals(RouteGroupEmission? other)
    {
        return other is not null
            && Key == other.Key
            && Name == other.Name
            && ParentKey == other.ParentKey
            && Path.SequenceEqual(other.Path);
    }

    public override bool Equals(object? obj)
    {
        return obj is RouteGroupEmission other && Equals(other);
    }

    public override int GetHashCode()
    {
        return Key.GetHashCode();
    }
}
