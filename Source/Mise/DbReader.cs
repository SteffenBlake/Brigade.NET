using System.Data;
using System.Data.Common;
using Brigade.Net.Core.Results;

namespace Brigade.Net.Mise;

/// <summary>Executes read commands against one non-concurrent connection scope.</summary>
/// <remarks>
/// Pass <paramref name="config"/> to let this instance create, open, own, and dispose a connection.
/// Pass <paramref name="connection"/> to use a caller-owned connection that this instance never
/// closes or disposes. Exactly one argument must be non-null. Every terminal owns and asynchronously
/// disposes the command and reader it creates. Provider and disposal exceptions escape unchanged.
/// </remarks>
/// <param name="config">Configuration for an owned connection.</param>
/// <param name="connection">An existing caller-owned connection.</param>
public class DbReader(IMiseConfig? config = null, DbConnection? connection = null) : IAsyncDisposable
{
    private readonly IMiseConfig? _config = Validate(config, connection);
    private DbConnection? _connection = connection;
    private int _active;
    private bool _disposed;

    /// <summary>Reads all rows and returns a successful list, including an empty list.</summary>
    public async Task<Result<IReadOnlyList<T>>> ListAsync<T>(
        IQueryBuilder query,
        CancellationToken cancellationToken = default
    )
        where T : IMiseRow<T>
    {
        Enter();
        try
        {
            await using var command = await CreateCommandAsync(query, cancellationToken);
            await using var reader = await command.ExecuteReaderAsync(cancellationToken);
            var ordinals = T.BindOrdinals(reader);
            var values = new List<T>();
            while (await reader.ReadAsync(cancellationToken))
            {
                values.Add(T.Materialize(reader, ordinals));
            }
            return values;
        }
        finally
        {
            Exit();
        }
    }

    /// <summary>Reads the first row or returns <see cref="NotFound"/> when no row exists.</summary>
    public async Task<Result<T>> FirstOrNotFoundAsync<T>(
        IQueryBuilder query,
        CancellationToken cancellationToken = default
    )
        where T : IMiseRow<T>
    {
        Enter();
        try
        {
            await using var command = await CreateCommandAsync(query, cancellationToken);
            await using var reader = await command.ExecuteReaderAsync(cancellationToken);
            if (!await reader.ReadAsync(cancellationToken))
            {
                return new NotFound();
            }
            var ordinals = T.BindOrdinals(reader);
            return T.Materialize(reader, ordinals);
        }
        finally
        {
            Exit();
        }
    }

    /// <summary>Executes a scalar command. Database NULL maps to <see langword="null"/>.</summary>
    public async Task<Result<T?>> ScalarAsync<T>(
        IQueryBuilder query,
        CancellationToken cancellationToken = default
    )
    {
        Enter();
        try
        {
            await using var command = await CreateCommandAsync(query, cancellationToken);
            var value = await command.ExecuteScalarAsync(cancellationToken);
            return value is null or DBNull ? default : (T)value;
        }
        finally
        {
            Exit();
        }
    }

    /// <summary>Returns whether the command yields at least one row.</summary>
    public async Task<Result<bool>> ExistsAsync(
        IQueryBuilder query,
        CancellationToken cancellationToken = default
    )
    {
        Enter();
        try
        {
            await using var command = await CreateCommandAsync(query, cancellationToken);
            await using var reader = await command.ExecuteReaderAsync(CommandBehavior.SingleRow, cancellationToken);
            return await reader.ReadAsync(cancellationToken);
        }
        finally
        {
            Exit();
        }
    }

    /// <summary>Disposes the connection only when this instance created it from configuration.</summary>
    public virtual async ValueTask DisposeAsync()
    {
        if (_disposed)
        {
            return;
        }
        if (Volatile.Read(ref _active) != 0)
        {
            throw new InvalidOperationException("Cannot dispose a Mise reader while an operation is active.");
        }

        _disposed = true;
        if (_config is not null && _connection is not null)
        {
            await _connection.DisposeAsync();
        }
    }

    /// <summary>Creates and binds one command owned by the calling terminal.</summary>
    protected async ValueTask<DbCommand> CreateCommandAsync(
        IQueryBuilder query,
        CancellationToken cancellationToken
    )
    {
        var built = query.Build();
        var activeConnection = await GetConnectionAsync(cancellationToken);
        var command = activeConnection.CreateCommand();
        try
        {
            command.CommandText = built.Text;
            command.CommandType = built.CommandType;
            if (built.Timeout is int timeout)
            {
                command.CommandTimeout = timeout;
            }
            foreach (var specification in built.Parameters)
            {
                var parameter = _config is null
                    ? command.CreateParameter()
                    : _config.ProviderFactory.CreateParameter()
                        ?? throw new MiseInvalidMappingException(GetType(), "provider returned no parameter");
                parameter.ParameterName = specification.Name;
                parameter.Value = specification.Value ?? DBNull.Value;
                if (specification.DbType is DbType dbType)
                {
                    parameter.DbType = dbType;
                }
                command.Parameters.Add(parameter);
            }
            return command;
        }
        catch
        {
            await command.DisposeAsync();
            throw;
        }
    }

    /// <summary>Begins exclusive use of this instance.</summary>
    protected void Enter()
    {
        ObjectDisposedException.ThrowIf(_disposed, this);
        if (Interlocked.CompareExchange(ref _active, 1, 0) != 0)
        {
            throw new InvalidOperationException("Mise reader and writer instances allow one active operation at a time.");
        }
    }

    /// <summary>Ends exclusive use of this instance.</summary>
    protected void Exit() => Volatile.Write(ref _active, 0);

    private async ValueTask<DbConnection> GetConnectionAsync(CancellationToken cancellationToken)
    {
        if (_connection is null)
        {
            _connection = _config!.ProviderFactory.CreateConnection()
                ?? throw new MiseInvalidMappingException(GetType(), "provider returned no connection");
            _connection.ConnectionString = _config.ConnectionString;
        }
        if (_connection.State == ConnectionState.Closed)
        {
            await _connection.OpenAsync(cancellationToken);
        }
        return _connection;
    }

    private static IMiseConfig? Validate(IMiseConfig? config, DbConnection? connection)
    {
        if ((config is null) == (connection is null))
        {
            throw new ArgumentException("Supply exactly one of config or connection.");
        }
        return config;
    }
}
