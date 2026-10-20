namespace Frms.DataAccess.Repositories.Models;

public sealed record ExtraFeeTypeRecord(Guid ExtraFeeTypeId, string Name, decimal DefaultAmount, string Status);
public sealed record PublicUnitTypeRecord(Guid UnitTypeId, string Name, string Mode, string Size,
    decimal RentalPrice, string? Description, string? ImageUrl = null, string? VideoUrl = null);
public sealed record EmployeeDirectoryRecord(Guid EmployeeId, Guid? FacilityId, string FullName,
    string Role, string Status);
