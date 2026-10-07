using Frms.DataAccess.Persistence;
using Frms.DataAccess.Persistence.Entities;
using Frms.DataAccess.Repositories.Interfaces;
using Microsoft.EntityFrameworkCore;

namespace Frms.DataAccess.Repositories.Implementations;

internal sealed class DiscountRepository(
    FrmsDbContext dbContext)
    : IDiscountRepository {
    public Task<Guid?> GetCustomerIdByUserAccountIdAsync(
        Guid userAccountId,
        CancellationToken cancellationToken) {
        return dbContext.Customers
            .AsNoTracking()
            .Where(x => x.UserAccountId == userAccountId)
            .Select(x => (Guid?)x.CustomerId)
            .SingleOrDefaultAsync(cancellationToken);
    }

    public Task<Employee?> GetEmployeeByUserAccountIdAsync(
        Guid userAccountId,
        CancellationToken cancellationToken) {
        return dbContext.Employees
            .AsNoTracking()
            .SingleOrDefaultAsync(
                x => x.UserAccountId == userAccountId,
                cancellationToken);
    }

    public Task<bool> CustomerExistsAsync(
        Guid customerId,
        CancellationToken cancellationToken) {
        return dbContext.Customers
            .AsNoTracking()
            .AnyAsync(
                x => x.CustomerId == customerId,
                cancellationToken);
    }

    public async Task<bool> HasFacilityRelationshipAsync(
        Guid customerId,
        Guid facilityId,
        CancellationToken cancellationToken) {
        var hasReservation =
            await dbContext.Reservations
                .AsNoTracking()
                .AnyAsync(
                    x => x.CustomerId == customerId &&
                         x.FacilityId == facilityId,
                    cancellationToken);

        if (hasReservation) {
            return true;
        }

        return await dbContext.Contracts
            .AsNoTracking()
            .AnyAsync(
                x => x.CustomerId == customerId &&
                     x.FacilityId == facilityId,
                cancellationToken);
    }

    public Task<int> CountByCustomerAsync(
        Guid customerId,
        CancellationToken cancellationToken) {
        return dbContext.Discounts
            .AsNoTracking()
            .Where(x => x.CustomerId == customerId)
            .CountAsync(cancellationToken);
    }

    public async Task<IReadOnlyList<Discount>> ListByCustomerAsync(
        Guid customerId,
        int skip,
        int take,
        CancellationToken cancellationToken) {
        return await dbContext.Discounts
            .AsNoTracking()
            .Where(x => x.CustomerId == customerId)
            .OrderByDescending(x => x.EffectiveFrom)
            .ThenBy(x => x.DiscountId)
            .Skip(skip)
            .Take(take)
            .ToListAsync(cancellationToken);
    }
}
