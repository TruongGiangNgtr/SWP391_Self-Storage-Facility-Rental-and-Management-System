using Frms.Business.Models.Commands;
using Frms.Business.Models.Results;

namespace Frms.Business.Services.Interfaces;

/// <summary>Business contract for MoMo Sandbox payment processing (EPS-01, PAY-001, PAY-003 and PAY-004).</summary>
public interface IPaymentService {
    /// <summary>PAY-001: starts a payment attempt for an existing Deposit/Rental Fee Invoice.</summary>
    Task<InvoicePaymentStartResult> StartInvoicePaymentAsync(
        Guid invoiceId,
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
