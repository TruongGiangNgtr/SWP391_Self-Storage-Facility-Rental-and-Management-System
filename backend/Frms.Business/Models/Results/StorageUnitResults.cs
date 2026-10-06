namespace Frms.Business.Models.Results;

public sealed record StorageUnitResult(
    Guid StorageUnitId,
    Guid FacilityId,
    Guid UnitTypeId,
    string UnitCode,
    string? LocationInfo,
    string Status);
