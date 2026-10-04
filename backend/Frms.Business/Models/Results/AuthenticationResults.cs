namespace Frms.Business.Models.Results;

public sealed record AuthenticatedUserResult(Guid UserAccountId, string Role, string Status);
public sealed record AuthenticationResult(string AccessToken, string TokenType, DateTimeOffset ExpiresAt, AuthenticatedUserResult User);
public sealed record CurrentAccountResult(Guid UserAccountId, string Role, string Status, string Email, string PhoneNumber, Guid? CustomerId, Guid? EmployeeId, Guid? FacilityId, string? FullName);
public sealed record IssuedToken(string AccessToken, DateTimeOffset ExpiresAt);
