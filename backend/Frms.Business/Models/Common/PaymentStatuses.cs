namespace Frms.Business.Models.Common;

/// <summary>
/// Canonical Payment.Status values. Allowed transitions:
/// PENDING -> SUCCESS and PENDING -> FAILED. A retry creates another Payment attempt
/// rather than moving FAILED back to PENDING.
/// </summary>
public static class PaymentStatuses
{
    public const string Pending = "PENDING";
    public const string Success = "SUCCESS";
    public const string Failed = "FAILED";
}
