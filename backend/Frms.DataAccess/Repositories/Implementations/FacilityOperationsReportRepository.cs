using Frms.DataAccess.Persistence;
using Frms.DataAccess.Repositories.Interfaces;
using Frms.DataAccess.Repositories.Models;
using Microsoft.EntityFrameworkCore;

namespace Frms.DataAccess.Repositories.Implementations;

internal sealed class FacilityOperationsReportRepository(FrmsDbContext dbContext)
    : IFacilityOperationsReportRepository
{
    public Task<bool> FacilityExistsAsync(Guid facilityId, CancellationToken cancellationToken) =>
        dbContext.Facilities.AsNoTracking().AnyAsync(
            x => x.FacilityId == facilityId, cancellationToken);

    public async Task<FacilityOperationsRecord> GetOperationsAsync(
        Guid facilityId, CancellationToken cancellationToken)
    {
        var counts = await dbContext.StorageUnits.AsNoTracking()
            .Where(x => x.FacilityId == facilityId)
            .GroupBy(x => x.FacilityId)
            .Select(g => new
            {
                Total = g.Count(),
                Available = g.Count(x => x.Status == "AVAILABLE"),
                InUse = g.Count(x => x.Status == "IN_USE"),
                Inspection = g.Count(x => x.Status == "INSPECTION"),
                Maintenance = g.Count(x => x.Status == "MAINTENANCE")
            })
            .SingleOrDefaultAsync(cancellationToken);

        // Count distinct active Contracts with at least one persisted OVERDUE
        // RENTAL_FEE Invoice. First-month offline rent has no Invoice.
        var overdueContracts = await dbContext.Contracts.AsNoTracking()
            .Where(c => c.FacilityId == facilityId && c.Status == "ACTIVE")
            .CountAsync(c => dbContext.Invoices.AsNoTracking().Any(i =>
                i.EntityId == c.ContractId &&
                i.InvoiceType == "RENTAL_FEE" &&
                i.Status == "OVERDUE" &&
                i.BillingMonth.HasValue &&
                i.BillingMonth.Value > c.StartMonth), cancellationToken);

        return new FacilityOperationsRecord(
            counts?.Available ?? 0,
            counts?.InUse ?? 0,
            counts?.Inspection ?? 0,
            counts?.Maintenance ?? 0,
            counts?.Total ?? 0,
            overdueContracts);
    }
}
