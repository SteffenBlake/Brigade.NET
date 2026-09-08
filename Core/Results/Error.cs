namespace Brigade.Net.Core.Results;

/// <summary>
/// A failure payload mirroring the RFC 7807 ProblemDetails shape.
/// </summary>
/// <param name="Type">A URI reference identifying the problem type.</param>
/// <param name="Title">A short, human-readable summary of the problem.</param>
/// <param name="Status">The HTTP status code associated with the problem.</param>
/// <param name="Detail">A human-readable explanation specific to this occurrence of the problem.</param>
/// <param name="Instance">A URI reference identifying the specific occurrence of the problem.</param>
/// <param name="Extensions">Additional problem-specific data.</param>
public sealed class Error(
    string? Type = null,
    string? Title = null,
    int? Status = null,
    string? Detail = null,
    string? Instance = null,
    IDictionary<string, object?>? Extensions = null) : FailureBase
{
    /// <summary>
    /// A URI reference identifying the problem type.
    /// </summary>
    public string? Type { get; } = Type;

    /// <summary>
    /// A short, human-readable summary of the problem.
    /// </summary>
    public string? Title { get; } = Title;

    /// <summary>
    /// The HTTP status code associated with the problem.
    /// </summary>
    public int? Status { get; } = Status;

    /// <summary>
    /// A human-readable explanation specific to this occurrence of the problem.
    /// </summary>
    public string? Detail { get; } = Detail;

    /// <summary>
    /// A URI reference identifying the specific occurrence of the problem.
    /// </summary>
    public string? Instance { get; } = Instance;

    /// <summary>
    /// Additional problem-specific data.
    /// </summary>
    public IDictionary<string, object?>? Extensions { get; } = Extensions;

    /// <inheritdoc />
    public override bool IsError(out Error error)
    {
        error = this;
        return true;
    }

    /// <inheritdoc />
    public override string ToString() =>
        $"Error {{ Type = {Type}, Title = {Title}, Status = {Status}, Detail = {Detail}, Instance = {Instance} }}";
}
