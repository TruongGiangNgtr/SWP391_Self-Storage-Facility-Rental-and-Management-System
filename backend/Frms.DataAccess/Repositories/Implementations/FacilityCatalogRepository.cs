using Frms.DataAccess.Persistence;
using Frms.DataAccess.Repositories.Interfaces;
using Frms.DataAccess.Repositories.Models;
using Microsoft.EntityFrameworkCore;

namespace Frms.DataAccess.Repositories.Implementations;

internal sealed class FacilityCatalogRepository(
    FrmsDbContext dbContext)
    : IFacilityCatalogRepository {
    public async Task<(
        IReadOnlyList<FacilityCatalogRecord> Items,
        int TotalItems)> GetActivePagedAsync(
        int skip,
        int take,
        CancellationToken cancellationToken = default) {
        var query = dbContext.Facilities
            .AsNoTracking()
            .Where(x => x.Status == "ACTIVE");

        var totalItems =
            await query.CountAsync(cancellationToken);

        var items = await query
            .OrderBy(x => x.Name)
            .ThenBy(x => x.FacilityId)
            .Skip(skip)
            .Take(take)
            .Select(x => new FacilityCatalogRecord(
                x.FacilityId,
                x.Name,
                x.Address,
                x.ContactInfo,
                x.Description,
                x.Status))
            .ToListAsync(cancellationToken);

        return (items, totalItems);
    }

    public Task<FacilityCatalogRecord?> GetActiveByIdAsync(
        Guid facilityId,
        CancellationToken cancellationToken = default) {
        return dbContext.Facilities
            .AsNoTracking()
            .Where(x =>
                x.FacilityId == facilityId &&
                x.Status == "ACTIVE")
            .Select(x => new FacilityCatalogRecord(
                x.FacilityId,
                x.Name,
                x.Address,
                x.ContactInfo,
                x.Description,
                x.Status))
            .SingleOrDefaultAsync(cancellationToken);
    }
}
