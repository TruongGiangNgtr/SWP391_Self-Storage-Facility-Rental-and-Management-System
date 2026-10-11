using System.Net;
using System.Net.Http.Json;
using System.Security.Claims;
using System.Text.Encodings.Web;
using System.Text.Json;
using Frms.Business.Exceptions;
using Frms.Business.Models.Commands;
using Frms.Business.Models.Results;
using Frms.Business.Services.Interfaces;
using Frms.DataAccess.Repositories.Interfaces;
using Frms.DataAccess.Repositories.Models;
using Frms.DataAccess.StoredProcedures;
using Microsoft.AspNetCore.Authentication;
using Microsoft.AspNetCore.TestHost;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.DependencyInjection.Extensions;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using BusinessAuthentication = Frms.Business.Services.Interfaces.IAuthenticationService;

namespace Frms.ApiTests;

[TestFixture, Category("ReservationCancellation"), NonParallelizable]
public sealed class ReservationCancellationTests
{
    private static readonly Guid AccountId = Guid.Parse("aaaaaaaa-aaaa-aaaa-aaaa-aaaaaaaaaaaa");
    private static readonly Guid CustomerId = Guid.Parse("bbbbbbbb-bbbb-bbbb-bbbb-bbbbbbbbbbbb");
    private static readonly Guid ReservationId = Guid.Parse("cccccccc-cccc-cccc-cccc-cccccccccccc");
    private Repository repository = null!;
    private Auth auth = null!;
    private FrmsWebApplicationFactory root = null!;
    private Microsoft.AspNetCore.Mvc.Testing.WebApplicationFactory<Program> factory = null!;
    private HttpClient client = null!;

    [SetUp]
    public void SetUp()
    {
        repository = new();
        auth = new();
        root = new();
        factory = root.WithWebHostBuilder(builder => builder.ConfigureTestServices(services =>
        {
            services.RemoveAll<IReservationRepository>();
            services.AddSingleton<IReservationRepository>(repository);
            services.RemoveAll<BusinessAuthentication>();
            services.AddSingleton<BusinessAuthentication>(auth);
            services.AddAuthentication(options =>
            {
                options.DefaultAuthenticateScheme = "ReservationTest";
                options.DefaultChallengeScheme = "ReservationTest";
                options.DefaultForbidScheme = "ReservationTest";
            }).AddScheme<AuthenticationSchemeOptions, TestAuthentication>("ReservationTest", _ => { });
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

    [TestCase("PAID", true)]
    [TestCase("UNPAID", false)]
    public async Task API_RES005_001_ReturnsCanonicalAuthoritativeDetailWithoutRepricing(string depositStatus, bool hasVisit)
    {
        repository.Detail = Detail(depositStatus, hasVisit);
        using var response = await Cancel();
        using var document = JsonDocument.Parse(await response.Content.ReadAsStringAsync());
        var data = document.RootElement.GetProperty("data");
        Assert.Multiple(() =>
        {
            Assert.That(response.StatusCode, Is.EqualTo(HttpStatusCode.OK));
            Assert.That(data.GetProperty("reservationId").GetGuid(), Is.EqualTo(ReservationId));
            Assert.That(data.GetProperty("status").GetString(), Is.EqualTo("CANCELLED"));
            Assert.That(data.GetProperty("startMonth").GetString(), Is.EqualTo("2026-11"));
            Assert.That(data.GetProperty("endMonth").GetString(), Is.EqualTo("2027-01"));
            Assert.That(data.GetProperty("lockedRentalPrice").GetDecimal(), Is.EqualTo(123456.78m));
            Assert.That(data.GetProperty("depositAmount").GetDecimal(), Is.EqualTo(123456.78m));
            Assert.That(data.GetProperty("depositInvoice").GetProperty("status").GetString(), Is.EqualTo(depositStatus));
            Assert.That(data.GetProperty("depositInvoice").GetProperty("amountDue").GetDecimal(), Is.EqualTo(123456.78m));
            Assert.That(data.GetProperty("createdAt").GetDateTimeOffset(), Is.EqualTo(new DateTimeOffset(2026, 10, 10, 3, 0, 0, TimeSpan.Zero)));
            Assert.That(data.GetProperty("depositInvoice").GetProperty("dueDate").GetDateTimeOffset().Offset, Is.EqualTo(TimeSpan.Zero));
            Assert.That(repository.LastActor, Is.EqualTo(AccountId));
            Assert.That(repository.LastCustomer, Is.EqualTo(CustomerId));
            Assert.That(repository.LastReservation, Is.EqualTo(ReservationId));
            Assert.That(data.TryGetProperty("reason", out _), Is.False);
            Assert.That(data.TryGetProperty("customerId", out _), Is.False);
        });
        if (hasVisit)
        {
            Assert.That(data.GetProperty("reservationVisit").GetProperty("status").GetString(), Is.EqualTo("CANCELLED"));
            Assert.That(data.GetProperty("reservationVisit").GetProperty("visitDate").GetString(), Is.EqualTo("2026-11-03"));
        }
        else Assert.That(data.GetProperty("reservationVisit").ValueKind, Is.EqualTo(JsonValueKind.Null));
    }

    [TestCase("")]
    [TestCase("   ")]
    [TestCase(null)]
    public async Task API_RES005_002_EmptyReasonRejectedBeforeWrite(string? reason)
    {
        using var response = await Cancel(new { reason });
        await AssertError(response, HttpStatusCode.BadRequest, "VALIDATION_ERROR");
        Assert.That(repository.CancelCalls, Is.Zero);
    }

    [Test]
    public async Task API_RES005_003_AnonymousRejectedBeforeWrite()
    {
        client.DefaultRequestHeaders.Remove("X-Test-Role");
        using var response = await Cancel();
        Assert.That(response.StatusCode, Is.EqualTo(HttpStatusCode.Unauthorized));
        Assert.That(repository.CancelCalls, Is.Zero);
    }

    [TestCase("FACILITY_STAFF")]
    [TestCase("FACILITY_MANAGER")]
    [TestCase("BUSINESS_OPERATIONS_MANAGER")]
    [TestCase("SYSTEM_ADMINISTRATOR")]
    public async Task SEC_RES005_004_EmployeeCommandRejected(string role)
    {
        client.DefaultRequestHeaders.Remove("X-Test-Role");
        client.DefaultRequestHeaders.Add("X-Test-Role", role);
        using var response = await Cancel();
        Assert.That(response.StatusCode, Is.EqualTo(HttpStatusCode.Forbidden));
        Assert.That(repository.CancelCalls, Is.Zero);
    }

    [TestCase("INACTIVE", "CUSTOMER", "ACCOUNT_INACTIVE")]
    [TestCase("ACTIVE", "SYSTEM_ADMINISTRATOR", "FORBIDDEN")]
    public async Task SEC_RES005_005_ServiceRechecksAccountInsteadOfTrustingCustomerClaim(string status, string role, string code)
    {
        auth.Status = status;
        auth.Role = role;
        using var response = await Cancel();
        await AssertError(response, HttpStatusCode.Forbidden, code);
        Assert.That(repository.CancelCalls, Is.Zero);
    }

    [TestCase("RESOURCE_NOT_FOUND", 404)]
    [TestCase("RESERVATION_INVALID_STATUS", 409)]
    [TestCase("VISIT_INVALID_STATUS", 409)]
    [TestCase("ACCOUNT_INACTIVE", 403)]
    [TestCase("FORBIDDEN", 403)]
    public async Task API_RES005_006_AtomicRejectionMapsStableCodeAndTrace(string code, int status)
    {
        repository.ErrorCode = code;
        using var response = await Cancel();
        await AssertError(response, (HttpStatusCode)status, code);
        Assert.That(repository.CancelCalls, Is.EqualTo(1));
    }

    [Test]
    public async Task SEC_RES005_007_WrongOwnerIsNotDisclosed()
    {
        repository.Detail = repository.Detail with { CustomerId = Guid.NewGuid() };
        using var response = await Cancel();
        await AssertError(response, HttpStatusCode.NotFound, "RESOURCE_NOT_FOUND");
        Assert.That(repository.LastCustomer, Is.EqualTo(CustomerId));
    }

    [Test]
    public async Task API_RES005_008_MissingCustomerProfileDoesNotWrite()
    {
        repository.Customer = null;
        using var response = await Cancel();
        await AssertError(response, HttpStatusCode.NotFound, "RESOURCE_NOT_FOUND");
        Assert.That(repository.CancelCalls, Is.Zero);
    }

    [Test]
    public async Task CTL_RES005_009_OpenApiKeepsExistingReservationDetailContract()
    {
        using var response = await client.GetAsync("/openapi/v1.json");
        using var json = JsonDocument.Parse(await response.Content.ReadAsStringAsync());
        var action = json.RootElement.GetProperty("paths")
            .GetProperty("/api/v1/reservations/{reservationId}/cancel").GetProperty("post");
        var schema = action.GetProperty("responses").GetProperty("200")
            .GetProperty("content").GetProperty("application/json").GetProperty("schema");
        Assert.That(schema.GetProperty("$ref").GetString(), Does.Contain("ReservationDetailResponse"));
        Assert.That(action.GetProperty("requestBody").GetProperty("required").GetBoolean(), Is.True);
    }

    [Test]
    public async Task API_RES003_010_CustomerDetailRejectsCrossOwner()
    {
        repository.Detail = repository.Detail with { CustomerId = Guid.NewGuid() };
        using var response = await client.GetAsync($"/api/v1/reservations/{ReservationId}");
        await AssertError(response, HttpStatusCode.NotFound, "RESOURCE_NOT_FOUND");
        Assert.That(repository.CancelCalls, Is.Zero);
    }

    [Test]
    public async Task API_RES005_012_EmptyReservationIdIsValidationError()
    {
        using var response = await client.PostAsJsonAsync($"/api/v1/reservations/{Guid.Empty}/cancel",
            new { reason = "No longer needed." });
        await AssertError(response, HttpStatusCode.BadRequest, "VALIDATION_ERROR");
        Assert.That(repository.CancelCalls, Is.Zero);
    }

    [Test]
    public async Task UT_RES005_013_ServicePropagatesCancellationToken()
    {
        using var response = await Cancel();
        Assert.That(response.StatusCode, Is.EqualTo(HttpStatusCode.OK));
        Assert.That(repository.LastToken.CanBeCanceled, Is.True);
        Assert.That(repository.LastToken, Is.EqualTo(auth.LastToken));
    }

    [Test]
    public async Task API_RES002_014_CustomerListKeepsOwnScopeAndPagination()
    {
        using var response = await client.GetAsync("/api/v1/reservations?page=2&pageSize=5");
        using var json = JsonDocument.Parse(await response.Content.ReadAsStringAsync());
        Assert.Multiple(() =>
        {
            Assert.That(response.StatusCode, Is.EqualTo(HttpStatusCode.OK));
            Assert.That(repository.ListCustomer, Is.EqualTo(CustomerId));
            Assert.That(json.RootElement.GetProperty("pagination").GetProperty("page").GetInt32(), Is.EqualTo(2));
            Assert.That(json.RootElement.GetProperty("pagination").GetProperty("pageSize").GetInt32(), Is.EqualTo(5));
            Assert.That(json.RootElement.GetProperty("pagination").GetProperty("totalItems").GetInt32(), Is.EqualTo(6));
            Assert.That(json.RootElement.GetProperty("data")[0].GetProperty("reservationId").GetGuid(), Is.EqualTo(ReservationId));
            Assert.That(repository.CancelCalls, Is.Zero);
        });
    }

    [Test]
    public async Task API_RES004_011_ConfirmKeepsExistingSavedStatusAndVisit()
    {
        using var response = await client.PostAsJsonAsync($"/api/v1/reservations/{ReservationId}/confirm",
            new { reservationVisitDate = "2026-11-03" });
        using var json = JsonDocument.Parse(await response.Content.ReadAsStringAsync());
        Assert.Multiple(() =>
        {
            Assert.That(response.StatusCode, Is.EqualTo(HttpStatusCode.OK));
            Assert.That(json.RootElement.GetProperty("data").GetProperty("status").GetString(), Is.EqualTo("CONFIRMED"));
            Assert.That(json.RootElement.GetProperty("data").GetProperty("reservationVisit").GetProperty("status").GetString(), Is.EqualTo("SCHEDULED"));
            Assert.That(repository.CancelCalls, Is.Zero);
        });
    }

    private Task<HttpResponseMessage> Cancel(object? body = null) =>
        client.PostAsJsonAsync($"/api/v1/reservations/{ReservationId}/cancel", body ?? new { reason = "No longer needed." });

    private static async Task AssertError(HttpResponseMessage response, HttpStatusCode status, string code)
    {
        using var json = JsonDocument.Parse(await response.Content.ReadAsStringAsync());
        Assert.Multiple(() =>
        {
            Assert.That(response.StatusCode, Is.EqualTo(status));
            Assert.That(json.RootElement.GetProperty("code").GetString(), Is.EqualTo(code));
            Assert.That(json.RootElement.GetProperty("traceId").GetString(), Is.Not.Empty);
        });
    }

    private static ReservationDetailRecord Detail(string deposit = "PAID", bool hasVisit = true) =>
        new(ReservationId, CustomerId, Guid.Parse("dddddddd-dddd-dddd-dddd-dddddddddddd"),
            Guid.NewGuid(), Guid.NewGuid(), new(2026, 11, 1), new(2027, 1, 1),
            123456.78m, 123456.78m, "CANCELLED",
            new(Guid.NewGuid(), deposit, 123456.78m, new(2026, 10, 11, 3, 0, 0)),
            hasVisit ? new(Guid.NewGuid(), "RESERVATION", new(2026, 11, 3), "CANCELLED") : null,
            new(2026, 10, 10, 3, 0, 0));

    private sealed class Repository : IReservationRepository
    {
        public Guid? Customer { get; set; } = CustomerId;
        public ReservationDetailRecord Detail { get; set; } = ReservationCancellationTests.Detail();
        public string? ErrorCode { get; set; }
        public int CancelCalls { get; private set; }
        public Guid LastActor { get; private set; }
        public Guid LastCustomer { get; private set; }
        public Guid LastReservation { get; private set; }
        public CancellationToken LastToken { get; private set; }
        public Guid ListCustomer { get; private set; }
        public Task<Guid?> GetCustomerIdByUserAccountIdAsync(Guid userAccountId, CancellationToken cancellationToken) => Task.FromResult(Customer);
        public Task<ReservationDetailRecord> CancelAsync(Guid userAccountId, Guid customerId, Guid reservationId, CancellationToken cancellationToken)
        {
            CancelCalls++;
            LastActor = userAccountId;
            LastCustomer = customerId;
            LastReservation = reservationId;
            LastToken = cancellationToken;
            if (ErrorCode is { } code) throw new StoredProcedureBusinessException(code, code);
            if (customerId != Detail.CustomerId) throw new StoredProcedureBusinessException("RESOURCE_NOT_FOUND", "RESOURCE_NOT_FOUND");
            cancellationToken.ThrowIfCancellationRequested();
            return Task.FromResult(Detail);
        }
        public Task<ReservationDetailRecord?> GetByIdAsync(Guid reservationId, CancellationToken cancellationToken) => Task.FromResult<ReservationDetailRecord?>(Detail);
        public Task<ConfirmedReservationRecord> ConfirmAsync(Guid customerId, Guid reservationId, DateOnly reservationVisitDate, CancellationToken cancellationToken) =>
            Task.FromResult(new ConfirmedReservationRecord(reservationId, "CONFIRMED", new(Guid.NewGuid(), "RESERVATION", reservationVisitDate, "SCHEDULED")));
        public Task<CreatedReservationRecord> CreateAsync(Guid customerId, Guid facilityId, Guid unitTypeId, DateOnly startMonth, DateOnly endMonth, CancellationToken cancellationToken) => throw new NotSupportedException();
        public Task<ReservationPageRecord> ListByCustomerAsync(Guid customerId, int page, int pageSize, CancellationToken cancellationToken)
        {
            ListCustomer = customerId;
            return Task.FromResult(new ReservationPageRecord([Detail], page, pageSize, 6, 2));
        }
        public Task<ReservationPageRecord> ListByFacilityAsync(Guid facilityId, int page, int pageSize, CancellationToken cancellationToken) => throw new NotSupportedException();
        public Task<ReservationPageRecord> ListAllAsync(int page, int pageSize, CancellationToken cancellationToken) => throw new NotSupportedException();
    }

    private sealed class Auth : BusinessAuthentication
    {
        public string Role { get; set; } = "CUSTOMER";
        public string Status { get; set; } = "ACTIVE";
        public CancellationToken LastToken { get; private set; }
        public Task<CurrentAccountResult> GetCurrentAccountAsync(Guid userAccountId, CancellationToken cancellationToken)
        {
            LastToken = cancellationToken;
            return Task.FromResult(new CurrentAccountResult(userAccountId, Role, Status, "fixture@example.invalid", "fixture", CustomerId, null, null, "Fixture"));
        }
        public Task<bool> IsActiveAsync(Guid userAccountId, string role, CancellationToken cancellationToken) => Task.FromResult(Status == "ACTIVE" && Role == role);
        public Task<AuthenticationResult> LoginCustomerAsync(CustomerLoginCommand command, CancellationToken cancellationToken) => throw new NotSupportedException();
        public Task<AuthenticationResult> LoginEmployeeAsync(EmployeeLoginCommand command, CancellationToken cancellationToken) => throw new NotSupportedException();
    }

    public sealed class TestAuthentication(IOptionsMonitor<AuthenticationSchemeOptions> options,
        ILoggerFactory logger, UrlEncoder encoder) : AuthenticationHandler<AuthenticationSchemeOptions>(options, logger, encoder)
    {
        protected override Task<AuthenticateResult> HandleAuthenticateAsync()
        {
            var role = Request.Headers["X-Test-Role"].ToString();
            if (string.IsNullOrWhiteSpace(role)) return Task.FromResult(AuthenticateResult.NoResult());
            var claims = new[] { new Claim("sub", AccountId.ToString()), new Claim(ClaimTypes.Role, role) };
            return Task.FromResult(AuthenticateResult.Success(new AuthenticationTicket(
                new ClaimsPrincipal(new ClaimsIdentity(claims, Scheme.Name)), Scheme.Name)));
        }
    }
}
