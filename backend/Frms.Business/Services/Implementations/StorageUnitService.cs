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

    public async Task UpdateAsync(
        Guid storageUnitId,
        UpdateStorageUnitCommand command,
        CancellationToken cancellationToken = default)
    {
        var record = await repository.GetByIdAsync(
            storageUnitId,
            cancellationToken);

        if (record is null)
        {
            throw new BusinessException(
                "STORAGE_UNIT_NOT_FOUND",
                "Storage unit was not found.",
                404);
        }

        await facilityAuthorizationService.EnsureSameFacilityAsync(
            record.FacilityId,
            cancellationToken);

        if (command.UnitTypeId != record.UnitTypeId)
        {
            if (!await repository.UnitTypeExistsAsync(
                    command.UnitTypeId,
                    cancellationToken))
            {
                throw new BusinessException(
                    "UNIT_TYPE_NOT_FOUND",
                    "Unit type was not found.",
                    404);
            }

            if (record.Status is "IN_USE" or "INSPECTION")
            {
                throw new BusinessException(
                    "UNIT_TYPE_CHANGE_NOT_ALLOWED",
                    "Unit type cannot be changed while the storage unit is in use or inspection.",
                    409);
            }

            if (await repository.HasActiveContractAsync(
                    storageUnitId,
                    cancellationToken))
            {
                throw new BusinessException(
                    "UNIT_TYPE_CHANGE_NOT_ALLOWED",
                    "Unit type cannot be changed while the storage unit has an active contract.",
                    409);
            }
        }

        await repository.UpdateAsync(
            storageUnitId,
            command.UnitTypeId,
            command.LocationInfo?.Trim(),
            cancellationToken);
    }

    public async Task ChangeStatusAsync(
        Guid storageUnitId,
        ChangeStorageUnitStatusCommand command,
        CancellationToken cancellationToken = default)
    {
        var record = await repository.GetByIdAsync(
            storageUnitId,
            cancellationToken);

        if (record is null)
        {
            throw new BusinessException(
                "STORAGE_UNIT_NOT_FOUND",
                "Storage unit was not found.",
                404);
        }

        await facilityAuthorizationService.EnsureSameFacilityAsync(
            record.FacilityId,
            cancellationToken);

        var targetStatus = command.Status.Trim().ToUpperInvariant();

        var isValidTransition =
            (record.Status == "AVAILABLE" && targetStatus == "IN_USE") ||
            (record.Status == "AVAILABLE" && targetStatus == "MAINTENANCE") ||
            (record.Status == "IN_USE" && targetStatus == "INSPECTION") ||
            (record.Status == "INSPECTION" && targetStatus == "AVAILABLE") ||
            (record.Status == "INSPECTION" && targetStatus == "MAINTENANCE") ||
            (record.Status == "MAINTENANCE" && targetStatus == "AVAILABLE");

        if (!isValidTransition)
        {
            throw new BusinessException(
                "STORAGE_UNIT_INVALID_TRANSITION",
                $"Storage unit cannot transition from {record.Status} to {targetStatus}.",
                409);
        }

        if (record.Status == "AVAILABLE"
            && targetStatus == "MAINTENANCE"
            && await repository.HasActiveContractAsync(
                storageUnitId,
                cancellationToken))
        {
            throw new BusinessException(
                "STORAGE_UNIT_ACTIVE_CONTRACT",
                "Storage unit with an active contract cannot enter maintenance.",
                409);
        }

        await repository.UpdateStatusAsync(
            storageUnitId,
            targetStatus,
            cancellationToken);
    }
}
