namespace Brigade.Net.Core.Results;

/// <summary>A specific problem identified by a JSON Pointer.</summary>
/// <param name="Detail">A human-readable explanation of the problem.</param>
/// <param name="Pointer">A JSON Pointer locating the problem in the request.</param>
public sealed record ErrorDetail(string Detail, string Pointer);
