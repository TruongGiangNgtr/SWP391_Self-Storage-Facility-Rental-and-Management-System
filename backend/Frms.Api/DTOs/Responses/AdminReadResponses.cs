namespace Frms.Api.DTOs.Responses;

public sealed record AdminLoginHistoryResponse(Guid LoginHistoryId, Guid UserAccountId,
    DateTime LoginAt, string? IpAddress, string? DeviceInfo, string Status);
public sealed record AdminAuditLogResponse(Guid AuditLogId, Guid? UserAccountId,
    string Action, string EntityType, Guid? EntityId, string? OldValue,
    string? NewValue, DateTime CreatedAt);
public sealed record AdminFacilityLookupResponse(Guid FacilityId, string Name, string Status);
