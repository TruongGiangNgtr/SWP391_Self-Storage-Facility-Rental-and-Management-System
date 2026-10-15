using Frms.DataAccess.Persistence;
using Frms.DataAccess.Persistence.Entities;
using Frms.DataAccess.Repositories.Interfaces;
using Frms.DataAccess.Repositories.Models;
using Npgsql;
using Microsoft.EntityFrameworkCore;

namespace Frms.DataAccess.Repositories.Implementations;

internal sealed class CustomerRegistrationRepository(FrmsDbContext dbContext)
    : ICustomerRegistrationRepository
{
    public async Task CreateAsync(NewCustomerRegistration registration, CancellationToken cancellationToken)
    {
        var existing = await FindConflictAsync(registration, cancellationToken);
        if (existing is not null) throw new CustomerRegistrationConflictException(existing.Value);

        var roleId = await dbContext.UserRoles.AsNoTracking()
            .Where(x => x.RoleName == "CUSTOMER")
            .Select(x => x.RoleId)
            .SingleOrDefaultAsync(cancellationToken);
        if (roleId == Guid.Empty)
            throw new InvalidOperationException("The required CUSTOMER role seed is missing.");

        dbContext.UserAccounts.Add(new UserAccount
        {
            UserAccountId = registration.UserAccountId,
            RoleId = roleId,
            Email = registration.Email,
            PhoneNumber = registration.PhoneNumber,
            PasswordHash = registration.PasswordHash,
            Status = "ACTIVE",
            EmailVerifiedAt = null,
            CreatedAt = DateTime.SpecifyKind(registration.CreatedAtUtc, DateTimeKind.Utc)
        });
        dbContext.Customers.Add(new Customer
        {
            CustomerId = registration.CustomerId,
            UserAccountId = registration.UserAccountId,
            FullName = registration.FullName,
            Address = registration.Address,
            Cccd = registration.Cccd
        });

        // EF Core SaveChanges uses a SQL transaction for this two-row insert.
        // A concurrent registration may win after the initial existence check.
        try
        {
            await dbContext.SaveChangesAsync(cancellationToken);
        }
        catch (DbUpdateException exception) when (exception.InnerException is PostgresException postgres && postgres.SqlState == PostgresErrorCodes.UniqueViolation)
        {
            var collision = await FindConflictAsync(registration, cancellationToken);
            if (collision is not null) throw new CustomerRegistrationConflictException(collision.Value);
            throw;
        }
    }

    private async Task<RegistrationConflictIdentifier?> FindConflictAsync(
        NewCustomerRegistration registration, CancellationToken cancellationToken)
    {
        if (await dbContext.UserAccounts.AsNoTracking()
                .AnyAsync(x => x.Email == registration.Email, cancellationToken))
            return RegistrationConflictIdentifier.Email;
        if (await dbContext.UserAccounts.AsNoTracking()
                .AnyAsync(x => x.PhoneNumber == registration.PhoneNumber, cancellationToken))
            return RegistrationConflictIdentifier.PhoneNumber;
        if (registration.Cccd is not null && await dbContext.Customers.AsNoTracking()
                .AnyAsync(x => x.Cccd == registration.Cccd, cancellationToken))
            return RegistrationConflictIdentifier.Cccd;
        return null;
    }
}
