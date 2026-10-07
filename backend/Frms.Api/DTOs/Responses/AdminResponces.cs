namespace Frms.Api.DTOs.Responses;

public sealed record AdminUserAccountResponse(
    Guid UserAccountId,
    string Role,
    string Status,
    string Email,
    string PhoneNumber,
    DateTime CreatedAt,
    AdminUserProfileResponse? Profile);

public sealed record AdminUserProfileResponse(
    Guid? CustomerId,
    Guid? EmployeeId,
    string FullName,
    string? Address,
    string? Cccd,
    Guid? FacilityId);
