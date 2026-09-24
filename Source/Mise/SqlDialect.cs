using System.Text;
namespace Brigade.Net.Mise;

/// <summary>Defines engine SQL syntax used while compiling Mise builders.</summary>
public abstract class SqlDialect
{
    /// <summary>Gets the engine name used to reject mixed builder trees.</summary>
    public abstract string Name { get; }

    /// <summary>Gets whether RIGHT JOIN is supported.</summary>
    public virtual bool SupportsRightJoin => true;

    /// <summary>Gets whether FULL JOIN is supported.</summary>
    public virtual bool SupportsFullJoin => true;

    /// <summary>Gets whether recursive CTEs use WITH RECURSIVE.</summary>
    public virtual bool UsesRecursiveKeyword => true;

    /// <summary>Gets whether paging requires a top-level ORDER BY.</summary>
    public virtual bool RequiresOrderByForPaging => false;

    /// <summary>Quotes one identifier without accepting SQL syntax.</summary>
    public abstract string QuoteIdentifier(string identifier);

    /// <summary>Appends paging syntax after ORDER BY.</summary>
    public virtual void AppendPaging(
        StringBuilder text,
        int? limit,
        int? offset
    )
    {
        if (limit is not null || offset is not null)
        {
            throw new NotSupportedException("Paging requires an engine dialect.");
        }
    }
}
