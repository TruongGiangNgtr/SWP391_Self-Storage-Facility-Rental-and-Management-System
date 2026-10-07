using Frms.DataAccess.Repositories.Models;

namespace Frms.DataAccess.Repositories.Interfaces;

public interface IFacilityCatalogRepository {
    Task<(IReadOnlyList<FacilityCatalogRecord> Items, int TotalItems)>
        GetActivePagedAsync(
            int skip,
            int take,
            CancellationToken cancellationToken = default);

    Task<FacilityCatalogRecord?> GetActiveByIdAsync(
        Guid facilityId,
        CancellationToken cancellationToken = default);
}
