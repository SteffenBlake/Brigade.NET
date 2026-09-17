namespace Brigade.Net.Core.Results;

/// <summary>
/// A successful <see cref="Result{T}" /> whose value will stop being supported after a given date.
/// </summary>
/// <typeparam name="T">The type of the success value.</typeparam>
/// <param name="value">The success value.</param>
/// <param name="deprecatedAfterUtc">The UTC date and time after which this value is no longer supported.</param>
/// <param name="message">An optional human-readable explanation.</param>
[System.Text.Json.Serialization.JsonConverter(typeof(ResultJsonConverterFactory))]
public sealed class Deprecated<T>(T value, DateTime deprecatedAfterUtc, string? message = null) : Success<T>(value)
{
    /// <summary>
    /// The UTC date and time after which this value is no longer supported.
    /// </summary>
    public DateTime DeprecatedAfterUtc { get; } = deprecatedAfterUtc;

    /// <summary>
    /// An optional human-readable explanation.
    /// </summary>
    public string? Message { get; } = message;

    /// <inheritdoc />
    public override bool IsDeprecated(out Deprecated<T> deprecated)
    {
        deprecated = this;
        return true;
    }

    /// <summary>
    /// A deprecated result is not a success: only <see cref="IsDeprecated" /> is true for it.
    /// </summary>
    public override bool IsSuccess(out T success)
    {
        success = default!;
        return false;
    }

    /// <inheritdoc />
    public override Result<TOut> Map<TOut>(Func<T, TOut> mapper) => new Deprecated<TOut>(mapper(Value), DeprecatedAfterUtc, Message);

    /// <inheritdoc />
    public override async Task<Result<TOut>> MapAsync<TOut>(Func<T, Task<TOut>> mapper) =>
        new Deprecated<TOut>(await mapper(Value), DeprecatedAfterUtc, Message);
}
