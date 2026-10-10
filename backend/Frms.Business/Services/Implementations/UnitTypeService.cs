using Frms.Business.Models.Results;
using Frms.Business.Services.Interfaces;
using Frms.DataAccess.Persistence.Entities;
using Frms.DataAccess.Repositories.Interfaces;

namespace Frms.Business.Services.Implementations;

internal sealed class UnitTypeService(
    IUnitTypeRepository unitTypeRepository) : IUnitTypeService {
    private Task<(IReadOnlyList<UnitType> Items, int TotalItems)> GetPagedCoreAsync(
        int page,
        int pageSize,
        CancellationToken cancellationToken = default)
        => unitTypeRepository.GetPagedAsync(
            page,
            pageSize,
            cancellationToken);

    private async Task<UnitType?> UpdatePriceCoreAsync(
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

    private Task<(
    IReadOnlyList<UnitType> Items,
    int TotalItems,
    string? FacilityStatus)> GetFacilityPagedCoreAsync(
        Guid facilityId,
        int page,
        int pageSize,
        CancellationToken cancellationToken = default)
    => unitTypeRepository.GetFacilityPagedAsync(
        facilityId,
        page,
        pageSize,
        cancellationToken);

    private Task<UnitType?> GetByIdCoreAsync(
        Guid unitTypeId,
        CancellationToken cancellationToken = default)
        => unitTypeRepository.GetByIdAsync(
            unitTypeId,
            cancellationToken);

    public async Task<(IReadOnlyList<UnitTypeResult> Items, int TotalItems)> GetPagedAsync(int page, int pageSize, CancellationToken cancellationToken = default)
    {
        var row = await GetPagedCoreAsync(page, pageSize, cancellationToken);
        return (row.Items.Select(ServiceResultProjection.Map).ToArray(), row.TotalItems);
    }

    public async Task<UnitTypeResult?> UpdatePriceAsync(Guid unitTypeId, decimal rentalPrice, CancellationToken cancellationToken = default)
    {
        var row = await UpdatePriceCoreAsync(unitTypeId, rentalPrice, cancellationToken);
        return row is null ? null : ServiceResultProjection.Map(row);
    }

    public async Task<(IReadOnlyList<UnitTypeResult> Items, int TotalItems, string? FacilityStatus)> GetFacilityPagedAsync(Guid facilityId, int page, int pageSize, CancellationToken cancellationToken = default)
    {
        var row = await GetFacilityPagedCoreAsync(facilityId, page, pageSize, cancellationToken);
        return (row.Items.Select(ServiceResultProjection.Map).ToArray(), row.TotalItems, row.FacilityStatus);
    }

    public async Task<UnitTypeResult?> GetByIdAsync(Guid unitTypeId, CancellationToken cancellationToken = default)
    {
        var row = await GetByIdCoreAsync(unitTypeId, cancellationToken);
        return row is null ? null : ServiceResultProjection.Map(row);
    }
}
