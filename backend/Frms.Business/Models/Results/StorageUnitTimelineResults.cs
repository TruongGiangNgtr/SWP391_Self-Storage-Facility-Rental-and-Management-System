namespace Frms.Business.Models.Results;

public sealed record StorageUnitStatusIntervalResult(string Status, DateTimeOffset From, DateTimeOffset To);

public sealed record StorageUnitTimelineResult(
    Guid StorageUnitId,
    string UnitCode,
    string UnitTypeName,
    string FacilityName,
    string? LocationInfo,
    IReadOnlyList<StorageUnitStatusIntervalResult> Intervals);
