using Frms.Business.Models.Commands;
using Frms.Business.Models;

namespace Frms.Business.Services.Interfaces;

public interface IStorageUnitService {
    Task<Guid> CreateAsync(
        Guid facilityId,
        CreateStorageUnitCommand command,
        CancellationToken cancellationToken = default);

    Task<IReadOnlyList<StorageUnitListItem>> ListByFacilityAsync(
        Guid facilityId,
        CancellationToken cancellationToken = default);
}
