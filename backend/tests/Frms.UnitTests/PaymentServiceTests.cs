using Frms.Business.Abstractions.External;
using Frms.Business.Abstractions.Security;
using Frms.Business.Abstractions.Time;
using Frms.Business.Exceptions;
using Frms.Business.Models.Commands;
using Frms.Business.Models.Results;
using Frms.Business.Services.Implementations;
using Frms.Business.Services.Interfaces;
using Frms.DataAccess.Repositories.Interfaces;
using Frms.DataAccess.Repositories.Models;

namespace Frms.UnitTests;

[TestFixture, Category("PaymentService")]
public sealed class PaymentServiceTests
{
    private static readonly DateTimeOffset Now = new(2026, 10, 7, 10, 0, 0, TimeSpan.Zero);
    private const string SessionUrl = "https://payments.example.invalid/session?token=private-fixture";
    private Harness h = null!;
    [SetUp] public void Setup() => h = new();

    [TestCase(true), TestCase(false)]
    public void UnauthenticatedActor_IsDeniedBeforeLookup(bool start)
    {
        h.Current.Authenticated = false;
        Denied(start, "UNAUTHORIZED", 401);
        Assert.That(h.Auth.Calls, Is.Zero);
    }

    [TestCase(true), TestCase(false)]
    public void InactiveActor_IsDeniedBeforeLookup(bool start)
    {
        h.Auth.Actor = h.Auth.Actor with { Status = "INACTIVE" };
        Denied(start, "ACCOUNT_INACTIVE", 403);
    }

    [TestCase("SYSTEM_ADMINISTRATOR", true), TestCase("SYSTEM_ADMINISTRATOR", false)]
    [TestCase("UNKNOWN", true), TestCase("UNKNOWN", false)]
    [TestCase("FACILITY_STAFF", true), TestCase("FACILITY_MANAGER", true)]
    [TestCase("BUSINESS_OPERATIONS_MANAGER", true)]
    public void UnsupportedRole_IsDeniedBeforeLookup(string role, bool start)
    {
        h.Auth.Actor = h.Auth.Actor with { Role = role };
        Denied(start, "FORBIDDEN", 403);
    }

    private void Denied(bool start, string code, int status)
    {
        var error = Assert.ThrowsAsync<BusinessException>(async () =>
        { if (start) await h.Start(); else await h.Service.GetByIdAsync(h.Repo.Detail.PaymentId); });
        Assert.That(error!.Code, Is.EqualTo(code));
        Assert.That(error.SuggestedStatusCode, Is.EqualTo(status));
        Assert.That(h.Repo.Events, Is.Empty);
        Assert.That(h.Gateway.Creates, Is.Zero);
    }

    [TestCase(true), TestCase(false)]
    public void AnotherCustomer_IsDeniedWithoutSessionOrKeyDisclosure(bool start)
    {
        h.Auth.Actor = h.Auth.Actor with { CustomerId = Guid.NewGuid() };
        var error = Assert.ThrowsAsync<BusinessException>(async () =>
        { if (start) await h.Start(); else await h.Service.GetByIdAsync(h.Repo.Detail.PaymentId); });
        Assert.That(error!.Code, Is.EqualTo("FORBIDDEN"));
        Assert.That(error.SuggestedStatusCode, Is.EqualTo(403));
        Assert.That(error.ToString(), Does.Not.Contain(SessionUrl).And.Not.Contain(h.Key.ToString()));
        Assert.That(h.Repo.Events, Is.EqualTo(new[] { start ? "invoice" : "detail" }));
        Assert.That(h.Gateway.Creates, Is.Zero);
    }

    [TestCase(true), TestCase(false)]
    public void MissingResource_Returns404(bool start)
    {
        h.Repo.Missing = true;
        var error = Assert.ThrowsAsync<BusinessException>(async () =>
        { if (start) await h.Start(); else await h.Service.GetByIdAsync(h.Repo.Detail.PaymentId); });
        Assert.That(error!.SuggestedStatusCode, Is.EqualTo(404));
        Assert.That(h.Repo.Events, Does.Not.Contain("key"));
    }

    [TestCase("CUSTOMER"), TestCase("FACILITY_STAFF"), TestCase("FACILITY_MANAGER")]
    [TestCase("BUSINESS_OPERATIONS_MANAGER")]
    public async Task AuthorizedDetail_IsBusinessProjectionWithoutSession(string role)
    {
        h.Auth.Actor = h.Auth.Actor with { Role = role };
        var result = await h.Service.GetByIdAsync(h.Repo.Detail.PaymentId);
        Assert.That(result.PaymentId, Is.EqualTo(h.Repo.Detail.PaymentId));
        Assert.That(result.InvoiceId, Is.EqualTo(h.Repo.Detail.InvoiceId));
        Assert.That(result.Amount, Is.EqualTo(h.Repo.Detail.Amount));
        Assert.That(typeof(PaymentResult).GetProperties().Select(p => p.Name), Is.EquivalentTo(new[]
        { "PaymentId", "InvoiceId", "Amount", "PaymentMethod", "TransactionCode", "Status", "PaidAt", "CreatedAt" }));
        Assert.That(h.Auth.Calls, Is.EqualTo(1));
    }

    [TestCase("FACILITY_STAFF"), TestCase("FACILITY_MANAGER")]
    public async Task CurrentFacilityAssignment_IsRecheckedOnEachRead(string role)
    {
        h.Auth.Actor = h.Auth.Actor with { Role = role };
        await h.Service.GetByIdAsync(h.Repo.Detail.PaymentId);
        h.Auth.Actor = h.Auth.Actor with { FacilityId = Guid.NewGuid() };
        Assert.That(Assert.ThrowsAsync<BusinessException>(() => h.Service.GetByIdAsync(h.Repo.Detail.PaymentId))!.Code,
            Is.EqualTo("FORBIDDEN"));
        h.Auth.Actor = h.Auth.Actor with { FacilityId = null };
        Assert.That(Assert.ThrowsAsync<BusinessException>(() => h.Service.GetByIdAsync(h.Repo.Detail.PaymentId))!.Code,
            Is.EqualTo("FORBIDDEN"));
        Assert.That(h.Auth.Calls, Is.EqualTo(3));
    }

    [Test]
    public async Task Bom_ReadDoesNotRequireFacilityAssignment()
    {
        h.Auth.Actor = h.Auth.Actor with { Role = "BUSINESS_OPERATIONS_MANAGER", FacilityId = null };
        Assert.That((await h.Service.GetByIdAsync(h.Repo.Detail.PaymentId)).PaymentId, Is.EqualTo(h.Repo.Detail.PaymentId));
    }

    [TestCase("DEPOSIT", "UNPAID"), TestCase("DEPOSIT", "OVERDUE")]
    [TestCase("RENTAL_FEE", "UNPAID"), TestCase("RENTAL_FEE", "OVERDUE")]
    public async Task EligibleInvoice_CreatesSessionWithPersistedAmountAndIdentity(string type, string status)
    {
        h.Repo.Invoice = h.Repo.Invoice with { InvoiceType = type, Status = status, AmountDue = 999m };
        var result = await h.Start();
        Assert.That(h.Repo.Events, Is.EqualTo(new[] { "invoice", "key", "create", "save" }));
        Assert.That(h.Gateway.Request!.Amount, Is.EqualTo(125.50m), "Use persisted Payment, not pre-lock Invoice projection.");
        Assert.That(h.Gateway.Request.PaymentId, Is.EqualTo(h.Repo.Detail.PaymentId));
        Assert.That(result.Outcome, Is.EqualTo(PaymentStartOutcome.SessionAvailable));
        Assert.That(result.PaymentUrl == SessionUrl, Is.True);
        Assert.That(result.PaymentUrlExpiresAt, Is.Null);
        Assert.That(result.Status, Is.EqualTo("PENDING"));
        Assert.That(h.Repo.Applied, Is.Null);
        Assert.That(h.Gateway.Creates, Is.EqualTo(1));
    }

    [TestCase("DEPOSIT", "PAID", 1, false), TestCase("DEPOSIT", "CANCELLED", 1, false)]
    [TestCase("LATE_FEE", "UNPAID", 1, false), TestCase("EXTRA_FEE", "UNPAID", 1, false)]
    [TestCase("DAMAGE", "UNPAID", 1, false), TestCase("RENTAL_FEE", "UNPAID", 1, true)]
    [TestCase("DEPOSIT", "UNPAID", 0, false), TestCase("DEPOSIT", "UNPAID", -1, false)]
    public void IneligibleNewInvoice_DoesNotCreateOrCallGateway(string type, string status, int amount, bool firstMonth)
    {
        h.Repo.Invoice = h.Repo.Invoice with { InvoiceType = type, Status = status, AmountDue = amount,
            BillingMonth = firstMonth ? h.Repo.Invoice.ContractStartMonth : h.Repo.Invoice.BillingMonth };
        var error = Assert.ThrowsAsync<BusinessException>(() => h.Start());
        Assert.That(error!.Code, Is.EqualTo("INVOICE_NOT_PAYABLE"));
        Assert.That(h.Repo.Events, Is.EqualTo(new[] { "invoice", "key" }));
        Assert.That(h.Gateway.Creates, Is.Zero);
    }

    [TestCase("PAID"), TestCase("CANCELLED")]
    public async Task ExistingAttempt_BypassesNewEligibilityAndIgnoresChangedReturnUrl(string invoiceStatus)
    {
        h.Existing(SessionUrl);
        h.Repo.Invoice = h.Repo.Invoice with { Status = invoiceStatus };
        var first = await h.Start();
        var retry = await h.Service.StartInvoicePaymentAsync(h.Repo.Invoice.InvoiceId,
            new(h.Key, "https://different.example.invalid/return"));
        Assert.That(retry == first, Is.True);
        Assert.That(retry.PaymentId, Is.EqualTo(h.Repo.Detail.PaymentId));
        Assert.That(retry.PaymentUrl == SessionUrl, Is.True);
        Assert.That(h.Gateway.Creates, Is.Zero);
        Assert.That(h.Repo.Events, Is.EqualTo(new[] { "invoice", "key", "invoice", "key" }));
    }

    [TestCase("SUCCESS"), TestCase("FAILED")]
    public async Task TerminalAttempt_HidesStaleSessionAndNeverCreates(string status)
    {
        h.Existing(SessionUrl, Now.AddMinutes(-1), status);
        var result = await h.Start();
        Assert.That(result.Status, Is.EqualTo(status));
        Assert.That(result.Outcome, Is.EqualTo(PaymentStartOutcome.Terminal));
        Assert.That(result.PaymentUrl, Is.Null);
        Assert.That(result.PaymentUrlExpiresAt, Is.Null);
        Assert.That(h.Gateway.Creates, Is.Zero);
    }

    [TestCase(-1), TestCase(0), TestCase(1)]
    public async Task KnownExpiry_UsesExactUtcBoundary(int seconds)
    {
        h.Existing(SessionUrl, Now.AddSeconds(seconds));
        if (seconds <= 0)
            Assert.That(Assert.ThrowsAsync<BusinessException>(() => h.Start())!.Code, Is.EqualTo("PAYMENT_SESSION_EXPIRED"));
        else
            Assert.That((await h.Start()).PaymentUrlExpiresAt, Is.EqualTo(Now.AddSeconds(1)));
        Assert.That(h.Gateway.Creates, Is.Zero);
    }

    [TestCase(false), TestCase(true)]
    public async Task ExistingWithoutSession_IncludingAtomicRace_NeverCreatesAgain(bool atomicRace)
    {
        if (atomicRace) h.Repo.CreateOutcome = PaymentAttemptOutcome.Existing;
        else h.Existing(null);
        var result = await h.Start();
        Assert.That(result.PaymentId, Is.EqualTo(h.Repo.Detail.PaymentId));
        Assert.That(result.Outcome, Is.EqualTo(PaymentStartOutcome.SessionUnavailable));
        Assert.That(result.Status, Is.EqualTo("PENDING"));
        Assert.That(result.PaymentUrl, Is.Null);
        Assert.That(h.Gateway.Creates, Is.Zero);
        Assert.That(h.Repo.Applied, Is.Null);
    }

    [TestCase(PaymentAttemptOutcome.IdempotencyConflict, "PAYMENT_IDEMPOTENCY_CONFLICT")]
    [TestCase(PaymentAttemptOutcome.SessionExpired, "PAYMENT_SESSION_EXPIRED")]
    [TestCase(PaymentAttemptOutcome.InvoiceNotPayable, "INVOICE_NOT_PAYABLE")]
    public void ControlledRepositoryConflict_DoesNotDiscloseAttempt(PaymentAttemptOutcome outcome, string code)
    {
        h.Repo.LookupOverride = new(outcome, null);
        var error = Assert.ThrowsAsync<BusinessException>(() => h.Start());
        Assert.That(error!.Code, Is.EqualTo(code));
        Assert.That(error.SuggestedStatusCode, Is.EqualTo(409));
        Assert.That(error.ToString(), Does.Not.Contain(h.Repo.Detail.PaymentId.ToString()).And.Not.Contain(h.Key.ToString()).And.Not.Contain(SessionUrl));
        Assert.That(h.Gateway.Creates, Is.Zero);
    }

    [Test]
    public async Task DefinitiveRejection_UsesOnlyDefinitiveFailurePath()
    {
        h.Gateway.Creation = new(PaymentGatewayCreationOutcome.DefinitiveRejection, null, null);
        var result = await h.Start();
        Assert.That(h.Repo.Applied, Is.EqualTo(new NormalizedPaymentResult(h.Repo.Detail.PaymentId,
            PaymentFinalStatus.Failed, null, null, null, PaymentResultSource.DefinitivePreSessionFailure)));
        Assert.That(h.Repo.ApplyCalls, Is.EqualTo(1));
        Assert.That(result.Status, Is.EqualTo("FAILED"));
        Assert.That(result.Outcome, Is.EqualTo(PaymentStartOutcome.Terminal));
    }

    [Test]
    public async Task UnknownProviderOutcome_RemainsPendingWithoutFailureWrite()
    {
        h.Gateway.Creation = new(PaymentGatewayCreationOutcome.Unknown, null, null);
        var result = await h.Start();
        Assert.That(result.Outcome, Is.EqualTo(PaymentStartOutcome.SessionUnavailable));
        Assert.That(result.Status, Is.EqualTo("PENDING"));
        Assert.That(h.Repo.Applied, Is.Null);
    }

    [Test]
    public async Task KnownProviderExpiry_IsPersistedInUtcWithoutInventedInterval()
    {
        var expiry = new DateTimeOffset(2026, 10, 7, 18, 0, 0, TimeSpan.FromHours(7));
        h.Gateway.Creation = new(PaymentGatewayCreationOutcome.SessionCreated, new(SessionUrl, null, expiry), null);
        var result = await h.Start();
        Assert.That(h.Repo.Attempt!.PaymentUrlExpiresAt, Is.EqualTo(expiry));
        Assert.That(h.Repo.Attempt.PaymentUrlExpiresAt!.Value.Offset, Is.EqualTo(TimeSpan.Zero));
        Assert.That(result.PaymentUrlExpiresAt, Is.EqualTo(expiry.ToUniversalTime()));
        Assert.That(h.Repo.ApplyCalls, Is.Zero);
    }

    [TestCase("missing-session"), TestCase("invalid-url"), TestCase("invalid-reference"), TestCase("contradictory-rejection")]
    public async Task MalformedCreationResult_DoesNotInventFailureOrPersistInvalidSession(string kind)
    {
        h.Gateway.Creation = kind switch
        {
            "missing-session" => new(PaymentGatewayCreationOutcome.SessionCreated, null, null),
            "invalid-url" => new(PaymentGatewayCreationOutcome.SessionCreated, new("not-a-url", null, null), null),
            "invalid-reference" => new(PaymentGatewayCreationOutcome.DefinitiveRejection, null, " "),
            _ => new(PaymentGatewayCreationOutcome.DefinitiveRejection, new(SessionUrl, null, null), null)
        };
        var result = await h.Start();
        Assert.That(result.Outcome, Is.EqualTo(PaymentStartOutcome.SessionUnavailable));
        Assert.That(result.Status, Is.EqualTo("PENDING"));
        Assert.That(h.Repo.Events, Does.Not.Contain("save"));
        Assert.That(h.Repo.ApplyCalls, Is.Zero);
    }

    [TestCase(true), TestCase(false)]
    public void InvalidStartInput_IsRejectedAfterOwnershipAndBeforeKeyLookup(bool emptyKey)
    {
        var command = emptyKey ? new StartPaymentCommand(Guid.Empty, "https://app.example.invalid/return") : new(h.Key, "not-a-url");
        Assert.That(Assert.ThrowsAsync<BusinessException>(() => h.Service.StartInvoicePaymentAsync(h.Repo.Invoice.InvoiceId, command))!.Code,
            Is.EqualTo("VALIDATION_ERROR"));
        Assert.That(h.Repo.Events, Is.EqualTo(new[] { "invoice" }));
        Assert.That(h.Gateway.Creates, Is.Zero);
    }

    [Test]
    public async Task EmptyCallback_IsRejectedBeforeVerification()
    {
        Assert.That((await h.Service.ProcessCallbackAsync(new(" "))).Outcome, Is.EqualTo(PaymentApplicationOutcome.InvalidResult));
        Assert.That(h.Gateway.Verifies, Is.Zero);
        Assert.That(h.Repo.ApplyCalls, Is.Zero);
    }

    [TestCase("timeout"), TestCase("transport"), TestCase("cancellation"), TestCase("save")]
    public async Task UncertainFailure_DoesNotMarkFailedOrCreateAgainOnRetry(string kind)
    {
        Exception error = kind switch { "timeout" => new TimeoutException(), "transport" => new HttpRequestException(),
            "cancellation" => new OperationCanceledException(), _ => new InvalidOperationException("Session save unavailable") };
        if (kind == "save") h.Repo.SaveError = error; else h.Gateway.CreateError = error;
        Assert.That(Assert.CatchAsync<Exception>(() => h.Start()), Is.SameAs(error));
        var retry = await h.Start();
        Assert.That(retry.PaymentId, Is.EqualTo(h.Repo.Detail.PaymentId));
        Assert.That(retry.Status, Is.EqualTo("PENDING"));
        Assert.That(retry.Outcome, Is.EqualTo(PaymentStartOutcome.SessionUnavailable));
        Assert.That(h.Repo.ApplyCalls, Is.Zero);
        Assert.That(h.Gateway.Creates, Is.EqualTo(1));
    }

    [Test]
    public async Task CallbackBeforeSessionSave_ReturnsPersistedTerminalState()
    {
        h.Repo.TerminalAtSave = true;
        var result = await h.Start();
        Assert.That(result.Status, Is.EqualTo("SUCCESS"));
        Assert.That(result.Outcome, Is.EqualTo(PaymentStartOutcome.Terminal));
        Assert.That(result.PaymentUrl, Is.Null);
        Assert.That(h.Repo.ApplyCalls, Is.Zero, "The Service must not reapply the callback during session save.");
    }

    [TestCase(PaymentGatewayCallbackOutcome.Unverified, PaymentApplicationOutcome.VerificationRejected)]
    [TestCase(PaymentGatewayCallbackOutcome.Unknown, PaymentApplicationOutcome.Unresolved)]
    [TestCase(PaymentGatewayCallbackOutcome.Pending, PaymentApplicationOutcome.Unresolved)]
    public async Task NonFinalOrUnverifiedCallback_CannotWrite(PaymentGatewayCallbackOutcome status, PaymentApplicationOutcome expected)
    {
        h.Gateway.Verified = h.Gateway.Verified with { Status = status };
        Assert.That((await h.Callback()).Outcome, Is.EqualTo(expected));
        Assert.That(h.Gateway.Verifies, Is.EqualTo(1));
        Assert.That(h.Repo.ApplyCalls, Is.Zero);
        Assert.That(h.Auth.Calls, Is.Zero, "Actor identity is not callback authentication.");
    }

    [Test]
    public void VerificationException_CannotReachFinancialWrite()
    {
        h.Gateway.VerifyError = new InvalidOperationException("Verification unavailable");
        Assert.ThrowsAsync<InvalidOperationException>(() => h.Callback());
        Assert.That(h.Repo.ApplyCalls, Is.Zero);
    }

    [TestCase("id"), TestCase("amount"), TestCase("time"), TestCase("reference"), TestCase("status")]
    public async Task MissingMandatoryVerifiedData_IsRejectedWithoutWrite(string field)
    {
        var value = h.Gateway.Verified;
        h.Gateway.Verified = field switch
        {
            "id" => value with { PaymentId = Guid.Empty }, "amount" => value with { Amount = null },
            "time" => value with { PaidAt = null }, "reference" => value with { TransactionCode = null },
            _ => value with { Status = (PaymentGatewayCallbackOutcome)100 }
        };
        Assert.That((await h.Callback()).Outcome, Is.EqualTo(PaymentApplicationOutcome.InvalidResult));
        Assert.That(h.Repo.ApplyCalls, Is.Zero);
    }

    [Test]
    public async Task VerifiedMapping_PreservesExactAmountReferenceAndUtcInstant()
    {
        var providerTime = new DateTimeOffset(2026, 10, 7, 17, 2, 3, TimeSpan.FromHours(7));
        h.Gateway.Verified = h.Gateway.Verified with { Amount = 125.5001m, PaidAt = providerTime, TransactionCode = "gateway-ref-not-order-id" };
        h.Repo.ApplyOutcome = PaymentApplyOutcome.AmountMismatch;
        var result = await h.Callback();
        Assert.That(h.Repo.Applied, Is.EqualTo(new NormalizedPaymentResult(h.Repo.Detail.PaymentId,
            PaymentFinalStatus.Success, "gateway-ref-not-order-id", 125.5001m, providerTime.ToUniversalTime(), PaymentResultSource.VerifiedCallback)));
        Assert.That(h.Repo.Applied!.VerifiedPaidAt!.Value.Offset, Is.EqualTo(TimeSpan.Zero));
        Assert.That(result.Outcome, Is.EqualTo(PaymentApplicationOutcome.AmountMismatch));
        Assert.That(h.Events, Is.EqualTo(new[] { "verify", "apply" }));
    }

    [Test]
    public async Task FailedCallback_DoesNotAcquireSuccessfulPaidAt()
    {
        h.Gateway.Verified = h.Gateway.Verified with { Status = PaymentGatewayCallbackOutcome.Failed };
        await h.Callback();
        Assert.That(h.Repo.Applied!.Status, Is.EqualTo(PaymentFinalStatus.Failed));
        Assert.That(h.Repo.Applied.VerifiedPaidAt, Is.Null);
        Assert.That(h.Repo.Applied.Source, Is.EqualTo(PaymentResultSource.VerifiedCallback));
    }

    [TestCase(PaymentApplyOutcome.Applied, PaymentApplicationOutcome.Applied)]
    [TestCase(PaymentApplyOutcome.Duplicate, PaymentApplicationOutcome.Duplicate)]
    [TestCase(PaymentApplyOutcome.NotFound, PaymentApplicationOutcome.NotFound)]
    [TestCase(PaymentApplyOutcome.InvalidResult, PaymentApplicationOutcome.InvalidResult)]
    [TestCase(PaymentApplyOutcome.AmountMismatch, PaymentApplicationOutcome.AmountMismatch)]
    [TestCase(PaymentApplyOutcome.ReferenceConflict, PaymentApplicationOutcome.ReferenceConflict)]
    [TestCase(PaymentApplyOutcome.TerminalConflict, PaymentApplicationOutcome.TerminalConflict)]
    public async Task RepositoryOutcomeAndReason_ArePreservedOnce(PaymentApplyOutcome input, PaymentApplicationOutcome expected)
    {
        h.Repo.ApplyOutcome = input;
        h.Repo.Reason = "LATE_SUCCESS_ON_CANCELLED_INVOICE";
        var result = await h.Callback();
        Assert.That(result.Outcome, Is.EqualTo(expected));
        Assert.That(result.Reason, Is.EqualTo(h.Repo.Reason));
        Assert.That(result.Payment!.PaymentId, Is.EqualTo(h.Repo.Detail.PaymentId));
        Assert.That(h.Repo.ApplyCalls, Is.EqualTo(1));
        Assert.That(h.Repo.Events, Is.EqualTo(new[] { "apply" }));
    }

    [Test]
    public async Task CancellationToken_IsPropagatedAcrossEveryBoundary()
    {
        using var source = new CancellationTokenSource();
        await h.Service.StartInvoicePaymentAsync(h.Repo.Invoice.InvoiceId, new(h.Key, "https://app.example.invalid/return"), source.Token);
        await h.Service.GetByIdAsync(h.Repo.Detail.PaymentId, source.Token);
        await h.Service.ProcessCallbackAsync(new("untrusted-test-input"), source.Token);
        Assert.That(h.Tokens.Count, Is.GreaterThanOrEqualTo(10));
        Assert.That(h.Tokens.All(t => t == source.Token), Is.True);
        source.Cancel();
        var calls = h.Tokens.Count;
        Assert.CatchAsync<OperationCanceledException>(() => h.Service.ProcessCallbackAsync(new("untrusted-test-input"), source.Token));
        Assert.That(h.Tokens.Count, Is.EqualTo(calls));
    }

    [Test]
    public void SensitiveRecords_AreRedactedAndNormalizedWriteIsNotPublic()
    {
        var records = new object[] { new StartPaymentCommand(h.Key, SessionUrl), new PaymentGatewayRequest(h.Repo.Detail.PaymentId, 1m, SessionUrl),
            new PaymentGatewaySession(SessionUrl, null, null), new PaymentGatewayCallbackRequest("private-payload"),
            new PaymentGatewayCreationResult(PaymentGatewayCreationOutcome.SessionCreated, new(SessionUrl, null, null), null),
            new InvoicePaymentStartResult(h.Repo.Detail.PaymentId, h.Repo.Invoice.InvoiceId, 1m, "MOMO", "PENDING", SessionUrl, PaymentStartOutcome.SessionAvailable, null) };
        foreach (var record in records)
            Assert.That(record.ToString(), Does.Not.Contain(SessionUrl).And.Not.Contain("private-payload").And.Not.Contain(h.Key.ToString()));
        Assert.That(typeof(IPaymentService).GetMethods().Select(m => m.Name), Does.Not.Contain("ApplyPaymentResultAsync"));
        Assert.That(typeof(PaymentService).GetMethods().Select(m => m.Name), Does.Not.Contain("ApplyPaymentResultAsync"));
    }

    private sealed class Harness
    {
        public readonly Guid Key = Guid.NewGuid();
        public readonly List<string> Events = [];
        public readonly List<CancellationToken> Tokens = [];
        public readonly Context Current = new();
        public readonly Auth Auth;
        public readonly Repo Repo;
        public readonly Gateway Gateway;
        public PaymentService Service { get; }
        public Harness()
        {
            var customer = Guid.NewGuid(); var facility = Guid.NewGuid(); var invoice = Guid.NewGuid();
            Auth = new(new(Current.UserAccountId, "CUSTOMER", "ACTIVE", "", "", customer, Guid.NewGuid(), facility, "Test"), Tokens);
            Repo = new(new() { InvoiceId = invoice, InvoiceType = "DEPOSIT", Status = "UNPAID", AmountDue = 125.50m,
                CustomerId = customer, CustomerUserAccountId = Current.UserAccountId, FacilityId = facility,
                BillingMonth = new(2026, 11, 1), ContractStartMonth = new(2026, 10, 1) }, Key, Events, Tokens);
            Gateway = new(Repo.Detail.PaymentId, Events, Tokens);
            Service = new(Repo, Gateway, Current, Auth, new Clock());
        }
        public Task<InvoicePaymentStartResult> Start() => Service.StartInvoicePaymentAsync(Repo.Invoice.InvoiceId, new(Key, "https://app.example.invalid/return"));
        public Task<PaymentApplicationResult> Callback() => Service.ProcessCallbackAsync(new("untrusted-test-input"));
        public void Existing(string? url, DateTimeOffset? expires = null, string status = "PENDING") =>
            Repo.Attempt = new(Repo.Detail with { Status = status }, Key, url, expires);
    }

    private sealed class Context : ICurrentUserContext
    {
        public bool Authenticated = true;
        public bool IsAuthenticated => Authenticated;
        public Guid UserAccountId { get; } = Guid.NewGuid();
        public string Role => throw new AssertionException("Do not trust the JWT role convenience claim.");
    }
    private sealed class Clock : IClock
    {
        public DateTimeOffset UtcNow => Now;
        public TimeZoneInfo BusinessTimeZone => TimeZoneInfo.Utc;
        public DateTimeOffset ToBusinessTime(DateTimeOffset value) => value;
    }
    private sealed class Auth(CurrentAccountResult actor, List<CancellationToken> tokens) : IAuthenticationService
    {
        public CurrentAccountResult Actor = actor;
        public int Calls;
        public Task<CurrentAccountResult> GetCurrentAccountAsync(Guid id, CancellationToken ct)
        { Calls++; tokens.Add(ct); Assert.That(id, Is.EqualTo(Actor.UserAccountId)); return Task.FromResult(Actor); }
        public Task<AuthenticationResult> LoginCustomerAsync(CustomerLoginCommand c, CancellationToken t) => throw new NotSupportedException();
        public Task<AuthenticationResult> LoginEmployeeAsync(EmployeeLoginCommand c, CancellationToken t) => throw new NotSupportedException();
        public Task<bool> IsActiveAsync(Guid id, string role, CancellationToken t) => throw new NotSupportedException();
    }
    private sealed class Gateway(Guid paymentId, List<string> events, List<CancellationToken> tokens) : IPaymentGateway
    {
        public int Creates, Verifies;
        public PaymentGatewayRequest? Request;
        public Exception? CreateError, VerifyError;
        public PaymentGatewayCreationResult Creation = new(PaymentGatewayCreationOutcome.SessionCreated, new(SessionUrl, "session-ref", null), null);
        public PaymentGatewayCallbackResult Verified = new(paymentId, 125.50m, "callback-ref", PaymentGatewayCallbackOutcome.Success, Now);
        public Task EnsureConfiguredAsync(CancellationToken ct) { tokens.Add(ct); return Task.CompletedTask; }
        public Task<PaymentGatewayCreationResult> CreatePaymentAsync(PaymentGatewayRequest request, CancellationToken ct)
        { Creates++; tokens.Add(ct); Request = request; if (CreateError is not null) throw CreateError; return Task.FromResult(Creation); }
        public Task<PaymentGatewayCallbackResult> VerifyAndNormalizeCallbackAsync(PaymentGatewayCallbackRequest request, CancellationToken ct)
        { Verifies++; events.Add("verify"); tokens.Add(ct); if (VerifyError is not null) throw VerifyError; return Task.FromResult(Verified); }
    }
    private sealed class Repo(PaymentInvoiceRecord invoice, Guid key, List<string> events, List<CancellationToken> tokens) : IPaymentRepository
    {
        public PaymentInvoiceRecord Invoice = invoice;
        public PaymentDetailRecord Detail = new(Guid.NewGuid(), invoice.InvoiceId, 125.50m, "MOMO", null, "PENDING", null, Now,
            invoice.CustomerId, invoice.CustomerUserAccountId, invoice.FacilityId);
        public readonly List<string> Events = [];
        public bool Missing, TerminalAtSave;
        public PaymentAttemptRecord? Attempt;
        public PaymentAttemptResult? LookupOverride;
        public PaymentAttemptOutcome CreateOutcome = PaymentAttemptOutcome.Created;
        public PaymentApplyOutcome ApplyOutcome = PaymentApplyOutcome.Applied;
        public string? Reason;
        public NormalizedPaymentResult? Applied;
        public int ApplyCalls;
        public Exception? SaveError;
        private void Call(string name, CancellationToken ct) { Events.Add(name); events.Add(name); tokens.Add(ct); }
        public Task<PaymentInvoiceRecord?> GetInvoiceAsync(Guid id, CancellationToken ct)
        { Call("invoice", ct); return Task.FromResult(Missing ? null : Invoice); }
        public Task<PaymentDetailRecord?> GetByIdAsync(Guid id, CancellationToken ct)
        { Call("detail", ct); return Task.FromResult<PaymentDetailRecord?>(Missing ? null : Detail); }
        public Task<PaymentAttemptResult> GetByIdempotencyKeyAsync(Guid id, Guid k, DateTimeOffset now, CancellationToken ct)
        { Call("key", ct); Assert.That(k, Is.EqualTo(key)); return Task.FromResult(LookupOverride ?? new(Attempt is null ? PaymentAttemptOutcome.NotFound : PaymentAttemptOutcome.Existing, Attempt)); }
        public Task<PaymentAttemptResult> CreateOrGetAsync(Guid id, Guid k, DateTimeOffset now, CancellationToken ct)
        { Call("create", ct); Assert.That(k, Is.EqualTo(key)); Attempt = new(Detail, k, null, null); return Task.FromResult(new PaymentAttemptResult(CreateOutcome, Attempt)); }
        public Task<PaymentAttemptResult> SaveSessionAsync(Guid id, PaymentSessionRecord session, DateTimeOffset now, CancellationToken ct)
        {
            Call("save", ct); if (SaveError is not null) throw SaveError;
            Attempt = TerminalAtSave ? Attempt! with { Detail = Detail with { Status = "SUCCESS", PaidAt = Now } }
                : Attempt! with { PaymentUrl = session.PaymentUrl, PaymentUrlExpiresAt = session.ExpiresAt };
            return Task.FromResult(new PaymentAttemptResult(PaymentAttemptOutcome.Existing, Attempt));
        }
        public Task<PaymentApplyResult> ApplyResultAsync(NormalizedPaymentResult result, CancellationToken ct)
        {
            Call("apply", ct); ApplyCalls++; Applied = result;
            if (Attempt is not null && ApplyOutcome == PaymentApplyOutcome.Applied)
                Attempt = Attempt with { Detail = Detail with { Status = result.Status == PaymentFinalStatus.Success ? "SUCCESS" : "FAILED", PaidAt = result.VerifiedPaidAt } };
            return Task.FromResult(new PaymentApplyResult(ApplyOutcome, Detail, Reason));
        }
    }
}
