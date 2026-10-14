using Frms.Business.Abstractions.Security;
using Frms.Business.Exceptions;
using Frms.Business.Services.Interfaces;
using Frms.DataAccess.Repositories.Interfaces;
using Frms.DataAccess.Repositories.Models;

namespace Frms.Business.Services.Implementations;

internal sealed class AdminReadService(IAdminReadRepository repository,
    ICurrentUserContext user, IAuthenticationService authentication) : IAdminReadService
{
    public async Task<(IReadOnlyList<LoginHistoryReadRecord> Items, int Total)> LoginHistoryAsync(
        Guid? userAccountId, string? status, DateTime? fromUtc, DateTime? toUtc,
        int page, int pageSize, CancellationToken ct)
    {
        await ValidateAsync(page, pageSize, fromUtc, toUtc, ct);
        if (status is not null && status is not ("SUCCESS" or "FAILED"))
            throw new BusinessException("VALIDATION_ERROR", "Invalid login status.", 400);
        return await repository.LoginHistoryAsync(userAccountId, status, fromUtc, toUtc, page, pageSize, ct);
    }

    public async Task<(IReadOnlyList<AuditLogReadRecord> Items, int Total)> AuditLogsAsync(
        Guid? userAccountId, string? entityType, Guid? entityId, string? action,
        DateTime? fromUtc, DateTime? toUtc, int page, int pageSize, CancellationToken ct)
    {
        await ValidateAsync(page, pageSize, fromUtc, toUtc, ct);
        return await repository.AuditLogsAsync(userAccountId, entityType, entityId, action,
            fromUtc, toUtc, page, pageSize, ct);
    }

    public async Task<(IReadOnlyList<FacilityLookupReadRecord> Items, int Total)> FacilityLookupAsync(
        int page, int pageSize, CancellationToken ct)
    {
        await ValidateAsync(page, pageSize, null, null, ct);
        return await repository.FacilityLookupAsync(page, pageSize, ct);
    }

    private async Task ValidateAsync(int page, int size, DateTime? from, DateTime? to, CancellationToken ct)
    {
        if (!user.IsAuthenticated)
            throw new BusinessException("UNAUTHORIZED", "Authentication is required.", 401);
        var current = await authentication.GetCurrentAccountAsync(user.UserAccountId, ct);
        if (user.Role != "SYSTEM_ADMINISTRATOR" || current.Role != "SYSTEM_ADMINISTRATOR")
            throw new BusinessException("FORBIDDEN", "Access denied.", 403);
        if (page < 1 || size is < 1 or > 100 || from.HasValue && to.HasValue && to < from)
            throw new BusinessException("VALIDATION_ERROR", "Invalid pagination or UTC date range.", 400);
    }
}
