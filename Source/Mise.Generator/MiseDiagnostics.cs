using Microsoft.CodeAnalysis;

namespace Brigade.Net.Mise.Generator;

public static class MiseDiagnostics
{
    private const string Category = "Mise";

    public static readonly DiagnosticDescriptor InvalidIdentifier = Error("MISE001", "Invalid database identifier", "{0} must not be null, empty, or whitespace");
    public static readonly DiagnosticDescriptor MissingColumn = Error("MISE002", "Mapped property has no column", "Mapped property '{0}' must have MiseColumnAttribute");
    public static readonly DiagnosticDescriptor DuplicateIdentifier = Error("MISE003", "Duplicate database identifier", "{0} identifier '{1}' is duplicated");
    public static readonly DiagnosticDescriptor InvalidKey = Error("MISE004", "Invalid primary key", "Primary-key position for '{0}' must be non-negative and unique");
    public static readonly DiagnosticDescriptor ContradictoryMetadata = Error("MISE005", "Contradictory mapping metadata", "Mapped property '{0}' cannot be both database-generated and computed");
    public static readonly DiagnosticDescriptor MustBePartial = Error("MISE006", "Row target must be partial", "Row target '{0}' and each containing type must be partial");
    public static readonly DiagnosticDescriptor UnsupportedMember = Error("MISE007", "Mapped member cannot be materialized", "Mapped property '{0}' cannot be assigned by the selected materialization path");
    public static readonly DiagnosticDescriptor InvalidConstructor = Error("MISE008", "Invalid materialization path", "Row target '{0}' has {1} valid materialization constructors; exactly one is required");
    public static readonly DiagnosticDescriptor UnknownRelationshipColumn = Error("MISE009", "Unknown relationship column", "Relationship '{0}' refers to unknown mapped column '{1}'");
    public static readonly DiagnosticDescriptor UnsupportedRowType = Error("MISE010", "Unsupported row type", "Row target '{0}' cannot be ref-like, static, or abstract");
    public static readonly DiagnosticDescriptor InvalidRelationshipTarget = Error("MISE011", "Invalid relationship target", "Relationship '{0}' target must have MiseTableAttribute");

    private static DiagnosticDescriptor Error(string id, string title, string message)
    {
        return new DiagnosticDescriptor(id, title, message, Category, DiagnosticSeverity.Error, true);
    }
}
