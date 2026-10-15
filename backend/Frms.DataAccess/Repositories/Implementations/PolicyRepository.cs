using Frms.DataAccess.Persistence;
using Frms.DataAccess.Persistence.Entities;
using Frms.DataAccess.Repositories.Interfaces;
using Microsoft.EntityFrameworkCore;

namespace Frms.DataAccess.Repositories.Implementations;

internal sealed class PolicyRepository(
    FrmsDbContext dbContext) : IPolicyRepository {
    public async Task<(IReadOnlyList<Policy> Items, int TotalItems)> GetPagedAsync(
        int page,
        int pageSize,
        CancellationToken cancellationToken = default) {
        var query = dbContext.Policies.AsNoTracking();

        var totalItems = await query.CountAsync(cancellationToken);

        var items = await query
            .OrderByDescending(x => x.Version)
            .Skip((page - 1) * pageSize)
            .Take(pageSize)
            .ToListAsync(cancellationToken);

        return (items, totalItems);
    }

    public async Task<Policy> CreateVersionAsync(
        int depositTimeoutHours,
        int reservationVisitStartDay,
        int reservationVisitEndDay,
        int monthlyPaymentDueDay,
        int overdueStartDay,
        int lateFeeDivisorDays,
        int earlyReturnWaiveFeeUntilDay,
        CancellationToken cancellationToken = default) {
        var createdId = await dbContext.Database.SqlQuery<Guid>($"""
            SELECT "PolicyId" AS "Value" FROM public."usp_CreatePolicyVersion"({depositTimeoutHours}, {reservationVisitStartDay}, {reservationVisitEndDay}, {monthlyPaymentDueDay}, {overdueStartDay}, {lateFeeDivisorDays}, {earlyReturnWaiveFeeUntilDay})
            """).SingleAsync(cancellationToken);

        var policy = await dbContext.Policies
            .AsNoTracking()
            .SingleAsync(x => x.PolicyId == createdId, cancellationToken);

        return policy;
    }
}
