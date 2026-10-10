using Frms.Business.Models.Results;

namespace Frms.Business.Services.Interfaces;

public interface IStaffWorkItemService {
    Task<StaffWorkItemPageResult> ListAsync(
        DateOnly? date,
        int page,
        int pageSize,
        CancellationToken cancellationToken = default);
}
