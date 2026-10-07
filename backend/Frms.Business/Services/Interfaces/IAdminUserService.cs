using Frms.DataAccess.Persistence.Entities;

namespace Frms.Business.Services.Interfaces;

public interface IAdminUserService {
    Task<(
        IReadOnlyList<(
            UserAccount Account,
            string RoleName,
            Customer? Customer,
            Employee? Employee)> Items,
        int TotalItems)> GetPagedAsync(
            int page,
            int pageSize,
            CancellationToken cancellationToken = default);

    Task<(
        UserAccount Account,
        string RoleName,
        Customer? Customer,
        Employee? Employee)?> GetByIdAsync(
            Guid userAccountId,
            CancellationToken cancellationToken = default);
}
