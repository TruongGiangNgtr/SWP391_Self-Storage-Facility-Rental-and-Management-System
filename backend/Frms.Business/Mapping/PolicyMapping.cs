using Frms.Business.Models.Results;
using Frms.DataAccess.Persistence.Entities;

namespace Frms.Business.Mapping;

internal static class PolicyMapping
{
    public static PolicyResult ToResult(this Policy policy) => new(
        policy.PolicyId, policy.Version, policy.Status,
        policy.EffectiveFrom, policy.EffectiveTo,
        policy.DepositTimeoutHours, policy.ReservationVisitStartDay,
        policy.ReservationVisitEndDay, policy.MonthlyPaymentDueDay,
        policy.OverdueStartDay, policy.LateFeeDivisorDays,
        policy.EarlyReturnWaiveFeeUntilDay, policy.CreatedAt);
}
