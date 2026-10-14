using Frms.DataAccess.Repositories.Models;

namespace Frms.DataAccess.Repositories.Interfaces;

public interface IAdminReadRepository
{
    Task<(IReadOnlyList<LoginHistoryReadRecord> Items, int Total)> LoginHistoryAsync(
        Guid? userAccountId, string? status, DateTime? fromUtc, DateTime? toUtc,
        int page, int pageSize, CancellationToken ct);
    Task<(IReadOnlyList<AuditLogReadRecord> Items, int Total)> AuditLogsAsync(
        Guid? userAccountId, string? entityType, Guid? entityId, string? action,
        DateTime? fromUtc, DateTime? toUtc, int page, int pageSize, CancellationToken ct);
    Task<(IReadOnlyList<FacilityLookupReadRecord> Items, int Total)> FacilityLookupAsync(
        int page, int pageSize, CancellationToken ct);
}
