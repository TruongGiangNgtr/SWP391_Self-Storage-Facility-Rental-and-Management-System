using Frms.Business.Exceptions;
using Frms.Business.Models.Commands;
using Frms.Business.Models.Results;
using Frms.Business.Services.Interfaces;
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

    public async Task<(IReadOnlyList<StorageUnitResult> Items, int TotalCount)> ListByFacilityAsync(
        Guid facilityId,
        int page,
        int pageSize,
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

        var (items, totalCount) = await repository.ListByFacilityAsync(
            facilityId,
            page,
            pageSize,
            cancellationToken);

        var results = items
            .Select(x => new StorageUnitResult(
                x.StorageUnitId,
                x.FacilityId,
                x.UnitTypeId,
                x.UnitCode,
                x.LocationInfo,
                x.Status))
            .ToList();

        return (results, totalCount);
    }
}
