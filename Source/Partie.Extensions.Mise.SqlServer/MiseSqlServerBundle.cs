namespace Brigade.Net.Partie.Extensions.Mise.SqlServer;

/// <summary>Provides a named SqlServer configuration, query reader and command writer with its transaction.</summary>
/// <param name="Config">The named database configuration provider.</param>
/// <param name="Reader">The query reader provider.</param>
/// <param name="Transaction">The command transaction provider.</param>
/// <param name="Writer">The command writer provider.</param>
public sealed record MiseSqlServerBundle<TRequest, TResult>(
    SqlServerDbConfigProvider<TRequest, TResult> Config,
    DbReaderProvider<TRequest, TResult> Reader,
    DbWriterTxnProvider<TRequest, TResult> Transaction,
    DbWriterProvider<TRequest, TResult> Writer
);
