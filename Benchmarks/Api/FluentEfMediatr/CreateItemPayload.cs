namespace Brigade.Net.Benchmarks.Api.FluentEfMediatr;

public sealed record CreateItemPayload(string? Title, int CategoryId, int Score);
