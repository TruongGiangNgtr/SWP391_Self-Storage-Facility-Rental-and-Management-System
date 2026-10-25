using System.Text.Json.Serialization;

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

/// <summary>Shared own-account profile fields. Customer details are owner-only.</summary>
[JsonDerivedType(typeof(CurrentCustomerProfileResponse))]
public record CurrentProfileResponse(Guid? CustomerId, Guid? EmployeeId, string? FullName, Guid? FacilityId);

/// <summary>Only the authenticated CUSTOMER's profile exposes the nullable stored CCCD.</summary>
public sealed record CurrentCustomerProfileResponse : CurrentProfileResponse
{
    public CurrentCustomerProfileResponse(Guid? customerId, string? fullName, string? cccd)
        : base(customerId, null, fullName, null) => Cccd = cccd;

    public string? Cccd { get; }
}
