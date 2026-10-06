using Frms.DataAccess.Persistence.Entities;

namespace Frms.DataAccess.Repositories.Interfaces;

public interface IUnitTypeRepository {
    Task<(IReadOnlyList<UnitType> Items, int TotalItems)> GetPagedAsync(
        int page,
        int pageSize,
        CancellationToken cancellationToken = default);

    Task<UnitType?> GetByIdAsync(
        Guid unitTypeId,
        CancellationToken cancellationToken = default);

    Task SaveChangesAsync(
        CancellationToken cancellationToken = default);

    Task<(
    IReadOnlyList<UnitType> Items,
    int TotalItems,
    string? FacilityStatus)> GetFacilityPagedAsync(
        Guid facilityId,
        int page,
        int pageSize,
        CancellationToken cancellationToken = default);
}
