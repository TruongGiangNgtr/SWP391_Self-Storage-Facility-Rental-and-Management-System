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
        await dbContext.Database.ExecuteSqlInterpolatedAsync($"""
            EXEC dbo.usp_CreatePolicyVersion
                @DepositTimeoutHours = {depositTimeoutHours},
                @ReservationVisitStartDay = {reservationVisitStartDay},
                @ReservationVisitEndDay = {reservationVisitEndDay},
                @MonthlyPaymentDueDay = {monthlyPaymentDueDay},
                @OverdueStartDay = {overdueStartDay},
                @LateFeeDivisorDays = {lateFeeDivisorDays},
                @EarlyReturnWaiveFeeUntilDay = {earlyReturnWaiveFeeUntilDay}
            """, cancellationToken);

        var policy = await dbContext.Policies
            .AsNoTracking()
            .Where(x => x.Status == "ACTIVE")
            .OrderByDescending(x => x.Version)
            .FirstAsync(cancellationToken);

        return policy;
    }
}
