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
        var allUnits = await dbContext.StorageUnits.AsNoTracking().CountAsync(cancellationToken);
        var inUse = await dbContext.StorageUnits.AsNoTracking()
            .CountAsync(x => x.Status == "IN_USE", cancellationToken);
        return new BusinessPortfolioRecord(facilities, active, inUse, allUnits);
    }
}
