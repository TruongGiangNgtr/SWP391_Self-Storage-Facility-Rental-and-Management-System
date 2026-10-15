using Frms.Business.Models.Results;
using Frms.DataAccess.Repositories.Models;

namespace Frms.Business.Services.Interfaces;

public interface IOperationalCatalogService
{
    Task<(IReadOnlyList<ExtraFeeTypeRecord> Items, int Total)> ListExtraFeeTypesAsync(
        int page, int pageSize, CancellationToken ct);
    Task<ExtraFeeTypeRecord> UpdateExtraFeeTypeAsync(Guid id, decimal amount, string status, CancellationToken ct);
    Task<CapacityPageResult> ListFacilityPublicUnitTypesAsync(
        Guid facilityId, string? mode, string? size, Guid? unitTypeId,
        string? startMonth, string? endMonth, string? sortBy, string? sortDirection,
        string? availableOnly, int page, int pageSize, CancellationToken ct);
    Task<(IReadOnlyList<PublicUnitTypeRecord> Items, int Total)> ListPublicUnitTypesAsync(
        string? mode, string? size, Guid? unitTypeId, string? sortBy, string? sortDirection,
        int page, int pageSize, CancellationToken ct);
}
