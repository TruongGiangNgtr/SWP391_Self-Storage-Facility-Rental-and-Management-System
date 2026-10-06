using Frms.Business.Models.Commands;
using Frms.Business.Models.Results;

namespace Frms.Business.Services.Interfaces;

public interface IStorageUnitService {
    Task<Guid> CreateAsync(
        Guid facilityId,
        CreateStorageUnitCommand command,
        CancellationToken cancellationToken = default);

    Task<(IReadOnlyList<StorageUnitResult> Items, int TotalCount)> ListByFacilityAsync(
        Guid facilityId,
        int page,
        int pageSize,
        CancellationToken cancellationToken = default);
}
