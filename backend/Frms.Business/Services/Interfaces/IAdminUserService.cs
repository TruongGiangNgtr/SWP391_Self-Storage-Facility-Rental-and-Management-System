using Frms.Business.Models.Results;

namespace Frms.Business.Services.Interfaces;

public interface IAdminUserService {
    Task<(
        IReadOnlyList<(
            AccountProfileResult Account,
            string RoleName,
            CustomerProfileResult? Customer,
            EmployeeProfileResult? Employee)> Items,
        int TotalItems)> GetPagedAsync(
            int page,
            int pageSize,
            CancellationToken cancellationToken = default);

    Task<(
        AccountProfileResult Account,
        string RoleName,
        CustomerProfileResult? Customer,
        EmployeeProfileResult? Employee)?> GetByIdAsync(
            Guid userAccountId,
            CancellationToken cancellationToken = default);
}
