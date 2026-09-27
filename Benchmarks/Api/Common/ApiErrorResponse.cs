namespace Brigade.Net.Benchmarks.Api.Common;

public sealed record ApiErrorResponse(
    string? Type,
    string? Title,
    int? Status,
    string? Detail,
    string? Instance,
    object? Extensions,
    IReadOnlyList<ApiErrorDetail> ErrorDetails
);
