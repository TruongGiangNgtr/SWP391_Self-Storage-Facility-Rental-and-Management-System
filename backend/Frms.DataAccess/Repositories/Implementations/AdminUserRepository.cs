using Frms.DataAccess.Persistence;
using Frms.DataAccess.Persistence.Entities;
using Frms.DataAccess.Repositories.Interfaces;
using Microsoft.EntityFrameworkCore;

namespace Frms.DataAccess.Repositories.Implementations;

internal sealed class AdminUserRepository(
    FrmsDbContext dbContext) : IAdminUserRepository {
    public async Task<(
        IReadOnlyList<(
            UserAccount Account,
            string RoleName,
            Customer? Customer,
            Employee? Employee)> Items,
        int TotalItems)> GetPagedAsync(
            int page,
            int pageSize,
            CancellationToken cancellationToken = default) {
        var totalItems = await dbContext.UserAccounts
            .AsNoTracking()
            .CountAsync(cancellationToken);

        var rows = await BuildQuery()
            .OrderByDescending(x => x.Account.CreatedAt)
            .ThenBy(x => x.Account.UserAccountId)
            .Skip((page - 1) * pageSize)
            .Take(pageSize)
            .ToListAsync(cancellationToken);

        var items = rows
            .Select(x => (
                Account: x.Account,
                RoleName: x.RoleName,
                Customer: x.Customer,
                Employee: x.Employee))
            .ToList();

        return (items, totalItems);
    }

    public async Task<(
        UserAccount Account,
        string RoleName,
        Customer? Customer,
        Employee? Employee)?> GetByIdAsync(
            Guid userAccountId,
            CancellationToken cancellationToken = default) {
        var row = await BuildQuery()
            .FirstOrDefaultAsync(
                x => x.Account.UserAccountId == userAccountId,
                cancellationToken);

        if (row is null)
            return null;

        return (
            Account: row.Account,
            RoleName: row.RoleName,
            Customer: row.Customer,
            Employee: row.Employee);
    }

    private IQueryable<UserAccountRow> BuildQuery()
        => from account in dbContext.UserAccounts.AsNoTracking()

           join role in dbContext.UserRoles.AsNoTracking()
               on account.RoleId equals role.RoleId

           join customerItem in dbContext.Customers.AsNoTracking()
               on account.UserAccountId equals customerItem.UserAccountId
               into customerGroup

           from customer in customerGroup.DefaultIfEmpty()

           join employeeItem in dbContext.Employees.AsNoTracking()
               on account.UserAccountId equals employeeItem.UserAccountId
               into employeeGroup

           from employee in employeeGroup.DefaultIfEmpty()

           select new UserAccountRow(
               account,
               role.RoleName,
               customer,
               employee);

    private sealed record UserAccountRow(
        UserAccount Account,
        string RoleName,
        Customer? Customer,
        Employee? Employee);
}
