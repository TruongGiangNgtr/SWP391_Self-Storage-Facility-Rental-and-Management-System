using Frms.DataAccess.Persistence;
using Frms.DataAccess.Repositories.Interfaces;
using Frms.DataAccess.Repositories.Models;
using Microsoft.EntityFrameworkCore;

namespace Frms.DataAccess.Repositories.Implementations;

internal sealed class AdminReadRepository(FrmsDbContext db) : IAdminReadRepository
{
    public async Task<(IReadOnlyList<LoginHistoryReadRecord> Items, int Total)> LoginHistoryAsync(
        Guid? userAccountId, string? status, DateTime? fromUtc, DateTime? toUtc,
        int page, int pageSize, CancellationToken ct)
    {
        var q = db.LoginHistory.AsNoTracking().AsQueryable();
        if (userAccountId.HasValue) q = q.Where(x => x.UserAccountId == userAccountId.Value);
        if (status is not null) q = q.Where(x => x.Status == status);
        if (fromUtc.HasValue) q = q.Where(x => x.LoginAt >= fromUtc.Value);
        if (toUtc.HasValue) q = q.Where(x => x.LoginAt <= toUtc.Value);
        var total = await q.CountAsync(ct);
        var items = await q.OrderByDescending(x => x.LoginAt).ThenByDescending(x => x.LoginHistoryId)
            .Skip((page - 1) * pageSize).Take(pageSize)
            .Select(x => new LoginHistoryReadRecord(x.LoginHistoryId, x.UserAccountId,
                x.LoginAt, x.IpAddress, x.DeviceInfo, x.Status)).ToArrayAsync(ct);
        return (items, total);
    }

    public async Task<(IReadOnlyList<AuditLogReadRecord> Items, int Total)> AuditLogsAsync(
        Guid? userAccountId, string? entityType, Guid? entityId, string? action,
        DateTime? fromUtc, DateTime? toUtc, int page, int pageSize, CancellationToken ct)
    {
        var q = db.AuditLogs.AsNoTracking().AsQueryable();
        if (userAccountId.HasValue) q = q.Where(x => x.UserAccountId == userAccountId.Value);
        if (entityType is not null) q = q.Where(x => x.EntityType == entityType);
        if (entityId.HasValue) q = q.Where(x => x.EntityId == entityId.Value);
        if (action is not null) q = q.Where(x => x.Action == action);
        if (fromUtc.HasValue) q = q.Where(x => x.CreatedAt >= fromUtc.Value);
        if (toUtc.HasValue) q = q.Where(x => x.CreatedAt <= toUtc.Value);
        var total = await q.CountAsync(ct);
        var items = await q.OrderByDescending(x => x.CreatedAt).ThenByDescending(x => x.AuditLogId)
            .Skip((page - 1) * pageSize).Take(pageSize)
            .Select(x => new AuditLogReadRecord(x.AuditLogId, x.UserAccountId, x.Action,
                x.EntityType, x.EntityId, x.OldValue, x.NewValue, x.CreatedAt)).ToArrayAsync(ct);
        return (items, total);
    }

    public async Task<(IReadOnlyList<FacilityLookupReadRecord> Items, int Total)> FacilityLookupAsync(
        int page, int pageSize, CancellationToken ct)
    {
        var q = db.Facilities.AsNoTracking();
        var total = await q.CountAsync(ct);
        var items = await q.OrderBy(x => x.Name).ThenBy(x => x.FacilityId)
            .Skip((page - 1) * pageSize).Take(pageSize)
            .Select(x => new FacilityLookupReadRecord(x.FacilityId, x.Name, x.Status))
            .ToArrayAsync(ct);
        return (items, total);
    }
}
