using Frms.DataAccess.Persistence;
using Frms.DataAccess.Repositories.Interfaces;
using Frms.DataAccess.Repositories.Models;
using Microsoft.EntityFrameworkCore;

namespace Frms.DataAccess.Repositories.Implementations;

internal sealed class BusinessPortfolioRepository(FrmsDbContext dbContext)
    : IBusinessPortfolioRepository
{
    public async Task<BusinessPortfolioRecord> GetSnapshotAsync(CancellationToken cancellationToken)
    {
        // System-wide physical usage, including INSPECTION and MAINTENANCE
        // in denominator, independent of Facility ACTIVE status.
        var facilities = await dbContext.Facilities.AsNoTracking().CountAsync(cancellationToken);
        var active = await dbContext.Contracts.AsNoTracking()
            .CountAsync(x => x.Status == "ACTIVE", cancellationToken);

        // Calculate both unit metrics in one SQL aggregation rather than
        // scanning StorageUnit with two independent COUNT queries.
        var unitCounts = await dbContext.StorageUnits.AsNoTracking()
            .GroupBy(_ => 1)
            .Select(g => new {
                Total = g.Count(),
                InUse = g.Count(x => x.Status == "IN_USE")
            })
            .SingleOrDefaultAsync(cancellationToken);
        return new BusinessPortfolioRecord(
            facilities, active, unitCounts?.InUse ?? 0, unitCounts?.Total ?? 0);
    }
}
