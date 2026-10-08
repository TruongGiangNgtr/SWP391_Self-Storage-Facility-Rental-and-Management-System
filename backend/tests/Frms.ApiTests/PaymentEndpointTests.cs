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
    private const string Session = "https://pay.payos.vn/web/fixture";
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
    public async Task PAY001_NewPayOsSessionReturns201_ThenSameKeyReturns200()
    {
        using var first = await Start();
        using var second = await Start();
        using var firstJson = JsonDocument.Parse(await first.Content.ReadAsStringAsync());
        using var secondJson = JsonDocument.Parse(await second.Content.ReadAsStringAsync());

        Assert.Multiple(() =>
        {
            Assert.That(first.StatusCode, Is.EqualTo(HttpStatusCode.Created));
            Assert.That(second.StatusCode, Is.EqualTo(HttpStatusCode.OK));
            Assert.That(firstJson.RootElement.GetProperty("data").GetProperty("paymentMethod").GetString(), Is.EqualTo("PAYOS"));
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

    [TestCase(PaymentApplicationOutcome.Applied, 200, "ACKNOWLEDGED")]
    [TestCase(PaymentApplicationOutcome.Duplicate, 200, "ACKNOWLEDGED")]
    [TestCase(PaymentApplicationOutcome.TerminalConflict, 200, "ACKNOWLEDGED")]
    [TestCase(PaymentApplicationOutcome.NotFound, 200, "IGNORED_UNKNOWN_ORDER")]
    [TestCase(PaymentApplicationOutcome.AmountMismatch, 409, "AMOUNT_MISMATCH")]
    [TestCase(PaymentApplicationOutcome.ReferenceConflict, 409, "REFERENCE_CONFLICT")]
    [TestCase(PaymentApplicationOutcome.VerificationRejected, 400, "PAYMENT_CALLBACK_INVALID")]
    [TestCase(PaymentApplicationOutcome.Unresolved, 400, "PAYMENT_CALLBACK_INVALID")]
    public async Task PAY004_AnonymousPostReturnsControlledAcknowledgement(
        PaymentApplicationOutcome outcome, int status, string expectedCode)
    {
        client.DefaultRequestHeaders.Remove("X-Test-Role");
        repository.Existing(key, "PENDING", null);
        repository.LookupUnknown = outcome == PaymentApplicationOutcome.NotFound;
        repository.ApplyOutcome = outcome;
        gateway.CallbackOutcome = outcome switch
        {
            PaymentApplicationOutcome.VerificationRejected => PaymentGatewayCallbackOutcome.Unverified,
            PaymentApplicationOutcome.Unresolved => PaymentGatewayCallbackOutcome.Unknown,
            _ => PaymentGatewayCallbackOutcome.Success
        };
        using var response = await client.PostAsJsonAsync("/api/v1/payments/payos/webhook", new { signature = "fixture", data = new { orderCode = 1000 } });
        var body = await response.Content.ReadAsStringAsync();
        Assert.Multiple(() =>
        {
            Assert.That((int)response.StatusCode, Is.EqualTo(status));
            Assert.That(body, Does.Contain(expectedCode));
            Assert.That(gateway.Verifies, Is.EqualTo(1));
            if (outcome is PaymentApplicationOutcome.NotFound or PaymentApplicationOutcome.VerificationRejected or PaymentApplicationOutcome.Unresolved)
                Assert.That(repository.Applies, Is.Zero);
            else Assert.That(repository.Applies, Is.EqualTo(1));
            if (status >= 400) Assert.That(body, Does.Contain("traceId"));
        });
    }

    [Test]
    public async Task PAY004_PersistenceFailureIsRetryableWithoutLeakingException()
    {
        client.DefaultRequestHeaders.Remove("X-Test-Role");
        repository.Existing(key, "PENDING", null);
        repository.FailApply = true;
        using var response = await client.PostAsJsonAsync("/api/v1/payments/payos/webhook", new { data = new { orderCode = 1000 } });
        var body = await response.Content.ReadAsStringAsync();
        Assert.That(response.StatusCode, Is.EqualTo(HttpStatusCode.ServiceUnavailable));
        Assert.That(body, Does.Contain("PAYMENT_CALLBACK_UNAVAILABLE").And.Contain("traceId").And.Not.Contain("private-diagnostic"));
    }

    [Test]
    public async Task PAY004_OversizedJsonIsRejectedBeforeServiceVerificationOrMutation()
    {
        client.DefaultRequestHeaders.Remove("X-Test-Role");
        using var response = await client.PostAsJsonAsync("/api/v1/payments/payos/webhook", new { data = new { extra = new string('x', 65_536) } });
        var body = await response.Content.ReadAsStringAsync();
        Assert.That((int)response.StatusCode, Is.EqualTo(413));
        Assert.That(body, Does.Contain("PAYMENT_CALLBACK_TOO_LARGE").And.Contain("traceId"));
        Assert.That(gateway.Verifies, Is.Zero);
        Assert.That(repository.Applies, Is.Zero);
    }

    [TestCase("FACILITY_STAFF")]
    [TestCase("FACILITY_MANAGER")]
    [TestCase("BUSINESS_OPERATIONS_MANAGER")]
    [TestCase("SYSTEM_ADMINISTRATOR")]
    public async Task PAY001_EmployeesCannotInitiate(string role)
    {
        SetRole(role);
        using var response = await Start();
        Assert.That(response.StatusCode, Is.EqualTo(HttpStatusCode.Forbidden));
        Assert.That(repository.Creates, Is.Zero);
        Assert.That(gateway.Creates, Is.Zero);
    }

    [TestCase("/api/v1/payments/vnpay/ipn", "GET")]
    [TestCase("/api/v1/payments/momo/callback", "POST")]
    [TestCase("/api/v1/invoices/11111111-1111-1111-1111-111111111111/payments/vnpay", "POST")]
    [TestCase("/api/v1/invoices/11111111-1111-1111-1111-111111111111/payments/momo", "POST")]
    [TestCase("/api/v1/payments/payos/return", "POST")]
    [TestCase("/api/v1/payments/payos/cancel", "POST")]
    public async Task RemovedAndBrowserMutationRoutesAreInactive(string route, string method)
    {
        using var response = await client.SendAsync(new HttpRequestMessage(new HttpMethod(method), route));
        Assert.That(response.StatusCode, Is.EqualTo(HttpStatusCode.NotFound));
        Assert.That(repository.Applies, Is.Zero);
    }

    [Test]
    public async Task OpenApi_ExposesPayOsPostAndAnonymousPostWebhookOnly()
    {
        using var response = await client.GetAsync("/openapi/v1.json");
        using var document = JsonDocument.Parse(await response.Content.ReadAsStringAsync());
        var paths = document.RootElement.GetProperty("paths");
        var start = paths.GetProperty("/api/v1/invoices/{invoiceId}/payments/payos").GetProperty("post");
        var ipn = paths.GetProperty("/api/v1/payments/payos/webhook").GetProperty("post");

        Assert.Multiple(() =>
        {
            Assert.That(start.GetProperty("responses").TryGetProperty("201", out _), Is.True);
            Assert.That(start.GetProperty("parameters").EnumerateArray().Any(parameter =>
                parameter.GetProperty("name").GetString() == "Idempotency-Key"
                && parameter.GetProperty("required").GetBoolean()), Is.True);
            Assert.That(ipn.TryGetProperty("requestBody", out _), Is.True);
            Assert.That(paths.TryGetProperty("/api/v1/payments/vnpay/ipn", out _), Is.False);
            Assert.That(!ipn.TryGetProperty("security", out var security) || security.GetArrayLength() == 0, Is.True);
            Assert.That(paths.TryGetProperty("/api/v1/payments/momo/callback", out _), Is.False);
        });
    }

    private Task<HttpResponseMessage> Start(string? header = "valid")
    {
        var request = new HttpRequestMessage(HttpMethod.Post, $"/api/v1/invoices/{invoice}/payments/payos")
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
        public int Verifies;
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
            PaymentGatewayCallbackRequest request, CancellationToken token)
        {
            Verifies++;
            return Task.FromResult(new PaymentGatewayCallbackResult(Guid.Empty, 12500m, "TF900001", CallbackOutcome, Now, 1000));
        }
    }

    private sealed class Repository(Guid invoice) : IPaymentRepository
    {
        public PaymentAttemptRecord? Attempt;
        public int Creates;
        public bool FailApply;
        public bool LookupUnknown;
        public int Applies;
        public PaymentApplicationOutcome ApplyOutcome = PaymentApplicationOutcome.Applied;

        public void Existing(Guid idempotencyKey, string status, string? url) => Attempt = new(
            new PaymentDetailRecord(Guid.NewGuid(), invoice, 12500m, "PAYOS", null, status, null, Now,
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

        public Task<PaymentDetailRecord?> GetByProviderOrderCodeAsync(long code, CancellationToken token) =>
            Task.FromResult<PaymentDetailRecord?>(LookupUnknown ? null : Attempt?.Detail);

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
            Applies++;
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
