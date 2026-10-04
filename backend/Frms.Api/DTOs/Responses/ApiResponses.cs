namespace Frms.Api.DTOs.Responses;

/// <summary>Successful API response envelope.</summary>
public sealed record ApiResponse<T>(T Data, string? Message = null);

/// <summary>Stable API error envelope.</summary>
public sealed record ApiErrorResponse(string Code, string Message, string TraceId, IReadOnlyDictionary<string, string[]>? Errors = null);

/// <summary>Authenticated account summary.</summary>
public sealed record AuthenticatedUserResponse(Guid UserAccountId, string Role, string Status);

/// <summary>FRMS JWT response.</summary>
public sealed record AuthTokenDataResponse(string AccessToken, string TokenType, AuthenticatedUserResponse User);

/// <summary>Authenticated account and profile response.</summary>
public sealed record CurrentAccountDataResponse(Guid UserAccountId, string Role, string Status, string Email, string PhoneNumber, CurrentProfileResponse? Profile);

/// <summary>Customer or employee profile attached to the current account.</summary>
public sealed record CurrentProfileResponse(Guid? CustomerId, Guid? EmployeeId, string? FullName, Guid? FacilityId);
