using Frms.Api.DTOs.Responses;
using Frms.Business.Models.Results;

namespace Frms.Api.Mapping;

internal static class PolicyResponseMapping
{
    public static PolicySummary ToSummary(this PolicyResult policy) => new(
        policy.PolicyId, policy.Version, policy.Status,
        policy.EffectiveFrom, policy.EffectiveTo, policy.DepositTimeoutHours,
        policy.ReservationVisitStartDay, policy.ReservationVisitEndDay,
        policy.MonthlyPaymentDueDay, policy.OverdueStartDay, policy.LateFeeDivisorDays,
        policy.EarlyReturnWaiveFeeUntilDay, policy.CreatedAt);
}
