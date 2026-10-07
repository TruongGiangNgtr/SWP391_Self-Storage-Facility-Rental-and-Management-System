using Frms.Business.Models.Results;
using Frms.Business.Services.Interfaces;
using Frms.DataAccess.Persistence.Entities;
using Frms.DataAccess.Repositories.Interfaces;

namespace Frms.Business.Services.Implementations;

internal sealed class PolicyService(
    IPolicyRepository policyRepository) : IPolicyService {
    public async Task<(IReadOnlyList<PolicyResult> Items, int TotalItems)> GetPagedAsync(
        int page,
        int pageSize,
        CancellationToken cancellationToken = default) {
        var (items, totalItems) = await policyRepository.GetPagedAsync(
            page,
            pageSize,
            cancellationToken);

        var results = items
            .Select(Map)
            .ToList();

        return (results, totalItems);
    }

    public async Task<PolicyResult> CreateVersionAsync(
        int depositTimeoutHours,
        int reservationVisitStartDay,
        int reservationVisitEndDay,
        int monthlyPaymentDueDay,
        int overdueStartDay,
        int lateFeeDivisorDays,
        int earlyReturnWaiveFeeUntilDay,
        CancellationToken cancellationToken = default) {
        var policy = await policyRepository.CreateVersionAsync(
            depositTimeoutHours,
            reservationVisitStartDay,
            reservationVisitEndDay,
            monthlyPaymentDueDay,
            overdueStartDay,
            lateFeeDivisorDays,
            earlyReturnWaiveFeeUntilDay,
            cancellationToken);

        return Map(policy);
    }

    private static PolicyResult Map(Policy policy)
        => new(
            policy.PolicyId,
            policy.Version,
            policy.Status,
            policy.EffectiveFrom,
            policy.EffectiveTo,
            policy.DepositTimeoutHours,
            policy.ReservationVisitStartDay,
            policy.ReservationVisitEndDay,
            policy.MonthlyPaymentDueDay,
            policy.OverdueStartDay,
            policy.LateFeeDivisorDays,
            policy.EarlyReturnWaiveFeeUntilDay,
            policy.CreatedAt);
}
