using Frms.Business.Abstractions.Security;
using Frms.Business.Exceptions;
using Frms.Business.Services.Interfaces;
using Frms.DataAccess.Repositories.Interfaces;
using Frms.DataAccess.Repositories.Models;

namespace Frms.Business.Services.Implementations;

internal sealed class EmployeeDirectoryService(IEmployeeDirectoryRepository repository,
    ICurrentUserContext user, IAuthenticationService auth,
    IFacilityAuthorizationService facilityAuth) : IEmployeeDirectoryService
{
    public async Task<(IReadOnlyList<EmployeeDirectoryRecord> Items, int Total)> ListBusinessAsync(
        int page, int pageSize, CancellationToken ct)
    {
        await RequireAsync("BUSINESS_OPERATIONS_MANAGER", ct);
        CheckPage(page, pageSize);
        return await repository.ListAsync(null, null, false, page, pageSize, ct);
    }

    public async Task<EmployeeDirectoryRecord> GetBusinessAsync(Guid employeeId, CancellationToken ct)
    {
        await RequireAsync("BUSINESS_OPERATIONS_MANAGER", ct);
        return await repository.GetAsync(employeeId, ct) ??
            throw new BusinessException("RESOURCE_NOT_FOUND", "Employee not found.", 404);
    }

    public async Task<(IReadOnlyList<EmployeeDirectoryRecord> Items, int Total)> ListStaffAsync(
        Guid facilityId, int page, int pageSize, CancellationToken ct)
    {
        await RequireAsync("FACILITY_MANAGER", ct);
        await facilityAuth.EnsureSameFacilityAsync(facilityId, ct);
        CheckPage(page, pageSize);
        return await repository.ListAsync(facilityId, "FACILITY_STAFF", true, page, pageSize, ct);
    }

    private async Task RequireAsync(string role, CancellationToken ct)
    {
        if (!user.IsAuthenticated) throw new BusinessException("UNAUTHORIZED", "Authentication is required.", 401);
        var account = await auth.GetCurrentAccountAsync(user.UserAccountId, ct);
        if (user.Role != role || account.Role != role)
            throw new BusinessException("FORBIDDEN", "Access denied.", 403);
    }

    private static void CheckPage(int page, int size)
    {
        if (page < 1 || size is < 1 or > 100)
            throw new BusinessException("VALIDATION_ERROR", "Invalid pagination.", 400);
    }
}
