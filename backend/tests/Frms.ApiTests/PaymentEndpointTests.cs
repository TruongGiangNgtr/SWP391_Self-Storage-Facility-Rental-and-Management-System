using System.Net;
using System.Net.Http.Json;
using System.Security.Claims;
using System.Text.Encodings.Web;
using System.Text.Json;
using Frms.Business.Abstractions.External;
using Frms.Business.Abstractions.Time;
using Frms.Business.Models.Commands;
using Frms.Business.Models.Results;
using Frms.Business.Services.Interfaces;
using Frms.DataAccess.Repositories.Interfaces;
using Frms.DataAccess.Repositories.Models;
using Microsoft.AspNetCore.Authentication;
using Microsoft.AspNetCore.TestHost;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.DependencyInjection.Extensions;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using IAuthenticationService = Frms.Business.Services.Interfaces.IAuthenticationService;

namespace Frms.ApiTests;

[TestFixture, Category("PaymentApi")]
public sealed class PaymentEndpointTests
{
    private static readonly DateTimeOffset Now = new(2026, 10, 7, 10, 0, 0, TimeSpan.Zero);
    private static readonly Guid Account = Guid.Parse("aaaaaaaa-aaaa-aaaa-aaaa-aaaaaaaaaaaa");
    private static readonly Guid Customer = Guid.Parse("bbbbbbbb-bbbb-bbbb-bbbb-bbbbbbbbbbbb");
    private static readonly Guid Facility = Guid.Parse("cccccccc-cccc-cccc-cccc-cccccccccccc");
    private const string Session = "https://sandbox.vnpayment.vn/paymentv2/vpcpay.html?private=fixture";
    private Repository repository = null!;
    private Gateway gateway = null!;
    private Auth auth = null!;
    private FrmsWebApplicationFactory root = null!;
    private Microsoft.AspNetCore.Mvc.Testing.WebApplicationFactory<Program> factory = null!;
    private HttpClient client = null!;
    private Guid invoice;
    private Guid key;

    [SetUp]
    public void SetUp()
    {
        invoice = Guid.NewGuid();
        key = Guid.NewGuid();
        repository = new(invoice);
        gateway = new();
        auth = new();
        root = new();
        factory = root.WithWebHostBuilder(builder => builder.ConfigureTestServices(services =>
        {
            services.RemoveAll<IPaymentRepository>();
            services.AddSingleton<IPaymentRepository>(repository);
            services.RemoveAll<IPaymentGateway>();
            services.AddSingleton<IPaymentGateway>(gateway);
            services.RemoveAll<IAuthenticationService>();
            services.AddSingleton<IAuthenticationService>(auth);
            services.RemoveAll<IClock>();
            services.AddSingleton<IClock, Clock>();
            services.AddAuthentication(options =>
                {
                    options.DefaultAuthenticateScheme = "PaymentTest";
                    options.DefaultChallengeScheme = "PaymentTest";
                    options.DefaultForbidScheme = "PaymentTest";
                })
                .AddScheme<AuthenticationSchemeOptions, TestAuthentication>("PaymentTest", _ => { });
        }));
        client = factory.CreateClient();
        client.DefaultRequestHeaders.Add("X-Test-Role", "CUSTOMER");
    }

    [TearDown]
    public void TearDown()
    {
        client.Dispose();
        factory.Dispose();
        root.Dispose();
    }

    [Test]
    public async Task PAY001_NewVnPaySessionReturns201_ThenSameKeyReturns200()
    {
        using var first = await Start();
        using var second = await Start();
        using var firstJson = JsonDocument.Parse(await first.Content.ReadAsStringAsync());
        using var secondJson = JsonDocument.Parse(await second.Content.ReadAsStringAsync());

        Assert.Multiple(() =>
        {
            Assert.That(first.StatusCode, Is.EqualTo(HttpStatusCode.Created));
            Assert.That(second.StatusCode, Is.EqualTo(HttpStatusCode.OK));
            Assert.That(firstJson.RootElement.GetProperty("data").GetProperty("paymentMethod").GetString(), Is.EqualTo("VNPAY"));
            Assert.That(firstJson.RootElement.GetProperty("data").GetProperty("amount").GetDecimal(), Is.EqualTo(12500m));
            Assert.That(secondJson.RootElement.GetProperty("data").GetProperty("paymentId").GetGuid(),
                Is.EqualTo(firstJson.RootElement.GetProperty("data").GetProperty("paymentId").GetGuid()));
            Assert.That(gateway.Creates, Is.EqualTo(1));
            Assert.That(repository.Creates, Is.EqualTo(1));
        });
    }

    [TestCase(null)]
    [TestCase("not-a-uuid")]
    [TestCase("00000000-0000-0000-0000-000000000000")]
    public async Task PAY001_InvalidIdempotencyKeyIsRejectedBeforeCreation(string? header)
    {
        using var response = await Start(header);

        Assert.That(response.StatusCode, Is.EqualTo(HttpStatusCode.BadRequest));
        Assert.That(repository.Creates, Is.Zero);
        Assert.That(gateway.Creates, Is.Zero);
    }

    [TestCase("CUSTOMER", true)]
    [TestCase("FACILITY_STAFF", true)]
    [TestCase("FACILITY_MANAGER", true)]
    [TestCase("BUSINESS_OPERATIONS_MANAGER", true)]
    [TestCase("SYSTEM_ADMINISTRATOR", false)]
    public async Task PAY003_RoleMatrixAndPrivateFieldExclusion(string role, bool allowed)
    {
        SetRole(role);
        repository.Existing(key, "PENDING", Session);

        using var response = await client.GetAsync($"/api/v1/payments/{repository.Attempt!.Detail.PaymentId}");
        var body = await response.Content.ReadAsStringAsync();

        Assert.That(response.StatusCode, Is.EqualTo(allowed ? HttpStatusCode.OK : HttpStatusCode.Forbidden));
        Assert.That(body, Does.Not.Contain("paymentUrl").And.Not.Contain("idempotencyKey").And.Not.Contain(Session));
    }

    [TestCase(PaymentApplicationOutcome.Applied, "00")]
    [TestCase(PaymentApplicationOutcome.Duplicate, "02")]
    [TestCase(PaymentApplicationOutcome.TerminalConflict, "02")]
    [TestCase(PaymentApplicationOutcome.NotFound, "01")]
    [TestCase(PaymentApplicationOutcome.AmountMismatch, "04")]
    [TestCase(PaymentApplicationOutcome.VerificationRejected, "97")]
    [TestCase(PaymentApplicationOutcome.Unresolved, "99")]
    public async Task PAY004_AnonymousGetReturnsProviderAcknowledgement(
        PaymentApplicationOutcome outcome, string expectedCode)
    {
        client.DefaultRequestHeaders.Remove("X-Test-Role");
        repository.ApplyOutcome = outcome;
        gateway.CallbackOutcome = outcome switch
        {
            PaymentApplicationOutcome.VerificationRejected => PaymentGatewayCallbackOutcome.Unverified,
            PaymentApplicationOutcome.Unresolved => PaymentGatewayCallbackOutcome.Unknown,
            _ => PaymentGatewayCallbackOutcome.Success
        };

        using var response = await client.GetAsync("/api/v1/payments/vnpay/ipn?vnp_SecureHash=fixture");
        using var json = JsonDocument.Parse(await response.Content.ReadAsStringAsync());

        Assert.That(response.StatusCode, Is.EqualTo(HttpStatusCode.OK));
        Assert.That(json.RootElement.GetProperty("RspCode").GetString(), Is.EqualTo(expectedCode));
        Assert.That(json.RootElement.TryGetProperty("Message", out _), Is.True);
    }

    [Test]
    public async Task PAY004_PersistenceFailureReturnsRetryable99WithoutLeakingException()
    {
        client.DefaultRequestHeaders.Remove("X-Test-Role");
        repository.FailApply = true;

        using var response = await client.GetAsync("/api/v1/payments/vnpay/ipn?vnp_SecureHash=fixture");
        var body = await response.Content.ReadAsStringAsync();

        Assert.That(response.StatusCode, Is.EqualTo(HttpStatusCode.OK));
        Assert.That(body, Does.Contain("\"RspCode\":\"99\"").And.Not.Contain("private-diagnostic"));
    }

    [Test]
    public async Task OpenApi_ExposesVnPayPostAndAnonymousGetIpnOnly()
    {
        using var response = await client.GetAsync("/openapi/v1.json");
        using var document = JsonDocument.Parse(await response.Content.ReadAsStringAsync());
        var paths = document.RootElement.GetProperty("paths");
        var start = paths.GetProperty("/api/v1/invoices/{invoiceId}/payments/vnpay").GetProperty("post");
        var ipn = paths.GetProperty("/api/v1/payments/vnpay/ipn").GetProperty("get");

        Assert.Multiple(() =>
        {
            Assert.That(start.GetProperty("responses").TryGetProperty("201", out _), Is.True);
            Assert.That(start.GetProperty("parameters").EnumerateArray().Any(parameter =>
                parameter.GetProperty("name").GetString() == "Idempotency-Key"
                && parameter.GetProperty("required").GetBoolean()), Is.True);
            Assert.That(ipn.TryGetProperty("requestBody", out _), Is.False);
            Assert.That(!ipn.TryGetProperty("security", out var security) || security.GetArrayLength() == 0, Is.True);
            Assert.That(paths.TryGetProperty("/api/v1/payments/momo/callback", out _), Is.False);
        });
    }

    private Task<HttpResponseMessage> Start(string? header = "valid")
    {
        var request = new HttpRequestMessage(HttpMethod.Post, $"/api/v1/invoices/{invoice}/payments/vnpay")
        {
            Content = JsonContent.Create(new { returnUrl = "https://app.example.invalid/payment/vnpay-return", amount = 1 })
        };
        if (header is not null)
            request.Headers.TryAddWithoutValidation("Idempotency-Key", header == "valid" ? key.ToString() : header);
        return client.SendAsync(request);
    }

    private void SetRole(string role)
    {
        client.DefaultRequestHeaders.Remove("X-Test-Role");
        client.DefaultRequestHeaders.Add("X-Test-Role", role);
        auth.Actor = auth.Actor with { Role = role };
    }

    public sealed class TestAuthentication(
        IOptionsMonitor<AuthenticationSchemeOptions> options,
        ILoggerFactory logger,
        UrlEncoder encoder) : AuthenticationHandler<AuthenticationSchemeOptions>(options, logger, encoder)
    {
        protected override Task<AuthenticateResult> HandleAuthenticateAsync()
        {
            var role = Request.Headers["X-Test-Role"].ToString();
            if (string.IsNullOrEmpty(role)) return Task.FromResult(AuthenticateResult.NoResult());
            var principal = new ClaimsPrincipal(new ClaimsIdentity(
                [new Claim("sub", Account.ToString()), new Claim(ClaimTypes.Role, role)], Scheme.Name));
            return Task.FromResult(AuthenticateResult.Success(new AuthenticationTicket(principal, Scheme.Name)));
        }
    }

    private sealed class Clock : IClock
    {
        public DateTimeOffset UtcNow => Now;
        public TimeZoneInfo BusinessTimeZone => TimeZoneInfo.Utc;
        public DateTimeOffset ToBusinessTime(DateTimeOffset time) => time;
    }

    private sealed class Auth : IAuthenticationService
    {
        public CurrentAccountResult Actor = new(Account, "CUSTOMER", "ACTIVE", "", "", Customer,
            Guid.NewGuid(), Facility, "Test");
        public Task<CurrentAccountResult> GetCurrentAccountAsync(Guid id, CancellationToken token) => Task.FromResult(Actor);
        public Task<bool> IsActiveAsync(Guid id, string role, CancellationToken token) => Task.FromResult(true);
        public Task<AuthenticationResult> LoginCustomerAsync(CustomerLoginCommand command, CancellationToken token) => throw new NotSupportedException();
        public Task<AuthenticationResult> LoginEmployeeAsync(EmployeeLoginCommand command, CancellationToken token) => throw new NotSupportedException();
    }

    private sealed class Gateway : IPaymentGateway
    {
        public int Creates;
        public PaymentGatewayCallbackOutcome CallbackOutcome = PaymentGatewayCallbackOutcome.Success;
        public Task EnsureConfiguredAsync(CancellationToken token) => Task.CompletedTask;
        public void ValidatePaymentRequest(PaymentGatewayPreflight request) { }
        public Task<PaymentGatewayCreationResult> CreatePaymentAsync(PaymentGatewayRequest request, CancellationToken token)
        {
            Creates++;
            return Task.FromResult(new PaymentGatewayCreationResult(PaymentGatewayCreationOutcome.SessionCreated,
                new PaymentGatewaySession(Session, null, Now.AddMinutes(15)), null));
        }
        public Task<PaymentGatewayCallbackResult> VerifyAndNormalizeCallbackAsync(
            PaymentGatewayCallbackRequest request, CancellationToken token) => Task.FromResult(
                new PaymentGatewayCallbackResult(Guid.NewGuid(), 12500m, "123456789",
                    CallbackOutcome, Now));
    }

    private sealed class Repository(Guid invoice) : IPaymentRepository
    {
        public PaymentAttemptRecord? Attempt;
        public int Creates;
        public bool FailApply;
        public PaymentApplicationOutcome ApplyOutcome = PaymentApplicationOutcome.Applied;

        public void Existing(Guid idempotencyKey, string status, string? url) => Attempt = new(
            new PaymentDetailRecord(Guid.NewGuid(), invoice, 12500m, "VNPAY", null, status, null, Now,
                Customer, Account, Facility), idempotencyKey, url, null);

        public Task<PaymentInvoiceRecord?> GetInvoiceAsync(Guid id, CancellationToken token) =>
            Task.FromResult<PaymentInvoiceRecord?>(new PaymentInvoiceRecord
            {
                InvoiceId = id,
                InvoiceType = "DEPOSIT",
                Status = "UNPAID",
                AmountDue = 12500m,
                CustomerId = Customer,
                CustomerUserAccountId = Account,
                FacilityId = Facility
            });

        public Task<PaymentDetailRecord?> GetByIdAsync(Guid id, CancellationToken token) =>
            Task.FromResult<PaymentDetailRecord?>(Attempt?.Detail);

        public Task<PaymentAttemptResult> GetByIdempotencyKeyAsync(
            Guid id, Guid idempotencyKey, DateTimeOffset now, CancellationToken token) => Task.FromResult(
                Attempt is null
                    ? new PaymentAttemptResult(PaymentAttemptOutcome.NotFound, null)
                    : Attempt.Detail.InvoiceId != id
                        ? new PaymentAttemptResult(PaymentAttemptOutcome.IdempotencyConflict, null)
                        : new PaymentAttemptResult(PaymentAttemptOutcome.Existing, Attempt));

        public Task<PaymentAttemptResult> CreateOrGetAsync(
            Guid id, Guid idempotencyKey, DateTimeOffset now, CancellationToken token)
        {
            Creates++;
            Existing(idempotencyKey, "PENDING", null);
            return Task.FromResult(new PaymentAttemptResult(PaymentAttemptOutcome.Created, Attempt));
        }

        public Task<PaymentAttemptResult> SaveSessionAsync(
            Guid id, PaymentSessionRecord session, DateTimeOffset now, CancellationToken token)
        {
            Attempt = Attempt! with { PaymentUrl = session.PaymentUrl, PaymentUrlExpiresAt = session.ExpiresAt };
            return Task.FromResult(new PaymentAttemptResult(PaymentAttemptOutcome.Existing, Attempt));
        }

        public Task<PaymentApplyResult> ApplyResultAsync(NormalizedPaymentResult result, CancellationToken token)
        {
            if (FailApply) throw new IOException("private-diagnostic");
            var repositoryOutcome = ApplyOutcome switch
            {
                PaymentApplicationOutcome.Applied => PaymentApplyOutcome.Applied,
                PaymentApplicationOutcome.Duplicate => PaymentApplyOutcome.Duplicate,
                PaymentApplicationOutcome.NotFound => PaymentApplyOutcome.NotFound,
                PaymentApplicationOutcome.AmountMismatch => PaymentApplyOutcome.AmountMismatch,
                PaymentApplicationOutcome.TerminalConflict => PaymentApplyOutcome.TerminalConflict,
                PaymentApplicationOutcome.ReferenceConflict => PaymentApplyOutcome.ReferenceConflict,
                _ => PaymentApplyOutcome.InvalidResult
            };
            return Task.FromResult(new PaymentApplyResult(repositoryOutcome, null, null));
        }
    }
}
