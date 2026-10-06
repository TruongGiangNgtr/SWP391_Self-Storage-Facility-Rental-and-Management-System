using Frms.DataAccess.Persistence;
using Frms.DataAccess.Persistence.Entities;
using Frms.DataAccess.Repositories.Interfaces;
using Microsoft.EntityFrameworkCore;

namespace Frms.DataAccess.Repositories.Implementations;

internal sealed class FacilityRepository(
    FrmsDbContext dbContext) : IFacilityRepository {
    public async Task<(IReadOnlyList<Facility> Items, int TotalItems)> GetPagedAsync(
        int page,
        int pageSize,
        CancellationToken cancellationToken = default) {
        var query = dbContext.Facilities.AsNoTracking();

        var totalItems = await query.CountAsync(cancellationToken);

        var items = await query
            .OrderBy(x => x.Name)
            .ThenBy(x => x.FacilityId)
            .Skip((page - 1) * pageSize)
            .Take(pageSize)
            .ToListAsync(cancellationToken);

        return (items, totalItems);
    }

    public Task<Facility?> GetByIdAsync(
        Guid facilityId,
        CancellationToken cancellationToken = default)
        => dbContext.Facilities.FirstOrDefaultAsync(
            x => x.FacilityId == facilityId,
            cancellationToken);

    public async Task AddAsync(
        Facility facility,
        CancellationToken cancellationToken = default) {
        await dbContext.Facilities.AddAsync(facility, cancellationToken);
        await dbContext.SaveChangesAsync(cancellationToken);
    }

    public async Task SaveChangesAsync(
        CancellationToken cancellationToken = default) {
        await dbContext.SaveChangesAsync(cancellationToken);
    }
}
