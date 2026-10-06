using Frms.Business.Models.Commands;
using Frms.Business.Models.Results;

namespace Frms.Business.Services.Interfaces;

/// <summary>Business contract for MoMo Sandbox payment processing (EPS-01, PAY-001..004).</summary>
public interface IPaymentService {
    /// <summary>PAY-001: starts a payment attempt for an existing Deposit/Rental Fee Invoice.</summary>
    Task<InvoicePaymentStartResult> StartInvoicePaymentAsync(
        Guid invoiceId,
        StartPaymentCommand command,
        CancellationToken cancellationToken = default);

    /// <summary>
    /// PAY-002: starts the first-month pre-handover payment for a Reservation.
    /// Amount = Reservation.LockedRentalPrice, no Contract Discount, and Payment.InvoiceId is null
    /// until Complete Handover links the first Rental Fee Invoice.
    /// </summary>
    Task<FirstMonthPaymentStartResult> StartFirstMonthPaymentAsync(
        Guid reservationId,
        StartPaymentCommand command,
        CancellationToken cancellationToken = default);

    /// <summary>PAY-003: returns the authoritative status of the caller's own Payment.</summary>
    Task<PaymentResult> GetByIdAsync(
        Guid paymentId,
        CancellationToken cancellationToken = default);

    /// <summary>
    /// PAY-004: applies a payment result already verified and normalized by the provider adapter.
    /// Must be idempotent by gateway transaction/reference (BR-PAY-01).
    /// </summary>
    Task<PaymentResult> ApplyPaymentResultAsync(
        ApplyPaymentResultCommand command,
        CancellationToken cancellationToken = default);
}
