using Frms.Business.Models.Results;
using Frms.Business.Services.Interfaces;
using Frms.DataAccess.Persistence.Entities;
using Frms.DataAccess.Repositories.Interfaces;

namespace Frms.Business.Services.Implementations;

internal sealed class AdminUserService(
    IAdminUserRepository adminUserRepository) : IAdminUserService {
    private Task<(
        IReadOnlyList<(
            UserAccount Account,
            string RoleName,
            Customer? Customer,
            Employee? Employee)> Items,
        int TotalItems)> GetPagedCoreAsync(
            int page,
            int pageSize,
            CancellationToken cancellationToken = default)
        => adminUserRepository.GetPagedAsync(
            page,
            pageSize,
            cancellationToken);

    private Task<(
        UserAccount Account,
        string RoleName,
        Customer? Customer,
        Employee? Employee)?> GetByIdCoreAsync(
            Guid userAccountId,
            CancellationToken cancellationToken = default)
        => adminUserRepository.GetByIdAsync(
            userAccountId,
            cancellationToken);

    public async Task<(IReadOnlyList<(AccountProfileResult Account, string RoleName, CustomerProfileResult? Customer, EmployeeProfileResult? Employee)> Items, int TotalItems)> GetPagedAsync(int page, int pageSize, CancellationToken cancellationToken = default)
    {
        var row = await GetPagedCoreAsync(page, pageSize, cancellationToken);
        return (row.Items.Select(ServiceResultProjection.Map).ToArray(), row.TotalItems);
    }

    public async Task<(AccountProfileResult Account, string RoleName, CustomerProfileResult? Customer, EmployeeProfileResult? Employee)?> GetByIdAsync(Guid userAccountId, CancellationToken cancellationToken = default)
    {
        return ServiceResultProjection.Map(await GetByIdCoreAsync(userAccountId, cancellationToken));
    }
}
