namespace Frms.DataAccess.Repositories.Models;

public sealed record StorageUnitRecord(
    Guid StorageUnitId,
    Guid FacilityId,
    Guid UnitTypeId,
    string UnitCode,
    string? LocationInfo,
    string Status);