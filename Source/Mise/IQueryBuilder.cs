namespace Brigade.Net.Mise;

/// <summary>Builds an immutable command at the execution boundary.</summary>
public interface IQueryBuilder
{
    /// <summary>Builds a fresh immutable command snapshot.</summary>
    MiseCommand Build();
}
