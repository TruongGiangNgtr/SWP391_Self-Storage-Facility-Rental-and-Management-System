using Frms.DataAccess.Repositories.Models;

namespace Frms.DataAccess.Repositories.Interfaces;

public interface IOperationalCatalogRepository
{
    Task<(IReadOnlyList<ExtraFeeTypeRecord> Items, int Total)> ListExtraFeeTypesAsync(
        bool activeOnly, int page, int pageSize, CancellationToken ct);
    Task<ExtraFeeTypeRecord?> UpdateExtraFeeTypeAsync(Guid id, decimal amount, string status, CancellationToken ct);
    Task<(IReadOnlyList<PublicUnitTypeRecord> Items, int Total)> ListPublicUnitTypesAsync(
        string? mode, string? size, Guid? unitTypeId, string sortBy, bool descending,
        int page, int pageSize, CancellationToken ct);
}
