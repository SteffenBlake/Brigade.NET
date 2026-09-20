using Brigade.Net.Core.Results;
using Brigade.Net.Partie;

namespace Brigade.Net.Partie.Extensions.Expo.Tests;

public sealed class ExpoValidationPartieTests
{
    [Theory]
    [InlineData(false)]
    [InlineData(true)]
    public async Task ValidRequestInvokesNext(bool command)
    {
        var nextCalls = 0;
        Next<Unit, int> next = _ =>
        {
            nextCalls++;
            return ValueTask.FromResult<Result<int>>(42);
        };

        var result = command
            ? await ExpoValidationPartie<ValidationPartieRequest, int>.OnCommandAsync(
                Unit.Default,
                new(true),
                next,
                CancellationToken.None
            )
            : await ExpoValidationPartie<ValidationPartieRequest, int>.OnQueryAsync(
                Unit.Default,
                new(true),
                next,
                CancellationToken.None
            );

        int? value = null;
        result.Map(success => value = success);
        Assert.Equal(42, value);
        Assert.Equal(1, nextCalls);
    }

    [Theory]
    [InlineData(false)]
    [InlineData(true)]
    public async Task InvalidRequestReturnsOneErrorAndDoesNotInvokeNext(bool command)
    {
        var nextCalls = 0;
        Next<Unit, int> next = _ =>
        {
            nextCalls++;
            return ValueTask.FromResult<Result<int>>(42);
        };

        var result = command
            ? await ExpoValidationPartie<ValidationPartieRequest, int>.OnCommandAsync(
                Unit.Default,
                new(false),
                next,
                CancellationToken.None
            )
            : await ExpoValidationPartie<ValidationPartieRequest, int>.OnQueryAsync(
                Unit.Default,
                new(false),
                next,
                CancellationToken.None
            );

        Error? error = null;
        result.Map(success: _ => false, error: value =>
        {
            error = value;
            return true;
        });
        Assert.NotNull(error);
        Assert.Collection(
            error.ErrorDetails,
            detail => Assert.Equal("/first", detail.Pointer),
            detail => Assert.Equal("/second", detail.Pointer)
        );
        Assert.Equal(0, nextCalls);
    }
}
