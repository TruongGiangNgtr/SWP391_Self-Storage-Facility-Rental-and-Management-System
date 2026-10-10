using Frms.DataAccess.Repositories.Models;

namespace Frms.DataAccess.Repositories.Interfaces;

public interface IStaffWorkItemRepository {
    Task<Guid?> GetEmployeeIdAsync(
        Guid userAccountId,
        Guid facilityId,
        CancellationToken cancellationToken);

    Task<(IReadOnlyList<StaffWorkItemRecord> Items,
        int TotalItems)> GetWorkItemsAsync(
        Guid facilityId,
        Guid employeeId,
        DateOnly date,
        int page,
        int pageSize,
        CancellationToken cancellationToken);
}
