namespace Brigade.Net.Core.Results;

/// <summary>
/// A successful <see cref="Result{T}" /> carrying a value.
/// </summary>
/// <typeparam name="T">The type of the success value.</typeparam>
/// <param name="value">The success value.</param>
public class Success<T>(T value) : Result<T>
{
    /// <summary>
    /// The success value.
    /// </summary>
    public T Value { get; } = value;

    /// <inheritdoc />
    public override string ToString() => Value?.ToString() ?? string.Empty;

    /// <inheritdoc />
    public override bool IsSuccess(out T success)
    {
        success = Value;
        return true;
    }

    /// <inheritdoc />
    public override Result<TOut> Map<TOut>(Func<T, TOut> mapper) => new Success<TOut>(mapper(Value));

    /// <inheritdoc />
    public override async Task<Result<TOut>> MapAsync<TOut>(Func<T, Task<TOut>> mapper) => new Success<TOut>(await mapper(Value));
}
