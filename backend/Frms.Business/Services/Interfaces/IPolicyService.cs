using Frms.Business.Models.Results;

namespace Frms.Business.Services.Interfaces;

public interface IPolicyService {
    Task<(IReadOnlyList<PolicyResult> Items, int TotalItems)> GetPagedAsync(
        int page,
        int pageSize,
        CancellationToken cancellationToken = default);

    Task<PolicyResult> CreateVersionAsync(
        int depositTimeoutHours,
        int reservationVisitStartDay,
        int reservationVisitEndDay,
        int monthlyPaymentDueDay,
        int overdueStartDay,
        int lateFeeDivisorDays,
        int earlyReturnWaiveFeeUntilDay,
        CancellationToken cancellationToken = default);
}
