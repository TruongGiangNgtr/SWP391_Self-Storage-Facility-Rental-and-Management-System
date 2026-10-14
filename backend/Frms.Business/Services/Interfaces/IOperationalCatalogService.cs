using Frms.DataAccess.Repositories.Models;

namespace Frms.Business.Services.Interfaces;

public interface IOperationalCatalogService
{
    Task<(IReadOnlyList<ExtraFeeTypeRecord> Items, int Total)> ListExtraFeeTypesAsync(
        int page, int pageSize, CancellationToken ct);
    Task<ExtraFeeTypeRecord> UpdateExtraFeeTypeAsync(Guid id, decimal amount, string status, CancellationToken ct);
    Task<(IReadOnlyList<PublicUnitTypeRecord> Items, int Total)> ListPublicUnitTypesAsync(
        string? mode, string? size, Guid? unitTypeId, string? sortBy, string? sortDirection,
        int page, int pageSize, CancellationToken ct);
}
