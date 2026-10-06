using Frms.DataAccess.Persistence.Entities;

namespace Frms.DataAccess.Repositories.Interfaces;

public interface IPolicyRepository {
    Task<(IReadOnlyList<Policy> Items, int TotalItems)> GetPagedAsync(
        int page,
        int pageSize,
        CancellationToken cancellationToken = default);

    Task<Policy> CreateVersionAsync(
        int depositTimeoutHours,
        int reservationVisitStartDay,
        int reservationVisitEndDay,
        int monthlyPaymentDueDay,
        int overdueStartDay,
        int lateFeeDivisorDays,
        int earlyReturnWaiveFeeUntilDay,
        CancellationToken cancellationToken = default);
}
