namespace Frms.Business.Models.Commands;

public sealed record CreateAdminEmployeeCommand(
    string FullName,
    string Email,
    string PhoneNumber,
    string Role,
    Guid? FacilityId);

public sealed record UpdateAdminEmployeeCommand(
    string FullName);
