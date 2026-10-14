using Frms.Business.Abstractions.Security;
using Frms.Business.Exceptions;
using Frms.Business.Services.Interfaces;
using Frms.DataAccess.Repositories.Interfaces;
using Frms.DataAccess.Repositories.Models;

namespace Frms.Business.Services.Implementations;

internal sealed class OperationalCatalogService(
    IOperationalCatalogRepository repository, ICurrentUserContext user,
    IAuthenticationService authentication, IFacilityAuthorizationService facilityAuth) : IOperationalCatalogService
{
    public async Task<(IReadOnlyList<ExtraFeeTypeRecord> Items, int Total)> ListExtraFeeTypesAsync(
        int page, int pageSize, CancellationToken ct)
    {
        CheckPage(page, pageSize);
        await RequireActiveAsync(ct);
        if (user.Role != "BUSINESS_OPERATIONS_MANAGER")
        {
            if (user.Role is not ("FACILITY_STAFF" or "FACILITY_MANAGER")) throw Forbidden();
            // A facility assignment is mandatory, even though the catalogue is global.
            await facilityAuth.GetAssignedFacilityIdAsync(ct);
        }
        return await repository.ListExtraFeeTypesAsync(user.Role != "BUSINESS_OPERATIONS_MANAGER",
            page, pageSize, ct);
    }

    public async Task<ExtraFeeTypeRecord> UpdateExtraFeeTypeAsync(
        Guid id, decimal amount, string status, CancellationToken ct)
    {
        await RequireActiveAsync(ct);
        if (user.Role != "BUSINESS_OPERATIONS_MANAGER") throw Forbidden();
        if (amount < 0 || (status != "ACTIVE" && status != "INACTIVE"))
            throw new BusinessException("VALIDATION_ERROR", "Invalid ExtraFeeType update.", 400);
        return await repository.UpdateExtraFeeTypeAsync(id, amount, status, ct)
            ?? throw new BusinessException("RESOURCE_NOT_FOUND", "ExtraFeeType not found.", 404);
    }

    public async Task<(IReadOnlyList<PublicUnitTypeRecord> Items, int Total)> ListPublicUnitTypesAsync(
        string? mode, string? size, Guid? unitTypeId, string? sortBy, string? sortDirection,
        int page, int pageSize, CancellationToken ct)
    {
        CheckPage(page, pageSize);
        if (mode is not null && mode is not ("PUBLIC" or "PRIVATE") ||
            sortBy is not null && sortBy is not ("name" or "rentalPrice") ||
            sortDirection is not null && sortDirection is not ("asc" or "desc") ||
            size is { Length: > 150 })
            throw new BusinessException("VALIDATION_ERROR", "Unsupported public catalogue filter.", 400);
        return await repository.ListPublicUnitTypesAsync(mode, size, unitTypeId,
            sortBy ?? "name", sortDirection == "desc", page, pageSize, ct);
    }

    private async Task RequireActiveAsync(CancellationToken ct)
    {
        if (!user.IsAuthenticated) throw new BusinessException("UNAUTHORIZED", "Authentication is required.", 401);
        var account = await authentication.GetCurrentAccountAsync(user.UserAccountId, ct);
        if (account.Role != user.Role) throw Forbidden();
    }

    private static void CheckPage(int page, int pageSize)
    {
        if (page < 1 || pageSize is < 1 or > 100)
            throw new BusinessException("VALIDATION_ERROR", "Invalid pagination.", 400);
    }

    private static BusinessException Forbidden() => new("FORBIDDEN", "Access denied.", 403);
}
