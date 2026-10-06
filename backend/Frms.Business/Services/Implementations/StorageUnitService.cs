using Frms.Business.Exceptions;
using Frms.Business.Models.Commands;
using Frms.Business.Services.Interfaces;
using Frms.DataAccess.Repositories.Interfaces;
using Frms.Business.Models;

namespace Frms.Business.Services.Implementations;

internal sealed class StorageUnitService(
    IStorageUnitRepository repository,
    IFacilityAuthorizationService facilityAuthorizationService)
    : IStorageUnitService {
    public async Task<Guid> CreateAsync(
        Guid facilityId,
        CreateStorageUnitCommand command,
        CancellationToken cancellationToken = default) {
        await facilityAuthorizationService.EnsureSameFacilityAsync(
            facilityId,
            cancellationToken);

        if (!await repository.FacilityExistsAsync(
                facilityId,
                cancellationToken)) {
            throw new BusinessException(
                "FACILITY_NOT_FOUND",
                "Facility was not found.",
                404);
        }

        if (!await repository.UnitTypeExistsAsync(
                command.UnitTypeId,
                cancellationToken)) {
            throw new BusinessException(
                "UNIT_TYPE_NOT_FOUND",
                "Unit type was not found.",
                404);
        }

        return await repository.CreateAsync(
            facilityId,
            command.UnitTypeId,
            command.UnitCode.Trim(),
            command.LocationInfo?.Trim(),
            cancellationToken);
    }

    public async Task<IReadOnlyList<StorageUnitListItem>> ListByFacilityAsync(
        Guid facilityId,
        CancellationToken cancellationToken = default)
    {
        await facilityAuthorizationService.EnsureSameFacilityAsync(
            facilityId,
            cancellationToken);

        var records = await repository.ListByFacilityAsync(
            facilityId,
            cancellationToken);

        return records
            .Select(x => new StorageUnitListItem(
                x.StorageUnitId,
                x.FacilityId,
                x.UnitTypeId,
                x.UnitCode,
                x.LocationInfo,
                x.Status))
            .ToList();
    }

    public async Task<StorageUnitListItem?> GetByIdAsync(
        Guid storageUnitId,
        CancellationToken cancellationToken = default)
    {
        var record = await repository.GetByIdAsync(
            storageUnitId,
            cancellationToken);

        if (record is null)
        {
            return null;
        }

        await facilityAuthorizationService.EnsureSameFacilityAsync(
            record.FacilityId,
            cancellationToken);

        return new StorageUnitListItem(
            record.StorageUnitId,
            record.FacilityId,
            record.UnitTypeId,
            record.UnitCode,
            record.LocationInfo,
            record.Status);
    }
}
