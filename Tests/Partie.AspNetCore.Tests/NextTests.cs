using Brigade.Net.Core.Results;

namespace Brigade.Net.Partie.AspNetCore.Tests;

public class NextTests
{
    [Fact]
    public async Task InvokeAsync_ReturnsResultFromDelegate()
    {
        Next<int, string> next = static value => ValueTask.FromResult<Result<string>>(value.ToString());

        var result = await next(42);

        Assert.True(result.IsSuccess(out var success));
        Assert.Equal("42", success);
    }
}
