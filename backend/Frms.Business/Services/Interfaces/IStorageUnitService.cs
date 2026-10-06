using Frms.Business.Models.Commands;
using Frms.Business.Models.Results;

namespace Frms.Business.Services.Interfaces;

public interface IStorageUnitService {
    Task<Guid> CreateAsync(
        Guid facilityId,
        CreateStorageUnitCommand command,
        CancellationToken cancellationToken = default);

    Task<(IReadOnlyList<StorageUnitResult> Items, int TotalCount)>
        ListByFacilityAsync(
            Guid facilityId,
            int page,
            int pageSize,
            CancellationToken cancellationToken = default);

    Task<StorageUnitResult> GetByIdAsync(
        Guid storageUnitId,
        CancellationToken cancellationToken = default);

    Task<StorageUnitResult> UpdateAsync(
        Guid storageUnitId,
        UpdateStorageUnitCommand command,
        CancellationToken cancellationToken = default);

    Task<StorageUnitResult> ChangeStatusAsync(
        Guid storageUnitId,
        ChangeStorageUnitStatusCommand command,
        CancellationToken cancellationToken = default);
}
