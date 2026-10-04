namespace Frms.Api.DTOs.Responses;

/// <summary>Represents the common FRMS success envelope.</summary>
public sealed record ApiResponse<T>(T? Data, string Message);

/// <summary>Represents pagination metadata for collection responses.</summary>
public sealed record PaginationResponse(
    int Page,
    int PageSize,
    long TotalItems,
    int TotalPages);

/// <summary>Represents the common FRMS collection envelope.</summary>
public sealed record PaginatedResponse<T>(
    IReadOnlyList<T> Data,
    PaginationResponse Pagination);
