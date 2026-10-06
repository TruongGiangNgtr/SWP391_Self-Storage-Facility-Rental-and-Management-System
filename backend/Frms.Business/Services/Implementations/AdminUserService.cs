using Frms.Business.Services.Interfaces;
using Frms.DataAccess.Persistence.Entities;
using Frms.DataAccess.Repositories.Interfaces;

namespace Frms.Business.Services.Implementations;

internal sealed class AdminUserService(
    IAdminUserRepository adminUserRepository) : IAdminUserService {
    public Task<(
        IReadOnlyList<(
            UserAccount Account,
            string RoleName,
            Customer? Customer,
            Employee? Employee)> Items,
        int TotalItems)> GetPagedAsync(
            int page,
            int pageSize,
            CancellationToken cancellationToken = default)
        => adminUserRepository.GetPagedAsync(
            page,
            pageSize,
            cancellationToken);

    public Task<(
        UserAccount Account,
        string RoleName,
        Customer? Customer,
        Employee? Employee)?> GetByIdAsync(
            Guid userAccountId,
            CancellationToken cancellationToken = default)
        => adminUserRepository.GetByIdAsync(
            userAccountId,
            cancellationToken);
}
