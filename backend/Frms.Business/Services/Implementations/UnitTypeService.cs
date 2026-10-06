using Frms.Business.Services.Interfaces;
using Frms.DataAccess.Persistence.Entities;
using Frms.DataAccess.Repositories.Interfaces;

namespace Frms.Business.Services.Implementations;

internal sealed class UnitTypeService(
    IUnitTypeRepository unitTypeRepository) : IUnitTypeService {
    public Task<(IReadOnlyList<UnitType> Items, int TotalItems)> GetPagedAsync(
        int page,
        int pageSize,
        CancellationToken cancellationToken = default)
        => unitTypeRepository.GetPagedAsync(
            page,
            pageSize,
            cancellationToken);

    public async Task<UnitType?> UpdatePriceAsync(
        Guid unitTypeId,
        decimal rentalPrice,
        CancellationToken cancellationToken = default) {
        var unitType = await unitTypeRepository.GetByIdAsync(
            unitTypeId,
            cancellationToken);

        if (unitType is null)
            return null;

        unitType.RentalPrice = rentalPrice;

        await unitTypeRepository.SaveChangesAsync(cancellationToken);

        return unitType;
    }

    public Task<(
    IReadOnlyList<UnitType> Items,
    int TotalItems,
    string? FacilityStatus)> GetFacilityPagedAsync(
        Guid facilityId,
        int page,
        int pageSize,
        CancellationToken cancellationToken = default)
    => unitTypeRepository.GetFacilityPagedAsync(
        facilityId,
        page,
        pageSize,
        cancellationToken);

    public Task<UnitType?> GetByIdAsync(
        Guid unitTypeId,
        CancellationToken cancellationToken = default)
        => unitTypeRepository.GetByIdAsync(
            unitTypeId,
            cancellationToken);
}
