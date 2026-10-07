using Frms.DataAccess.Persistence;
using Frms.DataAccess.Persistence.Entities;
using Frms.DataAccess.Repositories.Interfaces;
using Microsoft.EntityFrameworkCore;

namespace Frms.DataAccess.Repositories.Implementations;

internal sealed class AdminEmployeeRepository(
    FrmsDbContext dbContext) : IAdminEmployeeRepository {
    public Task<bool> EmailExistsAsync(
        string email,
        CancellationToken cancellationToken = default)
        => dbContext.UserAccounts
            .AsNoTracking()
            .AnyAsync(
                x => x.Email == email,
                cancellationToken);

    public Task<bool> PhoneNumberExistsAsync(
        string phoneNumber,
        CancellationToken cancellationToken = default)
        => dbContext.UserAccounts
            .AsNoTracking()
            .AnyAsync(
                x => x.PhoneNumber == phoneNumber,
                cancellationToken);

    public async Task<Guid?> GetRoleIdAsync(
        string roleName,
        CancellationToken cancellationToken = default) {
        return await dbContext.UserRoles
            .AsNoTracking()
            .Where(x => x.RoleName == roleName)
            .Select(x => (Guid?)x.RoleId)
            .SingleOrDefaultAsync(cancellationToken);
    }

    public Task<bool> FacilityExistsAsync(
        Guid facilityId,
        CancellationToken cancellationToken = default)
        => dbContext.Facilities
            .AsNoTracking()
            .AnyAsync(
                x => x.FacilityId == facilityId,
                cancellationToken);

    public async Task<(
        UserAccount Account,
        Employee Employee,
        string RoleName)?> GetByEmployeeIdAsync(
            Guid employeeId,
            CancellationToken cancellationToken = default) {
        var row = await (
            from employee in dbContext.Employees
            join account in dbContext.UserAccounts
                on employee.UserAccountId equals account.UserAccountId
            join role in dbContext.UserRoles
                on account.RoleId equals role.RoleId
            where employee.EmployeeId == employeeId
            select new EmployeeRow(
                account,
                employee,
                role.RoleName))
            .FirstOrDefaultAsync(cancellationToken);

        if (row is null)
            return null;

        return (
            row.Account,
            row.Employee,
            row.RoleName);
    }

    public async Task<(UserAccount Account, Employee Employee)> CreateAsync(
        Guid roleId,
        string fullName,
        string email,
        string phoneNumber,
        string passwordHash,
        Guid? facilityId,
        CancellationToken cancellationToken = default) {
        var account = new UserAccount {
            UserAccountId = Guid.NewGuid(),
            RoleId = roleId,
            Email = email,
            PhoneNumber = phoneNumber,
            PasswordHash = passwordHash,
            Status = "INACTIVE",
            EmailVerifiedAt = null,
            CreatedAt = DateTime.UtcNow
        };

        var employee = new Employee {
            EmployeeId = Guid.NewGuid(),
            UserAccountId = account.UserAccountId,
            FacilityId = facilityId,
            FullName = fullName
        };

        dbContext.UserAccounts.Add(account);
        dbContext.Employees.Add(employee);

        await dbContext.SaveChangesAsync(cancellationToken);

        return (account, employee);
    }

    public async Task SaveChangesAsync(
        CancellationToken cancellationToken = default) {
        await dbContext.SaveChangesAsync(cancellationToken);
    }

    public async Task SetAccountStatusAsync(
    Guid userAccountId,
    string status,
    CancellationToken cancellationToken) {
        await dbContext.Database.ExecuteSqlInterpolatedAsync(
            $"""
        EXEC dbo.usp_SetUserAccountStatus
            @UserAccountId = {userAccountId},
            @NewStatus = {status}
        """,
            cancellationToken);

        dbContext.ChangeTracker.Clear();
    }

    private sealed record EmployeeRow(
        UserAccount Account,
        Employee Employee,
        string RoleName);

    public async Task UpdateRoleFacilityAsync(
        Guid employeeId,
        string roleName,
        Guid? facilityId,
        CancellationToken cancellationToken = default) {
            await dbContext.Database.ExecuteSqlInterpolatedAsync($"""
            EXEC dbo.usp_UpdateEmployeeRoleFacility
                @EmployeeId = {employeeId},
                @RoleName = {roleName},
                @FacilityId = {facilityId}
            """, cancellationToken);

            dbContext.ChangeTracker.Clear();
    }
}
