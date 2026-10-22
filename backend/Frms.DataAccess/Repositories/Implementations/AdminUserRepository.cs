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

        // Page the root entity BEFORE joining optional profile tables.
        // Customer.UserAccountId and Employee.UserAccountId are unique.
        var accounts = dbContext.UserAccounts.AsNoTracking()
            .OrderByDescending(x => x.CreatedAt)
            .ThenBy(x => x.UserAccountId)
            .Skip((page - 1) * pageSize)
            .Take(pageSize);

        var query =
            from account in accounts

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

            select new {
                Account = account,
                RoleName = role.RoleName,
                Customer = customer,
                Employee = employee
            };

        // Re-apply the public ordering after the joins; pagination is already
        // completed in the account subquery above.
        var rows = await query
            .OrderByDescending(x => x.Account.CreatedAt)
            .ThenBy(x => x.Account.UserAccountId)
            .ToListAsync(cancellationToken);

        var items = rows
            .Select(x => (
                Account: x.Account,
                RoleName: x.RoleName,
                Customer: (Customer?)x.Customer,
                Employee: (Employee?)x.Employee))
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
        var row = await (
            from account in dbContext.UserAccounts.AsNoTracking()

            where account.UserAccountId == userAccountId

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

            select new {
                Account = account,
                RoleName = role.RoleName,
                Customer = customer,
                Employee = employee
            })
            .FirstOrDefaultAsync(cancellationToken);

        if (row is null)
            return null;

        return (
            Account: row.Account,
            RoleName: row.RoleName,
            Customer: (Customer?)row.Customer,
            Employee: (Employee?)row.Employee);
    }

    private sealed record UserAccountRow(
        UserAccount Account,
        string RoleName,
        Customer? Customer,
        Employee? Employee);
}
