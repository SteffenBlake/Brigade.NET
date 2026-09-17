using System;

namespace Brigade.Net.Partie.Generator;

/// <summary>A value-equatable engine declaration used for source output and semantic binding.</summary>
public readonly struct GeneratedDeclaration(string hintName, string source) : IEquatable<GeneratedDeclaration>
{
    public string HintName { get; } = hintName;
    public string Source { get; } = source;
    public bool Equals(GeneratedDeclaration other) => HintName == other.HintName && Source == other.Source;
    public override bool Equals(object? obj) => obj is GeneratedDeclaration other && Equals(other);
    public override int GetHashCode() => HintName.GetHashCode() ^ Source.GetHashCode();
}
