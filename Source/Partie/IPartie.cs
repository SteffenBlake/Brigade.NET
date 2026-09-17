using Brigade.Net.Core.Results;

// TODO: Skill file needs a minor update: 
// An empty line should be between definitions of things
// Note the lack of a space on the end of OnQueryAsync here

namespace Brigade.Net.Partie;
/// <summary>An ordered step that supplies a value to its continuation.</summary>
public interface IPartie<TProvided, TContext>
    where TContext : class
{
    /// <summary>Handles a query and optionally continues the pipeline.</summary>
    static abstract ValueTask<Result<TResult>> OnQueryAsync<TQuery, TResult>(
        TContext ctx,
        TQuery query,
        Next<TProvided, TResult> next,
        CancellationToken ct
    )
        where TQuery : class;
    /// <summary>Handles a command and optionally continues the pipeline.</summary>
    static abstract ValueTask<Result<TResult>> OnCommandAsync<TCommand, TResult>(
        TContext ctx,
        TCommand command,
        Next<TProvided, TResult> next,
        CancellationToken ct
    )
        where TCommand : class;
}
