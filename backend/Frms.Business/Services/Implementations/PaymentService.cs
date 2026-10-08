using Frms.Business.Abstractions.External;
using Frms.Business.Abstractions.Security;
using Frms.Business.Abstractions.Time;
using Frms.Business.Exceptions;
using Frms.Business.Models.Commands;
using Frms.Business.Models.Common;
using Frms.Business.Models.Results;
using Frms.Business.Services.Interfaces;
using Frms.DataAccess.Repositories.Interfaces;
using Frms.DataAccess.Repositories.Models;

namespace Frms.Business.Services.Implementations;

public sealed class PaymentService(
    IPaymentRepository repository,
    IPaymentGateway gateway,
    ICurrentUserContext currentUser,
    IAuthenticationService authentication,
    IClock clock) : IPaymentService
{
    public async Task<InvoicePaymentStartResult> StartInvoicePaymentAsync(
        Guid invoiceId, StartPaymentCommand command, CancellationToken cancellationToken = default)
    {
        var actor = await ActiveActorAsync(cancellationToken);
        if (actor.Role != "CUSTOMER") throw Forbidden();
        var invoice = await repository.GetInvoiceAsync(invoiceId, cancellationToken) ?? throw NotFound();
        if (!Owns(actor, invoice.CustomerId, invoice.CustomerUserAccountId)) throw Forbidden();
        if (command.IdempotencyKey == Guid.Empty)
            throw new BusinessException("VALIDATION_ERROR", "A UUID idempotency key is required.", 400);

        var existing = await repository.GetByIdempotencyKeyAsync(
            invoiceId, command.IdempotencyKey, clock.UtcNow, cancellationToken);
        if (existing.Outcome != PaymentAttemptOutcome.NotFound)
            return StartResult(existing);

        // Eligibility applies to a new attempt only. The repository rechecks under its Invoice lock.
        if (!Payable(invoice))
            throw NotPayable();
        if (invoice.AmountDue <= 0m || decimal.Truncate(invoice.AmountDue) != invoice.AmountDue) throw UnsupportedAmount();

        // Known configuration errors must not consume a new idempotency key.
        await gateway.EnsureConfiguredAsync(cancellationToken);
        gateway.ValidatePaymentRequest(new(invoice.AmountDue, command.ReturnUrl, command.ClientIpAddress));
        var created = await repository.CreateOrGetAsync(
            invoiceId, command.IdempotencyKey, clock.UtcNow, cancellationToken);
        if (created.Outcome != PaymentAttemptOutcome.Created)
            return StartResult(created);
        return await CreateSessionAsync(created, invoice.InvoiceId, command, cancellationToken);
    }

    private async Task<InvoicePaymentStartResult> CreateSessionAsync(PaymentAttemptResult result,
        Guid invoiceId, StartPaymentCommand command, CancellationToken cancellationToken)
    {
        var attempt = RequireAttempt(result);
        var request = new PaymentGatewayRequest(attempt.Detail.PaymentId, attempt.Detail.Amount,
            command.ReturnUrl, command.ClientIpAddress, attempt.Detail.CreatedAt, attempt.ProviderOrderCode);

        PaymentGatewayCreationResult provider;
        try
        {
            provider = await gateway.CreatePaymentAsync(request, cancellationToken);
        }
        catch (OperationCanceledException) when (cancellationToken.IsCancellationRequested) { throw; }
        catch (Exception)
        {
            return await ReloadAttemptAsync(invoiceId, command.IdempotencyKey, cancellationToken);
        }

        if (provider.Outcome == PaymentGatewayCreationOutcome.SessionCreated
            && provider.Session is { } session && ValidSession(session))
        {
            PaymentAttemptResult saved;
            try
            {
                saved = await repository.SaveSessionAsync(attempt.Detail.PaymentId,
                    new(session.TransactionCode, session.PaymentUrl, session.ExpiresAt?.ToUniversalTime()),
                    clock.UtcNow, cancellationToken);
            }
            catch (OperationCanceledException) when (cancellationToken.IsCancellationRequested) { throw; }
            catch (Exception)
            {
                return await ReloadAttemptAsync(invoiceId, command.IdempotencyKey, cancellationToken);
            }
            if (saved.Outcome != PaymentAttemptOutcome.ReferenceConflict)
            {
                var response = StartResult(saved);
                return response with { NewlyInitiated = response.Outcome == PaymentStartOutcome.SessionAvailable };
            }
            var current = await ReloadAttemptAsync(invoiceId, command.IdempotencyKey, cancellationToken);
            return current.Outcome == PaymentStartOutcome.Terminal ? current
                : current with { Outcome = PaymentStartOutcome.ReferenceConflict, PaymentUrl = null, PaymentUrlExpiresAt = null };
        }
        return await ReloadAttemptAsync(invoiceId, command.IdempotencyKey, cancellationToken);
    }

    public async Task<PaymentResult> GetByIdAsync(Guid paymentId, CancellationToken cancellationToken = default)
    {
        var actor = await ActiveActorAsync(cancellationToken);
        if (actor.Role is not ("CUSTOMER" or "FACILITY_STAFF" or "FACILITY_MANAGER" or "BUSINESS_OPERATIONS_MANAGER"))
            throw Forbidden();
        var payment = await repository.GetByIdAsync(paymentId, cancellationToken) ?? throw NotFound();
        var allowed = actor.Role switch
        {
            "CUSTOMER" => Owns(actor, payment.CustomerId, payment.CustomerUserAccountId),
            "FACILITY_STAFF" or "FACILITY_MANAGER" => actor.EmployeeId.HasValue
                && actor.FacilityId.HasValue && actor.FacilityId == payment.FacilityId,
            "BUSINESS_OPERATIONS_MANAGER" => true,
            _ => false
        };
        if (!allowed) throw Forbidden();
        return Detail(payment);
    }

    public async Task<PaymentApplicationResult> ProcessCallbackAsync(
        PaymentGatewayCallbackRequest request, CancellationToken cancellationToken = default)
    {
        cancellationToken.ThrowIfCancellationRequested();
        if (string.IsNullOrWhiteSpace(request.RawPayload)) return InvalidResult();
        // No actor role or caller-supplied verification flag authorizes a result write.
        var verified = await gateway.VerifyAndNormalizeCallbackAsync(request, cancellationToken);
        if (verified.Status == PaymentGatewayCallbackOutcome.Unverified)
            return new(PaymentApplicationOutcome.VerificationRejected, null, null);
        if (verified.Status is PaymentGatewayCallbackOutcome.Unknown or PaymentGatewayCallbackOutcome.Pending)
            return new(PaymentApplicationOutcome.Unresolved, null, null);
        if (verified.ProviderOrderCode is not { } orderCode) return InvalidResult();
        var payment = await repository.GetByProviderOrderCodeAsync(orderCode, cancellationToken);
        if (payment is null) return new(PaymentApplicationOutcome.NotFound, null, null);
        if (payment.PaymentMethod != "PAYOS") return InvalidResult();
        if (verified.Amount != payment.Amount)
            return new(PaymentApplicationOutcome.AmountMismatch, null, null);
        return await ApplyPaymentResultAsync(new(payment.PaymentId, verified.TransactionCode,
            verified.Status, verified.Amount, verified.PaidAt), cancellationToken);
    }

    // Only ProcessCallbackAsync may enter this normalized boundary after gateway verification.
    private async Task<PaymentApplicationResult> ApplyPaymentResultAsync(
        ApplyPaymentResultCommand command, CancellationToken cancellationToken)
    {
        if (command.PaymentId == Guid.Empty || string.IsNullOrWhiteSpace(command.TransactionCode)
            || command.TransactionCode.Length > 150
            || command.Status is not (PaymentGatewayCallbackOutcome.Success or PaymentGatewayCallbackOutcome.Failed)
            || (command.Status == PaymentGatewayCallbackOutcome.Success
                && (!command.VerifiedAmount.HasValue || !command.VerifiedPaidAt.HasValue)))
            return InvalidResult();

        var success = command.Status == PaymentGatewayCallbackOutcome.Success;
        // Do not round verified amounts. The SQL path independently enforces equality
        // even after the webhook boundary's no-write mismatch check.
        var result = await repository.ApplyResultAsync(new(command.PaymentId,
            success ? PaymentFinalStatus.Success : PaymentFinalStatus.Failed,
            command.TransactionCode, command.VerifiedAmount,
            success ? command.VerifiedPaidAt?.ToUniversalTime() : null,
            PaymentResultSource.VerifiedCallback), cancellationToken);
        var outcome = result.Outcome switch
        {
            PaymentApplyOutcome.Applied => PaymentApplicationOutcome.Applied,
            PaymentApplyOutcome.Duplicate => PaymentApplicationOutcome.Duplicate,
            PaymentApplyOutcome.NotFound => PaymentApplicationOutcome.NotFound,
            PaymentApplyOutcome.InvalidResult => PaymentApplicationOutcome.InvalidResult,
            PaymentApplyOutcome.AmountMismatch => PaymentApplicationOutcome.AmountMismatch,
            PaymentApplyOutcome.ReferenceConflict => PaymentApplicationOutcome.ReferenceConflict,
            PaymentApplyOutcome.TerminalConflict => PaymentApplicationOutcome.TerminalConflict,
            _ => throw new InvalidOperationException("Unexpected Payment repository outcome.")
        };
        return new(outcome, result.Payment is null ? null : Detail(result.Payment), result.Reason);
    }

    private async Task<CurrentAccountResult> ActiveActorAsync(CancellationToken cancellationToken)
    {
        cancellationToken.ThrowIfCancellationRequested();
        if (!currentUser.IsAuthenticated) throw new BusinessException("UNAUTHORIZED", "Authentication is required.", 401);
        var actor = await authentication.GetCurrentAccountAsync(currentUser.UserAccountId, cancellationToken);
        if (actor.Status != "ACTIVE") throw new BusinessException("ACCOUNT_INACTIVE", "The account is inactive.", 403);
        return actor;
    }

    private async Task<InvoicePaymentStartResult> ReloadAttemptAsync(Guid invoiceId, Guid key, CancellationToken cancellationToken) =>
        StartResult(await repository.GetByIdempotencyKeyAsync(invoiceId, key, clock.UtcNow, cancellationToken));

    private InvoicePaymentStartResult StartResult(PaymentAttemptResult result)
    {
        var attempt = RequireAttempt(result);
        var detail = attempt.Detail;
        if (detail.Status != PaymentStatuses.Pending)
            return new(detail.PaymentId, detail.InvoiceId, detail.Amount, detail.PaymentMethod, detail.Status,
                null, PaymentStartOutcome.Terminal, null);
        if (attempt.PaymentUrlExpiresAt <= clock.UtcNow) throw Expired();
        var usable = !string.IsNullOrWhiteSpace(attempt.PaymentUrl);
        return new(detail.PaymentId, detail.InvoiceId, detail.Amount, detail.PaymentMethod, detail.Status,
            usable ? attempt.PaymentUrl : null,
            usable ? PaymentStartOutcome.SessionAvailable : PaymentStartOutcome.SessionUnavailable,
            usable ? attempt.PaymentUrlExpiresAt : null);
    }

    private static PaymentAttemptRecord RequireAttempt(PaymentAttemptResult result) => result.Outcome switch
    {
        PaymentAttemptOutcome.IdempotencyConflict => throw new BusinessException(
            PaymentErrorCodes.IdempotencyConflict, "The idempotency key cannot be used for this Invoice.", 409),
        PaymentAttemptOutcome.InvoiceNotPayable => throw NotPayable(),
        PaymentAttemptOutcome.AmountUnsupported => throw UnsupportedAmount(),
        PaymentAttemptOutcome.SessionExpired => throw Expired(),
        PaymentAttemptOutcome.NotFound => throw NotFound(),
        PaymentAttemptOutcome.Created or PaymentAttemptOutcome.Existing => result.Attempt
            ?? throw new InvalidOperationException("Payment repository did not return the requested attempt."),
        _ => throw new InvalidOperationException("Unexpected Payment attempt outcome.")
    };

    private static PaymentResult Detail(PaymentDetailRecord payment) => new(payment.PaymentId, payment.InvoiceId,
        payment.Amount, payment.PaymentMethod, payment.TransactionCode, payment.Status, payment.PaidAt, payment.CreatedAt);
    private static bool Owns(CurrentAccountResult actor, Guid customerId, Guid accountId) =>
        actor.CustomerId == customerId && actor.UserAccountId == accountId;
    private static bool IsHttpUrl(string? url) => Uri.TryCreate(url, UriKind.Absolute, out var uri)
        && (uri.Scheme == Uri.UriSchemeHttps || uri.Scheme == Uri.UriSchemeHttp);
    private static bool ValidOptionalReference(string? reference) => reference is null
        || (!string.IsNullOrWhiteSpace(reference) && reference.Length <= 150);
    private static bool ValidSession(PaymentGatewaySession session) => IsHttpUrl(session.PaymentUrl)
        && session.PaymentUrl.Length <= 2048 && ValidOptionalReference(session.TransactionCode);
    private static BusinessException Forbidden() => new("FORBIDDEN", "Access to this resource is denied.", 403);
    private static BusinessException NotFound() => new("RESOURCE_NOT_FOUND", "The requested resource was not found.", 404);
    private static BusinessException NotPayable() => new(PaymentErrorCodes.InvoiceNotPayable, "The Invoice is not payable.", 409);
    private static BusinessException Expired() => new(PaymentErrorCodes.SessionExpired, "The payment session has expired.", 409);
    private static BusinessException UnsupportedAmount() => new(PaymentErrorCodes.AmountUnsupported, "The Invoice amount is not supported for payment initiation.", 409);
    private static bool Payable(PaymentInvoiceRecord invoice) => invoice.Status is "UNPAID" or "OVERDUE"
        && (invoice.InvoiceType == "DEPOSIT"
            || (invoice.InvoiceType == "RENTAL_FEE" && invoice.BillingMonth > invoice.ContractStartMonth));
    private static PaymentApplicationResult InvalidResult() => new(PaymentApplicationOutcome.InvalidResult, null, null);
}
