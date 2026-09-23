using System;
using System.Collections.Immutable;
using System.Linq;
using Microsoft.CodeAnalysis;

namespace Brigade.Net.Mise.Generator;

public sealed class GeneratedTarget(
    string hintName,
    string? source,
    ImmutableArray<Diagnostic> diagnostics
) : IEquatable<GeneratedTarget>
{
    private readonly string equalityKey = CreateEqualityKey(hintName, source, diagnostics);

    public string HintName { get; } = hintName;

    public string? Source { get; } = source;

    public ImmutableArray<Diagnostic> Diagnostics { get; } = diagnostics;

    public bool Equals(GeneratedTarget? other)
    {
        return other is not null && equalityKey == other.equalityKey;
    }

    public override bool Equals(object? obj)
    {
        return obj is GeneratedTarget other && Equals(other);
    }

    public override int GetHashCode()
    {
        return equalityKey.GetHashCode();
    }

    private static string CreateEqualityKey(
        string hintName,
        string? source,
        ImmutableArray<Diagnostic> diagnostics
    )
    {
        var diagnosticKeys = diagnostics.Select(diagnostic => string.Join(
            "\u001f",
            diagnostic.Id,
            diagnostic.Severity,
            diagnostic.GetMessage(),
            diagnostic.Location.SourceSpan,
            diagnostic.Location.SourceTree?.FilePath
        ));
        return hintName + "\u001e" + source + "\u001e" + string.Join("\u001d", diagnosticKeys);
    }
}
