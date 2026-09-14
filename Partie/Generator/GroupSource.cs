using System;
using System.Collections.Immutable;
using Microsoft.CodeAnalysis;

namespace Brigade.Net.Partie.Generator;

internal sealed class GroupSource(
    string key,
    string registrations,
    string members,
    string adapter
) : IEquatable<GroupSource>
{
    public string Key { get; } = key;
    public string Registrations { get; } = registrations;
    public string Members { get; } = members;
    public string Adapter { get; } = adapter;

    public bool Equals(GroupSource? other) => other is not null && Key == other.Key && Registrations == other.Registrations && Members == other.Members && Adapter == other.Adapter;
    public override bool Equals(object? obj) => obj is GroupSource other && Equals(other);
    public override int GetHashCode() => Key.GetHashCode();
}
