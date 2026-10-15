using Frms.DataAccess.Persistence;
using Frms.DataAccess.Persistence.Entities;
using Frms.DataAccess.Repositories.Interfaces;
using Frms.DataAccess.Repositories.Models;
using Npgsql;
using Microsoft.EntityFrameworkCore;

namespace Frms.DataAccess.Repositories.Implementations;

internal sealed class CustomerRepository(FrmsDbContext db) : ICustomerRepository
{
    public async Task<CustomerProfileRecord?> UpdateOwnProfileAsync(
        Guid userAccountId,
        bool updateFullName, string? fullName,
        bool updateAddress, string? address,
        bool updateCccd, string? cccd,
        CancellationToken ct)
    {
        // Ownership is derived from JWT subject; no CustomerId accepted from the client.
        var customer = await db.Customers.SingleOrDefaultAsync(
            x => x.UserAccountId == userAccountId, ct);
        if (customer is null) return null;

        if (updateCccd && cccd is not null && await HasCccdConflictAsync(customer.CustomerId, cccd, ct))
            throw new CustomerCccdConflictException();
        if (updateFullName) customer.FullName = fullName!;
        if (updateAddress) customer.Address = address;
        if (updateCccd) customer.Cccd = cccd;

        try
        {
            await db.SaveChangesAsync(ct);
        }
        catch (DbUpdateException ex) when (ex.InnerException is PostgresException postgres && postgres.SqlState == PostgresErrorCodes.UniqueViolation)
        {
            // Keep the existing SQL uniqueness constraint authoritative under concurrency.
            if (updateCccd && cccd is not null && await HasCccdConflictAsync(customer.CustomerId, cccd, ct))
                throw new CustomerCccdConflictException();
            throw;
        }
        return new CustomerProfileRecord(customer.CustomerId, customer.UserAccountId, customer.FullName);
    }

    private Task<bool> HasCccdConflictAsync(Guid ownCustomerId, string cccd, CancellationToken ct) =>
        db.Customers.AsNoTracking().AnyAsync(x => x.Cccd == cccd && x.CustomerId != ownCustomerId, ct);

    // Both Reservation and Contract relationships count. All filters are applied in SQL
    // before COUNT, ORDER BY and paging; never page an unscoped Customer set.
    private IQueryable<Customer> FacilityCustomers(Guid facilityId) =>
        db.Customers.AsNoTracking().Where(customer =>
            db.Reservations.Any(r => r.CustomerId == customer.CustomerId && r.FacilityId == facilityId) ||
            db.Contracts.Any(c => c.CustomerId == customer.CustomerId && c.FacilityId == facilityId));

    public Task<int> CountByFacilityAsync(Guid facilityId, CancellationToken ct) =>
        FacilityCustomers(facilityId).CountAsync(ct);

    public async Task<IReadOnlyList<CustomerDirectoryRecord>> ListByFacilityAsync(
        Guid facilityId, int skip, int take, CancellationToken ct) =>
        await FacilityCustomers(facilityId)
            .OrderBy(x => x.FullName).ThenBy(x => x.CustomerId)
            .Skip(skip).Take(take)
            .Select(x => new CustomerDirectoryRecord(x.CustomerId, x.FullName))
            .ToArrayAsync(ct);

    public Task<CustomerDirectoryRecord?> GetByFacilityAsync(
        Guid facilityId, Guid customerId, CancellationToken ct) =>
        FacilityCustomers(facilityId).Where(x => x.CustomerId == customerId)
            .Select(x => new CustomerDirectoryRecord(x.CustomerId, x.FullName))
            .SingleOrDefaultAsync(ct);

    public Task<int> CountAllAsync(CancellationToken ct) =>
        db.Customers.AsNoTracking().CountAsync(ct);

    public async Task<IReadOnlyList<CustomerDirectoryRecord>> ListAllAsync(
        int skip, int take, CancellationToken ct) =>
        await db.Customers.AsNoTracking()
            .OrderBy(x => x.FullName).ThenBy(x => x.CustomerId)
            .Skip(skip).Take(take)
            .Select(x => new CustomerDirectoryRecord(x.CustomerId, x.FullName))
            .ToArrayAsync(ct);

    public Task<CustomerDirectoryRecord?> GetByIdAsync(Guid customerId, CancellationToken ct) =>
        db.Customers.AsNoTracking().Where(x => x.CustomerId == customerId)
            .Select(x => new CustomerDirectoryRecord(x.CustomerId, x.FullName))
            .SingleOrDefaultAsync(ct);
}
