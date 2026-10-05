using Frms.Business.Models.Commands;

namespace Frms.Business.Services.Interfaces;

public interface IStorageUnitService {
    Task<Guid> CreateAsync(
        Guid facilityId,
        CreateStorageUnitCommand command,
        CancellationToken cancellationToken = default);
}
