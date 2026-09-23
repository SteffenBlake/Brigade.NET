namespace Brigade.Net.Mise;

/// <summary>Compiles a read query into a fresh SQL snapshot.</summary>
public interface IQueryBuilder
{
    /// <summary>Compiles the query and its parameters.</summary>
    CompiledSql Compile();
}
