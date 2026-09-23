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
    public static readonly DiagnosticDescriptor MustBePartial = Error("MISE006", "Mapped target must be partial", "Mapped target '{0}' and each containing type must be partial");
    public static readonly DiagnosticDescriptor UnsupportedMember = Error("MISE007", "Mapped member cannot be materialized", "Mapped property '{0}' cannot be assigned by the selected materialization path");
    public static readonly DiagnosticDescriptor InvalidConstructor = Error("MISE008", "Invalid materialization path", "Row target '{0}' has {1} valid materialization constructors; exactly one is required");
    public static readonly DiagnosticDescriptor UnknownRelationshipColumn = Error("MISE009", "Unknown relationship column", "Relationship '{0}' refers to unknown mapped column '{1}'");
    public static readonly DiagnosticDescriptor UnsupportedRowType = Error("MISE010", "Unsupported row type", "Row target '{0}' cannot be ref-like, static, or abstract");
    public static readonly DiagnosticDescriptor InvalidRelationshipTarget = Error("MISE011", "Invalid relationship target", "Relationship '{0}' target must have the active engine's table attribute");
    public static readonly DiagnosticDescriptor UnsafeRawInterpolation = Error("MISE012", "Unsafe raw SQL interpolation", "A ':raw' interpolation must be a compile-time constant string");
    public static readonly DiagnosticDescriptor MultipleEngineTables = Error("MISE013", "Multiple database engines on one table", "Mapped target '{0}' has table attributes for more than one database engine");
    public static readonly DiagnosticDescriptor MultipleEngineRows = Error("MISE014", "Multiple database engines on one row", "Mapped target '{0}' has row attributes for more than one database engine");
    public static readonly DiagnosticDescriptor MismatchedEngineRow = Error("MISE015", "Table and row engine mismatch", "Mapped target '{0}' must use table and row attributes from the same database engine");
    public static readonly DiagnosticDescriptor GeneratedMemberCollision = Error("MISE016", "Generated member name collision", "Generated member name '{0}' collides within Tbl");
    public static readonly DiagnosticDescriptor UnsupportedJoin = Error("MISE017", "Unsupported database join", "{0} does not support {1}");

    private static DiagnosticDescriptor Error(string id, string title, string message)
    {
        return new DiagnosticDescriptor(id, title, message, Category, DiagnosticSeverity.Error, true);
    }
}
