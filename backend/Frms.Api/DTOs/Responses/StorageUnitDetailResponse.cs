namespace Frms.Api.DTOs.Responses;

/// <summary>Canonical StorageUnit response.</summary>
public sealed record StorageUnitDetailResponse(
    Guid StorageUnitId,
    Guid FacilityId,
    Guid UnitTypeId,
    string UnitCode,
    string? LocationInfo,
    string Status);
