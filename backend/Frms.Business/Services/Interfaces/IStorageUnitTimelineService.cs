using Frms.Business.Models.Results;

namespace Frms.Business.Services.Interfaces;

public interface IStorageUnitTimelineService
{
    Task<(IReadOnlyList<StorageUnitTimelineResult> Items, int TotalCount)> ListAsync(
        Guid facilityId, Guid? unitTypeId, string? month, int page, int pageSize,
        CancellationToken cancellationToken = default);

    Task<StorageUnitTimelineResult> GetAsync(
        Guid facilityId, Guid storageUnitId, string? month,
        CancellationToken cancellationToken = default);
}
