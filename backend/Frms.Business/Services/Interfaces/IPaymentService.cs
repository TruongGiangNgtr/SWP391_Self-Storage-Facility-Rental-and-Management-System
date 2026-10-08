using Frms.Business.Models.Commands;
using Frms.Business.Models.Results;
using Frms.Business.Abstractions.External;

namespace Frms.Business.Services.Interfaces;

/// <summary>Business contract for VNPay Sandbox payment processing (EPS-01, PAY-001, PAY-003 and PAY-004).</summary>
public interface IPaymentService {
    /// <summary>PAY-001: starts a payment attempt for an existing Deposit/Rental Fee Invoice.</summary>
    Task<InvoicePaymentStartResult> StartInvoicePaymentAsync(
        Guid invoiceId,
        StartPaymentCommand command,
        CancellationToken cancellationToken = default);

    /// <summary>PAY-003: authorized Customer-own, assigned-Facility or BOM read.</summary>
    Task<PaymentResult> GetByIdAsync(
        Guid paymentId,
        CancellationToken cancellationToken = default);

    /// <summary>
    /// PAY-004: verifies untrusted input through the gateway before applying a normalized result.
    /// This is not a customer/employee command and does not define provider HTTP acknowledgement.
    /// </summary>
    Task<PaymentApplicationResult> ProcessCallbackAsync(
        PaymentGatewayCallbackRequest request,
        CancellationToken cancellationToken = default);
}
