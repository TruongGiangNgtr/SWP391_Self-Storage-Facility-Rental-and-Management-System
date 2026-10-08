using Frms.DataAccess.Repositories.Models;

namespace Frms.DataAccess.Repositories.Interfaces;

public interface IPaymentRepository
{
    Task<PaymentInvoiceRecord?> GetInvoiceAsync(Guid invoiceId, CancellationToken cancellationToken);
    Task<PaymentDetailRecord?> GetByIdAsync(Guid paymentId, CancellationToken cancellationToken);
    Task<PaymentDetailRecord?> GetByProviderOrderCodeAsync(long providerOrderCode, CancellationToken cancellationToken);

    // Scope to the requested Invoice before returning any session/key information.
    Task<PaymentAttemptResult> GetByIdempotencyKeyAsync(
        Guid invoiceId, Guid idempotencyKey, DateTimeOffset now, CancellationToken cancellationToken);
    Task<PaymentAttemptResult> CreateOrGetAsync(
        Guid invoiceId, Guid idempotencyKey, DateTimeOffset now, CancellationToken cancellationToken);
    Task<PaymentAttemptResult> SaveSessionAsync(
        Guid paymentId, PaymentSessionRecord session, DateTimeOffset now, CancellationToken cancellationToken);
    Task<PaymentApplyResult> ApplyResultAsync(NormalizedPaymentResult result, CancellationToken cancellationToken);
}
