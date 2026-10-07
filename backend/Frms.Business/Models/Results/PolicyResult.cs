namespace Frms.Business.Models.Results;

public sealed record PolicyResult(
    Guid PolicyId,
    int Version,
    string Status,
    DateTime EffectiveFrom,
    DateTime? EffectiveTo,
    int DepositTimeoutHours,
    int ReservationVisitStartDay,
    int ReservationVisitEndDay,
    int MonthlyPaymentDueDay,
    int OverdueStartDay,
    int LateFeeDivisorDays,
    int EarlyReturnWaiveFeeUntilDay,
    DateTime CreatedAt);
