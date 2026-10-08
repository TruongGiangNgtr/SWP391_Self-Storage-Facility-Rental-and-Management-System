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
    private const string ReturnUrl = "https://app.example.invalid/payment/vnpay-return";
    private const string SessionUrl = "https://sandbox.vnpayment.vn/paymentv2/vpcpay.html?private=fixture";
    private Harness h = null!;

    [SetUp]
    public void SetUp() => h = new Harness();

    [Test]
    public async Task NewAttempt_UsesApprovedOrderingAndPersistsServerOwnedSession()
    {
        var result = await h.Start();

        Assert.Multiple(() =>
        {
            Assert.That(h.Events, Is.EqualTo(new[]
                { "invoice", "key", "configure", "validate", "create", "gateway-create", "save" }));
            Assert.That(h.Gateway.Request!.Amount, Is.EqualTo(h.Repo.Detail.Amount));
            Assert.That(h.Gateway.Request.ClientIpAddress, Is.EqualTo("203.0.113.10"));
            Assert.That(result.PaymentMethod, Is.EqualTo("VNPAY"));
            Assert.That(result.PaymentUrl, Is.EqualTo(SessionUrl));
            Assert.That(result.NewlyInitiated, Is.True);
            Assert.That(h.Repo.ApplyCalls, Is.Zero);
        });
    }

    [Test]
    public async Task ConfigurationFailure_DoesNotConsumeKey_AndRetryCanCreateNormally()
    {
        var expected = new BusinessException("EXTERNAL_PROVIDER_NOT_CONFIGURED", "not configured", 503);
        h.Gateway.ConfigurationError = expected;

        Assert.That(Assert.ThrowsAsync<BusinessException>(() => h.Start()), Is.SameAs(expected));
        Assert.Multiple(() =>
        {
            Assert.That(h.Events, Is.EqualTo(new[] { "invoice", "key", "configure" }));
            Assert.That(h.Repo.CreateCalls, Is.Zero);
            Assert.That(h.Gateway.Creates, Is.Zero);
            Assert.That(h.Repo.ApplyCalls, Is.Zero);
            Assert.That(h.Repo.Attempt, Is.Null);
        });

        h.Events.Clear();
        h.Gateway.ConfigurationError = null;
        var result = await h.Start();

        Assert.That(result.Outcome, Is.EqualTo(PaymentStartOutcome.SessionAvailable));
        Assert.That(h.Repo.CreateCalls, Is.EqualTo(1));
        Assert.That(h.Gateway.Creates, Is.EqualTo(1));
    }

    [Test]
    public void CancellationDuringConfiguration_DoesNotCreateAttemptOrFinancialResult()
    {
        using var source = new CancellationTokenSource();
        h.Gateway.Configure = token =>
        {
            source.Cancel();
            return Task.FromCanceled(token);
        };

        Assert.CatchAsync<OperationCanceledException>(() => h.Start(source.Token));
        Assert.Multiple(() =>
        {
            Assert.That(h.Events, Is.EqualTo(new[] { "invoice", "key", "configure" }));
            Assert.That(h.Repo.CreateCalls, Is.Zero);
            Assert.That(h.Gateway.Creates, Is.Zero);
            Assert.That(h.Repo.ApplyCalls, Is.Zero);
        });
    }

    [TestCase("PENDING", true, PaymentStartOutcome.SessionAvailable)]
    [TestCase("PENDING", false, PaymentStartOutcome.SessionUnavailable)]
    [TestCase("SUCCESS", true, PaymentStartOutcome.Terminal)]
    [TestCase("FAILED", true, PaymentStartOutcome.Terminal)]
    public async Task ExistingAuthorizedAttempt_BypassesGatewayConfiguration(
        string status, bool hasUrl, PaymentStartOutcome expected)
    {
        h.Existing(status, hasUrl ? SessionUrl : null);
        h.Gateway.ConfigurationError = new InvalidOperationException("unavailable");

        var result = await h.Start();

        Assert.Multiple(() =>
        {
            Assert.That(result.Outcome, Is.EqualTo(expected));
            Assert.That(result.PaymentUrl, Is.EqualTo(expected == PaymentStartOutcome.SessionAvailable ? SessionUrl : null));
            Assert.That(h.Gateway.ConfigurationChecks, Is.Zero);
            Assert.That(h.Gateway.Creates, Is.Zero);
            Assert.That(h.Repo.CreateCalls, Is.Zero);
        });
    }

    [Test]
    public async Task ConcurrentWinnerReturnedByCreate_DoesNotCreateSecondProviderSession()
    {
        h.Repo.CreateOutcome = PaymentAttemptOutcome.Existing;

        var result = await h.Start();

        Assert.Multiple(() =>
        {
            Assert.That(result.Outcome, Is.EqualTo(PaymentStartOutcome.SessionUnavailable));
            Assert.That(h.Events, Is.EqualTo(new[] { "invoice", "key", "configure", "validate", "create" }));
            Assert.That(h.Gateway.Creates, Is.Zero);
            Assert.That(h.Repo.CreateCalls, Is.EqualTo(1));
        });
    }

    [Test]
    public void ProviderPreflightFailure_HappensBeforeAtomicCreation()
    {
        h.Gateway.ValidationError = new BusinessException("PAYMENT_AMOUNT_UNSUPPORTED", "unsupported", 409);

        var error = Assert.ThrowsAsync<BusinessException>(() => h.Start());

        Assert.That(error!.Code, Is.EqualTo("PAYMENT_AMOUNT_UNSUPPORTED"));
        Assert.That(h.Events, Is.EqualTo(new[] { "invoice", "key", "configure", "validate" }));
        Assert.That(h.Repo.CreateCalls, Is.Zero);
        Assert.That(h.Gateway.Creates, Is.Zero);
    }

    [Test]
    public async Task ProviderTransportFailureAfterPersistence_ReturnsSamePendingAttemptWithoutFinancialWrite()
    {
        h.Gateway.CreationError = new TimeoutException("synthetic timeout");

        var result = await h.Start();

        Assert.Multiple(() =>
        {
            Assert.That(result.PaymentId, Is.EqualTo(h.Repo.Detail.PaymentId));
            Assert.That(result.Status, Is.EqualTo("PENDING"));
            Assert.That(result.Outcome, Is.EqualTo(PaymentStartOutcome.SessionUnavailable));
            Assert.That(result.PaymentUrl, Is.Null);
            Assert.That(h.Repo.CreateCalls, Is.EqualTo(1));
            Assert.That(h.Gateway.Creates, Is.EqualTo(1));
            Assert.That(h.Repo.ApplyCalls, Is.Zero);
        });
    }

    [TestCase("PAID")]
    [TestCase("CANCELLED")]
    public void IneligibleInvoice_DoesNotCheckConfigurationOrCreate(string status)
    {
        h.Repo.Invoice = h.Repo.Invoice with { Status = status };

        var error = Assert.ThrowsAsync<BusinessException>(() => h.Start());

        Assert.That(error!.Code, Is.EqualTo("INVOICE_NOT_PAYABLE"));
        Assert.That(h.Gateway.ConfigurationChecks, Is.Zero);
        Assert.That(h.Repo.CreateCalls, Is.Zero);
    }

    [Test]
    public void AnotherCustomer_IsDeniedBeforeIdempotencyLookup()
    {
        h.Auth.Actor = h.Auth.Actor with { CustomerId = Guid.NewGuid() };

        var error = Assert.ThrowsAsync<BusinessException>(() => h.Start());

        Assert.That(error!.Code, Is.EqualTo("FORBIDDEN"));
        Assert.That(h.Events, Is.EqualTo(new[] { "invoice" }));
    }

    [Test]
    public async Task Callback_SuccessIsNormalizedAndWrittenOnceInUtc()
    {
        var providerTime = new DateTimeOffset(2026, 10, 7, 17, 2, 3, TimeSpan.FromHours(7));
        h.Gateway.Verified = new(h.Repo.Detail.PaymentId, 12500m, "123456789",
            PaymentGatewayCallbackOutcome.Success, providerTime);

        var result = await h.Service.ProcessCallbackAsync(new("?signed=payload"));

        Assert.Multiple(() =>
        {
            Assert.That(result.Outcome, Is.EqualTo(PaymentApplicationOutcome.Applied));
            Assert.That(h.Repo.ApplyCalls, Is.EqualTo(1));
            Assert.That(h.Repo.Applied, Is.EqualTo(new NormalizedPaymentResult(
                h.Repo.Detail.PaymentId, PaymentFinalStatus.Success, "123456789", 12500m,
                providerTime.ToUniversalTime(), PaymentResultSource.VerifiedCallback)));
        });
    }

    [TestCase(PaymentGatewayCallbackOutcome.Unverified, PaymentApplicationOutcome.VerificationRejected)]
    [TestCase(PaymentGatewayCallbackOutcome.Unknown, PaymentApplicationOutcome.Unresolved)]
    [TestCase(PaymentGatewayCallbackOutcome.Pending, PaymentApplicationOutcome.Unresolved)]
    public async Task Callback_NonFinalResultCannotWrite(
        PaymentGatewayCallbackOutcome providerOutcome, PaymentApplicationOutcome expected)
    {
        h.Gateway.Verified = h.Gateway.Verified with { Status = providerOutcome };

        var result = await h.Service.ProcessCallbackAsync(new("?untrusted=payload"));

        Assert.That(result.Outcome, Is.EqualTo(expected));
        Assert.That(h.Repo.ApplyCalls, Is.Zero);
    }

    [TestCase(PaymentApplyOutcome.Applied, PaymentApplicationOutcome.Applied)]
    [TestCase(PaymentApplyOutcome.Duplicate, PaymentApplicationOutcome.Duplicate)]
    [TestCase(PaymentApplyOutcome.NotFound, PaymentApplicationOutcome.NotFound)]
    [TestCase(PaymentApplyOutcome.AmountMismatch, PaymentApplicationOutcome.AmountMismatch)]
    [TestCase(PaymentApplyOutcome.ReferenceConflict, PaymentApplicationOutcome.ReferenceConflict)]
    [TestCase(PaymentApplyOutcome.TerminalConflict, PaymentApplicationOutcome.TerminalConflict)]
    public async Task Callback_RepositoryOutcomeIsPreserved(
        PaymentApplyOutcome repositoryOutcome, PaymentApplicationOutcome expected)
    {
        h.Repo.ApplyOutcome = repositoryOutcome;

        var result = await h.Service.ProcessCallbackAsync(new("?signed=payload"));

        Assert.That(result.Outcome, Is.EqualTo(expected));
        Assert.That(h.Repo.ApplyCalls, Is.EqualTo(1));
    }

    [Test]
    public async Task DetailRead_UsesAuthoritativeRoleAndNeverExposesSessionData()
    {
        h.Existing("PENDING", SessionUrl);

        var result = await h.Service.GetByIdAsync(h.Repo.Detail.PaymentId);

        Assert.That(result.PaymentId, Is.EqualTo(h.Repo.Detail.PaymentId));
        Assert.That(typeof(PaymentResult).GetProperties().Select(p => p.Name), Is.EquivalentTo(new[]
            { "PaymentId", "InvoiceId", "Amount", "PaymentMethod", "TransactionCode", "Status", "PaidAt", "CreatedAt" }));
    }

    [Test]
    public void SensitiveProviderValuesAreRedactedFromRecordStrings()
    {
        var records = new object[]
        {
            new StartPaymentCommand(h.Key, ReturnUrl, "203.0.113.10"),
            new PaymentGatewayPreflight(1m, ReturnUrl, "203.0.113.10"),
            new PaymentGatewayRequest(h.Repo.Detail.PaymentId, 1m, ReturnUrl, "203.0.113.10", Now),
            new PaymentGatewaySession(SessionUrl, null, null),
            new PaymentGatewayCallbackRequest("?private=payload")
        };

        foreach (var record in records)
            Assert.That(record.ToString(), Does.Not.Contain(ReturnUrl).And.Not.Contain(SessionUrl).And.Not.Contain("private=payload"));
    }

    private sealed class Harness
    {
        public readonly Guid Key = Guid.NewGuid();
        public readonly List<string> Events = [];
        public readonly Context Current = new();
        public readonly Auth Auth;
        public readonly Repo Repo;
        public readonly Gateway Gateway;
        public readonly PaymentService Service;

        public Harness()
        {
            var customer = Guid.NewGuid();
            var facility = Guid.NewGuid();
            var invoice = Guid.NewGuid();
            Auth = new(new(Current.UserAccountId, "CUSTOMER", "ACTIVE", "", "", customer,
                Guid.NewGuid(), facility, "Test"));
            Repo = new(new PaymentInvoiceRecord
            {
                InvoiceId = invoice,
                InvoiceType = "DEPOSIT",
                Status = "UNPAID",
                AmountDue = 12500m,
                CustomerId = customer,
                CustomerUserAccountId = Current.UserAccountId,
                FacilityId = facility,
                BillingMonth = new DateOnly(2026, 11, 1),
                ContractStartMonth = new DateOnly(2026, 10, 1)
            }, Key, Events);
            Gateway = new(Repo.Detail.PaymentId, Events);
            Service = new(Repo, Gateway, Current, Auth, new Clock());
        }

        public Task<InvoicePaymentStartResult> Start(CancellationToken token = default) =>
            Service.StartInvoicePaymentAsync(Repo.Invoice.InvoiceId,
                new StartPaymentCommand(Key, ReturnUrl, "203.0.113.10"), token);

        public void Existing(string status, string? url) =>
            Repo.Attempt = new PaymentAttemptRecord(Repo.Detail with { Status = status }, Key, url, null);
    }

    private sealed class Context : ICurrentUserContext
    {
        public bool IsAuthenticated => true;
        public Guid UserAccountId { get; } = Guid.NewGuid();
        public string Role => throw new AssertionException("JWT role must not authorize current access.");
    }

    private sealed class Clock : IClock
    {
        public DateTimeOffset UtcNow => Now;
        public TimeZoneInfo BusinessTimeZone => TimeZoneInfo.Utc;
        public DateTimeOffset ToBusinessTime(DateTimeOffset value) => value;
    }

    private sealed class Auth(CurrentAccountResult actor) : IAuthenticationService
    {
        public CurrentAccountResult Actor = actor;
        public Task<CurrentAccountResult> GetCurrentAccountAsync(Guid id, CancellationToken token) => Task.FromResult(Actor);
        public Task<AuthenticationResult> LoginCustomerAsync(CustomerLoginCommand command, CancellationToken token) => throw new NotSupportedException();
        public Task<AuthenticationResult> LoginEmployeeAsync(EmployeeLoginCommand command, CancellationToken token) => throw new NotSupportedException();
        public Task<bool> IsActiveAsync(Guid id, string role, CancellationToken token) => throw new NotSupportedException();
    }

    private sealed class Gateway(Guid paymentId, List<string> events) : IPaymentGateway
    {
        public int ConfigurationChecks;
        public int Creates;
        public Exception? ConfigurationError;
        public Exception? ValidationError;
        public Exception? CreationError;
        public Func<CancellationToken, Task>? Configure;
        public PaymentGatewayRequest? Request;
        public PaymentGatewayCallbackResult Verified = new(paymentId, 12500m, "123456789",
            PaymentGatewayCallbackOutcome.Success, Now);

        public Task EnsureConfiguredAsync(CancellationToken token)
        {
            ConfigurationChecks++;
            events.Add("configure");
            if (ConfigurationError is not null) return Task.FromException(ConfigurationError);
            return Configure?.Invoke(token) ?? Task.CompletedTask;
        }

        public void ValidatePaymentRequest(PaymentGatewayPreflight request)
        {
            events.Add("validate");
            if (ValidationError is not null) throw ValidationError;
        }

        public Task<PaymentGatewayCreationResult> CreatePaymentAsync(PaymentGatewayRequest request, CancellationToken token)
        {
            Creates++;
            Request = request;
            events.Add("gateway-create");
            if (CreationError is not null) throw CreationError;
            return Task.FromResult(new PaymentGatewayCreationResult(
                PaymentGatewayCreationOutcome.SessionCreated,
                new PaymentGatewaySession(SessionUrl, null, Now.AddMinutes(15)), null));
        }

        public Task<PaymentGatewayCallbackResult> VerifyAndNormalizeCallbackAsync(
            PaymentGatewayCallbackRequest request, CancellationToken token)
        {
            events.Add("verify");
            return Task.FromResult(Verified);
        }
    }

    private sealed class Repo(PaymentInvoiceRecord invoice, Guid expectedKey, List<string> events) : IPaymentRepository
    {
        public PaymentInvoiceRecord Invoice = invoice;
        public PaymentDetailRecord Detail = new(Guid.NewGuid(), invoice.InvoiceId, invoice.AmountDue, "VNPAY",
            null, "PENDING", null, Now, invoice.CustomerId, invoice.CustomerUserAccountId, invoice.FacilityId);
        public PaymentAttemptRecord? Attempt;
        public PaymentAttemptOutcome CreateOutcome = PaymentAttemptOutcome.Created;
        public PaymentApplyOutcome ApplyOutcome = PaymentApplyOutcome.Applied;
        public NormalizedPaymentResult? Applied;
        public int CreateCalls;
        public int ApplyCalls;

        public Task<PaymentInvoiceRecord?> GetInvoiceAsync(Guid invoiceId, CancellationToken token)
        {
            events.Add("invoice");
            return Task.FromResult<PaymentInvoiceRecord?>(Invoice);
        }

        public Task<PaymentDetailRecord?> GetByIdAsync(Guid paymentId, CancellationToken token) =>
            Task.FromResult<PaymentDetailRecord?>(Detail);

        public Task<PaymentAttemptResult> GetByIdempotencyKeyAsync(
            Guid invoiceId, Guid key, DateTimeOffset now, CancellationToken token)
        {
            events.Add("key");
            Assert.That(key, Is.EqualTo(expectedKey));
            return Task.FromResult(Attempt is null
                ? new PaymentAttemptResult(PaymentAttemptOutcome.NotFound, null)
                : new PaymentAttemptResult(PaymentAttemptOutcome.Existing, Attempt));
        }

        public Task<PaymentAttemptResult> CreateOrGetAsync(
            Guid invoiceId, Guid key, DateTimeOffset now, CancellationToken token)
        {
            CreateCalls++;
            events.Add("create");
            Attempt = new PaymentAttemptRecord(Detail, key, null, null);
            return Task.FromResult(new PaymentAttemptResult(CreateOutcome, Attempt));
        }

        public Task<PaymentAttemptResult> SaveSessionAsync(
            Guid paymentId, PaymentSessionRecord session, DateTimeOffset now, CancellationToken token)
        {
            events.Add("save");
            Attempt = Attempt! with { PaymentUrl = session.PaymentUrl, PaymentUrlExpiresAt = session.ExpiresAt };
            return Task.FromResult(new PaymentAttemptResult(PaymentAttemptOutcome.Existing, Attempt));
        }

        public Task<PaymentApplyResult> ApplyResultAsync(NormalizedPaymentResult result, CancellationToken token)
        {
            events.Add("apply");
            ApplyCalls++;
            Applied = result;
            return Task.FromResult(new PaymentApplyResult(ApplyOutcome, Detail, null));
        }
    }
}
