using Frms.Business.Exceptions;
using Frms.Business.Models;
using Frms.Business.Models.Commands;
using Frms.Business.Models.Results;
using Frms.Business.Services.Interfaces;
using Frms.DataAccess.Persistence.Entities;
using Frms.DataAccess.Repositories.Interfaces;

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

        if (command.UnitTypeId == Guid.Empty) {
            throw new BusinessException(
                "UNIT_TYPE_REQUIRED",
                "Unit type is required.",
                400);
        }

        if (string.IsNullOrWhiteSpace(command.UnitCode)) {
            throw new BusinessException(
                "UNIT_CODE_REQUIRED",
                "Unit code is required.",
                400);
        }

        if (!await repository.UnitTypeExistsAsync(
                command.UnitTypeId,
                cancellationToken)) {
            throw new BusinessException(
                "UNIT_TYPE_NOT_FOUND",
                "Unit type was not found.",
                404);
        }

        var unitCode = command.UnitCode.Trim();

        if (await repository.UnitCodeExistsAsync(
                facilityId,
                unitCode,
                cancellationToken)) {
            throw new BusinessException(
                "UNIT_CODE_ALREADY_EXISTS",
                "A storage unit with this unit code already exists in the facility.",
                409);
        }

        var locationInfo = NormalizeOptionalText(
            command.LocationInfo);

        return await repository.CreateAsync(
            facilityId,
            command.UnitTypeId,
            unitCode,
            locationInfo,
            cancellationToken);
    }

    public async Task<(IReadOnlyList<StorageUnitResult> Items, int TotalCount)>
        ListByFacilityAsync(
            Guid facilityId,
            int page,
            int pageSize,
            CancellationToken cancellationToken = default) {
        ValidatePagination(page, pageSize);

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

        var (items, totalCount) =
            await repository.ListByFacilityAsync(
                facilityId,
                page,
                pageSize,
                cancellationToken);

        return (
            items.Select(ToResult).ToList(),
            totalCount);
    }

    public async Task<StorageUnitResult> GetByIdAsync(
        Guid storageUnitId,
        CancellationToken cancellationToken = default) {
        var storageUnit =
            await GetAuthorizedStorageUnitAsync(
                storageUnitId,
                cancellationToken);

        return ToResult(storageUnit);
    }

    public async Task<StorageUnitResult> UpdateAsync(
        Guid storageUnitId,
        UpdateStorageUnitCommand command,
        CancellationToken cancellationToken = default) {
        var storageUnit =
            await GetAuthorizedStorageUnitAsync(
                storageUnitId,
                cancellationToken);

        if (command.UnitTypeId == Guid.Empty) {
            throw new BusinessException(
                "UNIT_TYPE_REQUIRED",
                "Unit type is required.",
                400);
        }

        var unitTypeChanged =
            command.UnitTypeId != storageUnit.UnitTypeId;

        if (unitTypeChanged) {
            if (!await repository.UnitTypeExistsAsync(
                    command.UnitTypeId,
                    cancellationToken)) {
                throw new BusinessException(
                    "UNIT_TYPE_NOT_FOUND",
                    "Unit type was not found.",
                    404);
            }

            if (storageUnit.Status is "IN_USE" or "INSPECTION") {
                throw new BusinessException(
                    "UNIT_TYPE_CHANGE_NOT_ALLOWED",
                    "Unit type cannot be changed while the storage unit is in use or under inspection.",
                    409);
            }

            if (await repository.HasActiveContractAsync(
                    storageUnitId,
                    cancellationToken)) {
                throw new BusinessException(
                    "UNIT_TYPE_CHANGE_NOT_ALLOWED",
                    "Unit type cannot be changed while an active contract occupies the storage unit.",
                    409);
            }
        }

        var locationInfo =
            NormalizeOptionalText(command.LocationInfo);

        await repository.UpdateAsync(
            storageUnitId,
            command.UnitTypeId,
            locationInfo,
            cancellationToken);

        return new StorageUnitResult(
            storageUnit.StorageUnitId,
            storageUnit.FacilityId,
            command.UnitTypeId,
            storageUnit.UnitCode,
            locationInfo,
            storageUnit.Status);
    }

    public async Task<StorageUnitResult> ChangeStatusAsync(
        Guid storageUnitId,
        ChangeStorageUnitStatusCommand command,
        CancellationToken cancellationToken = default) {
        var storageUnit =
            await GetAuthorizedStorageUnitAsync(
                storageUnitId,
                cancellationToken);

        if (string.IsNullOrWhiteSpace(command.Status)) {
            throw new BusinessException(
                "STORAGE_UNIT_STATUS_REQUIRED",
                "Storage unit status is required.",
                400);
        }

        var targetStatus =
            command.Status.Trim().ToUpperInvariant();

        if (targetStatus is not ("AVAILABLE" or "MAINTENANCE")) {
            throw new BusinessException(
                "STORAGE_UNIT_STATUS_NOT_ALLOWED",
                "Facility Manager may directly manage only AVAILABLE and MAINTENANCE operational states.",
                409);
        }

        var validManagerTransition =
            (storageUnit.Status == "AVAILABLE"
             && targetStatus == "MAINTENANCE")
            ||
            (storageUnit.Status == "MAINTENANCE"
             && targetStatus == "AVAILABLE");

        if (!validManagerTransition) {
            throw new BusinessException(
                "STORAGE_UNIT_STATUS_TRANSITION_INVALID",
                $"Storage unit cannot transition from {storageUnit.Status} to {targetStatus} through this operation.",
                409);
        }

        if (await repository.HasActiveContractAsync(
                storageUnitId,
                cancellationToken)) {
            throw new BusinessException(
                "STORAGE_UNIT_OCCUPIED",
                "Storage unit has an active contract and its status cannot be changed manually.",
                409);
        }

        await repository.UpdateStatusAsync(
            storageUnitId,
            targetStatus,
            cancellationToken);

        return new StorageUnitResult(
            storageUnit.StorageUnitId,
            storageUnit.FacilityId,
            storageUnit.UnitTypeId,
            storageUnit.UnitCode,
            storageUnit.LocationInfo,
            targetStatus);
    }

    private async Task<StorageUnit> GetAuthorizedStorageUnitAsync(
        Guid storageUnitId,
        CancellationToken cancellationToken) {
        var storageUnit =
            await repository.FindByIdAsync(
                storageUnitId,
                cancellationToken);

        if (storageUnit is null) {
            throw new BusinessException(
                "STORAGE_UNIT_NOT_FOUND",
                "Storage unit was not found.",
                404);
        }

        await facilityAuthorizationService.EnsureSameFacilityAsync(
            storageUnit.FacilityId,
            cancellationToken);

        return storageUnit;
    }

    private static StorageUnitResult ToResult(
        StorageUnit storageUnit) =>
        new(
            storageUnit.StorageUnitId,
            storageUnit.FacilityId,
            storageUnit.UnitTypeId,
            storageUnit.UnitCode,
            storageUnit.LocationInfo,
            storageUnit.Status);

    private static string? NormalizeOptionalText(
        string? value) =>
        string.IsNullOrWhiteSpace(value)
            ? null
            : value.Trim();

    private static void ValidatePagination(
        int page,
        int pageSize) {
        if (page < 1) {
            throw new BusinessException(
                "INVALID_PAGINATION",
                "Page must be greater than or equal to 1.",
                400);
        }

        if (pageSize < 1 || pageSize > 100) {
            throw new BusinessException(
                "INVALID_PAGINATION",
                "Page size must be between 1 and 100.",
                400);
        }
    }

    public async Task<
    (IReadOnlyList<StorageUnitResult> Items, int TotalCount)> ListByFacilityAsync(
        Guid facilityId,
        Guid? unitTypeId,
        string? status,
        int page,
        int pageSize,
        CancellationToken cancellationToken = default) {
        ValidatePagination(page, pageSize);

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

        if (unitTypeId == Guid.Empty) {
            throw new BusinessException(
                "VALIDATION_ERROR",
                "unitTypeId is invalid.",
                400);
        }

        var normalizedStatus =
            string.IsNullOrWhiteSpace(status)
                ? null
                : status.Trim().ToUpperInvariant();

        if (normalizedStatus is not null &&
            normalizedStatus != "AVAILABLE" &&
            normalizedStatus != "IN_USE" &&
            normalizedStatus != "INSPECTION" &&
            normalizedStatus != "MAINTENANCE") {
            throw new BusinessException(
                "VALIDATION_ERROR",
                "StorageUnit status is invalid.",
                400);
        }

        var totalCount =
            await repository.CountByFacilityAsync(
                facilityId,
                unitTypeId,
                normalizedStatus,
                cancellationToken);

        var records =
            await repository.ListByFacilityPageAsync(
                facilityId,
                unitTypeId,
                normalizedStatus,
                (page - 1) * pageSize,
                pageSize,
                cancellationToken);

        var items = records
            .Select(x => new StorageUnitResult(
                x.StorageUnitId,
                x.FacilityId,
                x.UnitTypeId,
                x.UnitCode,
                x.LocationInfo,
                x.Status))
            .ToList();

        return (items, totalCount);
    }
}
