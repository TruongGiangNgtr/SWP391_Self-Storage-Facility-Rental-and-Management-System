using Frms.DataAccess.Persistence.Entities;

namespace Frms.DataAccess.Repositories.Interfaces;

public interface IFacilityRepository {
    Task<(IReadOnlyList<Facility> Items, int TotalItems)> GetPagedAsync(
        int page,
        int pageSize,
        CancellationToken cancellationToken = default);

    Task<Facility?> GetByIdAsync(
        Guid facilityId,
        CancellationToken cancellationToken = default);

    Task AddAsync(
        Facility facility,
        CancellationToken cancellationToken = default);

    Task SaveChangesAsync(
        CancellationToken cancellationToken = default);
}
