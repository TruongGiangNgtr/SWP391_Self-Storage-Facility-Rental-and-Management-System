using Frms.DataAccess.Persistence.Entities;

namespace Frms.DataAccess.Repositories.Interfaces;

public interface ICapacityRepository
{
    Task<string?> GetFacilityStatusAsync(
        Guid facilityId,
        CancellationToken cancellationToken);

    Task<int> CountUnitTypesAsync(
        Guid facilityId,
        CancellationToken cancellationToken);

    Task<IReadOnlyList<UnitType>> ListUnitTypesAsync(
        Guid facilityId,
        int skip,
        int take,
        CancellationToken cancellationToken);

    Task<IReadOnlyDictionary<Guid, int>> GetAvailableCapacitiesAsync(
        Guid facilityId,
        IReadOnlyCollection<Guid> unitTypeIds,
        DateOnly startMonth,
        DateOnly endMonth,
        CancellationToken cancellationToken);
}