namespace Frms.Business.Models.Commands;

/// <summary>
/// Starts a payment attempt (PAY-001 / PAY-002). The amount is never client-supplied;
/// it is derived server-side from the Invoice or the Reservation.
/// </summary>
public sealed record StartPaymentCommand(
    string ReturnUrl);

/// <summary>
/// Normalized payment result produced by the payment provider adapter after it has
/// verified provider authenticity/reference (PAY-004). Contains no provider wire fields.
/// </summary>
/// <param name="PaymentId">Internal Payment identifier resolved from the provider reference.</param>
/// <param name="TransactionCode">Gateway transaction/reference used for idempotency.</param>
/// <param name="Status">Final outcome: <c>SUCCESS</c> or <c>FAILED</c> only.</param>
public sealed record ApplyPaymentResultCommand(
    Guid PaymentId,
    string TransactionCode,
    string Status);
