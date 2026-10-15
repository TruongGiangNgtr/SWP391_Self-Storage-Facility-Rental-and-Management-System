using Frms.DataAccess.Repositories.Models;

namespace Frms.DataAccess.Repositories.Interfaces;

public interface IFacilityCatalogRepository {
    Task<(IReadOnlyList<FacilityCatalogRecord> Items, int TotalItems)> SearchActiveAsync(
        string? location, Guid? facilityId, string? mode, string? size, Guid? unitTypeId,
        bool descending, int? skip, int? take, CancellationToken cancellationToken);

    Task<IReadOnlyDictionary<Guid, IReadOnlyList<Guid>>> GetMatchingOfferedUnitTypesAsync(
        IReadOnlyCollection<Guid> facilityIds, string? mode, string? size, Guid? unitTypeId,
        CancellationToken cancellationToken);

    Task<(IReadOnlyList<FacilityCatalogRecord> Items, int TotalItems)>
        GetActivePagedAsync(
            int skip,
            int take,
            CancellationToken cancellationToken = default);

    Task<FacilityCatalogRecord?> GetActiveByIdAsync(
        Guid facilityId,
        CancellationToken cancellationToken = default);
}
