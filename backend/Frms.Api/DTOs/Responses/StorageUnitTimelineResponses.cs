namespace Frms.Api.DTOs.Responses;

public sealed record StorageUnitStatusIntervalResponse(string Status, DateTimeOffset From, DateTimeOffset To);

public sealed record StorageUnitTimelineResponse(
    Guid StorageUnitId,
    string UnitCode,
    string UnitTypeName,
    string FacilityName,
    string? LocationInfo,
    IReadOnlyList<StorageUnitStatusIntervalResponse> Intervals);
