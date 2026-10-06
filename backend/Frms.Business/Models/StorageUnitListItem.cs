namespace Frms.Business.Models;

public sealed record StorageUnitListItem(
    Guid StorageUnitId,
    Guid FacilityId,
    Guid UnitTypeId,
    string UnitCode,
    string? LocationInfo,
    string Status);