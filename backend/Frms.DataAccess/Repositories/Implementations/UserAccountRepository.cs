using Frms.DataAccess.Persistence;
using Frms.DataAccess.Persistence.Entities;
using Frms.DataAccess.Repositories.Interfaces;
using Frms.DataAccess.Repositories.Models;
using Microsoft.EntityFrameworkCore;

namespace Frms.DataAccess.Repositories.Implementations;

internal sealed class UserAccountRepository(FrmsDbContext dbContext) : IUserAccountRepository
{
    public Task<AuthenticationAccount?> FindByPhoneNumberAsync(string normalizedPhoneNumber, CancellationToken cancellationToken) =>
        Query(dbContext.UserAccounts.Where(x => x.PhoneNumber == normalizedPhoneNumber)).SingleOrDefaultAsync(cancellationToken);

    public Task<AuthenticationAccount?> FindByEmailAsync(string normalizedEmail, CancellationToken cancellationToken) =>
        Query(dbContext.UserAccounts.Where(x => x.Email == normalizedEmail)).SingleOrDefaultAsync(cancellationToken);

    public Task<AuthenticationAccount?> FindByIdAsync(Guid userAccountId, CancellationToken cancellationToken) =>
        Query(dbContext.UserAccounts.Where(x => x.UserAccountId == userAccountId)).SingleOrDefaultAsync(cancellationToken);

    public async Task AppendLoginHistoryAsync(Guid userAccountId, bool succeeded, string? ipAddress, string? deviceInfo, DateTime loginAtUtc, CancellationToken cancellationToken)
    {
        dbContext.LoginHistory.Add(new LoginHistory
        {
            LoginHistoryId = Guid.NewGuid(),
            UserAccountId = userAccountId,
            LoginAt = DateTime.SpecifyKind(loginAtUtc, DateTimeKind.Utc),
            IpAddress = Truncate(ipAddress, 45),
            DeviceInfo = Truncate(deviceInfo, 1000),
            Status = succeeded ? "SUCCESS" : "FAILED"
        });
        await dbContext.SaveChangesAsync(cancellationToken);
    }

    private IQueryable<AuthenticationAccount> Query(IQueryable<UserAccount> accounts) =>
        from account in accounts.AsNoTracking()
        join role in dbContext.UserRoles.AsNoTracking() on account.RoleId equals role.RoleId
        join customer in dbContext.Customers.AsNoTracking() on account.UserAccountId equals customer.UserAccountId into customers
        from customer in customers.DefaultIfEmpty()
        join employee in dbContext.Employees.AsNoTracking() on account.UserAccountId equals employee.UserAccountId into employees
        from employee in employees.DefaultIfEmpty()
        select new AuthenticationAccount(
            account.UserAccountId,
            role.RoleName,
            account.Status,
            account.Email,
            account.PhoneNumber,
            account.PasswordHash,
            customer == null ? null : customer.CustomerId,
            employee == null ? null : employee.EmployeeId,
            employee == null ? null : employee.FacilityId,
            customer != null ? customer.FullName : employee != null ? employee.FullName : null);

    private static string? Truncate(string? value, int maxLength) => string.IsNullOrWhiteSpace(value) ? null : value[..Math.Min(value.Length, maxLength)];
}
