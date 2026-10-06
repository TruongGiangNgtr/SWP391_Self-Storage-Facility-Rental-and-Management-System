using Frms.DataAccess.Persistence.Entities;

namespace Frms.DataAccess.Repositories.Interfaces;

public interface IAdminUserRepository {
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
