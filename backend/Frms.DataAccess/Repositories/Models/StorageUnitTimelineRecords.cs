namespace Frms.DataAccess.Repositories.Models;

public sealed record StorageUnitTimelineUnitRecord(
    Guid StorageUnitId, string UnitCode, string UnitTypeName,
    string FacilityName, string? LocationInfo);

public sealed record StorageUnitStatusAuditRecord(
    Guid AuditLogId, Guid StorageUnitId, string Action,
    string? OldValue, string? NewValue, DateTime CreatedAt);
