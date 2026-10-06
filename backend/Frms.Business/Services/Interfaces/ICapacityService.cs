using Frms.Business.Models.Results;

namespace Frms.Business.Services.Interfaces;

public interface ICapacityService
{
    Task<CapacityPageResult> ListUnitTypesAsync(
        Guid facilityId,
        string? startMonth,
        string? endMonth,
        int page,
        int pageSize,
        CancellationToken cancellationToken = default);
}