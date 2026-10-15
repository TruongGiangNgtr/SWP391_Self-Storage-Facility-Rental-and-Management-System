namespace Frms.DataAccess.Repositories.Models;

public sealed record LoginHistoryReadRecord(Guid LoginHistoryId, Guid UserAccountId,
    DateTime LoginAt, string? IpAddress, string? DeviceInfo, string Status);
public sealed record AuditLogReadRecord(Guid AuditLogId, Guid? UserAccountId,
    string Action, string EntityType, Guid? EntityId, string? OldValue,
    string? NewValue, DateTime CreatedAt);
public sealed record FacilityLookupReadRecord(Guid FacilityId, string Name, string Status);
