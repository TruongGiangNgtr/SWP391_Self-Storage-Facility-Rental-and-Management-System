namespace Frms.DataAccess.Repositories.Models;

public sealed record AuthenticationAccount(
    Guid UserAccountId,
    string Role,
    string Status,
    string Email,
    string PhoneNumber,
    string PasswordHash,
    Guid? CustomerId,
    Guid? EmployeeId,
    Guid? FacilityId,
    string? FullName);
