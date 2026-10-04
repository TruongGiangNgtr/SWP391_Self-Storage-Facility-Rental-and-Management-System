namespace Frms.Api.DTOs.Responses;

/// <summary>Represents the stable API error envelope defined by FRMS SRS V10.</summary>
public sealed record ApiErrorResponse(
    string Code,
    string Message,
    string TraceId,
    IReadOnlyDictionary<string, string[]>? Errors = null);
