using Frms.Business.Abstractions.External;
using Frms.Business.Abstractions.Security;
using Frms.Business.Abstractions.Time;
using Frms.Business.DependencyInjection;
using Frms.Business.Models.Commands;
using Frms.Business.Models.Results;
using Frms.Business.Services.Implementations;
using Frms.Business.Services.Interfaces;
using Frms.DataAccess.DependencyInjection;
using Frms.DataAccess.Persistence;
using Frms.DataAccess.Persistence.Entities;
using Frms.DataAccess.Repositories.Implementations;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using static Frms.IntegrationTests.PaymentSqlTestSupport;

namespace Frms.IntegrationTests;

[TestFixture, NonParallelizable, Category("PaymentServiceSql")]
public sealed class PaymentServiceSqlTests
{
    private const string ReturnUrl = "https://app.example.invalid/payment/vnpay-return";
    private const string SessionUrl = "https://sandbox.vnpayment.vn/paymentv2/vpcpay.html?fixture=private";
    private ServiceProvider provider = null!;

    [OneTimeSetUp]
    public async Task SetUpDatabase()
    {
        provider = new ServiceCollection().AddLogging().AddBusiness().AddDataAccess(ConnectionString())
            .AddSingleton<IPasswordHasher, UnusedPasswordHasher>()
            .AddSingleton<ITokenService, UnusedTokenService>()
            .BuildServiceProvider();
        await using var db = Open();
        await db.Database.MigrateAsync();
    }

    [OneTimeTearDown]
    public void TearDownDatabase() => provider.Dispose();

    [Test]
    public async Task CON_PAY_ServiceConcurrentSameKey_CreatesOneRowAndOneVnPayRedirect()
    {
        var (invoice, accountId) = await Seed();
        var key = Guid.NewGuid();
        var gateway = new ControlledGateway();
        using var first = OpenService(accountId, gateway);
        using var second = OpenService(accountId, gateway);

        var results = await Task.WhenAll(
            first.Service.StartInvoicePaymentAsync(invoice.InvoiceId, Command(key)),
            second.Service.StartInvoicePaymentAsync(invoice.InvoiceId, Command(key)));

        await using var db = Open();
        var rowCount = await db.Payments.CountAsync(payment => payment.IdempotencyKey == key);
        Assert.Multiple(() =>
        {
            Assert.That(results.Select(result => result.PaymentId).Distinct().Count(), Is.EqualTo(1));
            Assert.That(rowCount, Is.EqualTo(1));
            Assert.That(gateway.Creates, Is.EqualTo(1));
            Assert.That(results.Count(result => result.PaymentUrl == SessionUrl), Is.GreaterThanOrEqualTo(1));
        });
    }

    [Test]
    public async Task INT_PAY_CallbackAmountMismatchLeavesPaymentAndInvoicePendingWithSafeAudit()
    {
        var (invoice, accountId) = await Seed();
        var gateway = new ControlledGateway();
        using var service = OpenService(accountId, gateway);
        var attempt = await service.Service.StartInvoicePaymentAsync(invoice.InvoiceId, Command(Guid.NewGuid()));
        gateway.Verified = new(attempt.PaymentId, invoice.AmountDue + 1m, "900001",
            PaymentGatewayCallbackOutcome.Success, ProviderTime);

        var result = await service.Service.ProcessCallbackAsync(new("?signed=fixture"));

        await using var db = Open();
        var stored = await db.Payments.SingleAsync(payment => payment.PaymentId == attempt.PaymentId);
        var audit = await db.AuditLogs.SingleAsync(row => row.EntityId == attempt.PaymentId);
        var invoiceStatus = await db.Invoices.Where(row => row.InvoiceId == invoice.InvoiceId)
            .Select(row => row.Status).SingleAsync();
        Assert.Multiple(() =>
        {
            Assert.That(result.Outcome, Is.EqualTo(PaymentApplicationOutcome.AmountMismatch));
            Assert.That(stored.Status, Is.EqualTo("PENDING"));
            Assert.That(stored.PaidAt, Is.Null);
            Assert.That(invoiceStatus, Is.EqualTo("UNPAID"));
            Assert.That(audit.NewValue, Does.Contain("PAYMENT_AMOUNT_MISMATCH")
                .And.Not.Contain("signed=fixture").And.Not.Contain(SessionUrl));
        });
    }

    [Test]
    public async Task INT_PAY_VerifiedSuccessPaysInvoiceAndDuplicateIsIdempotent()
    {
        var (invoice, accountId) = await Seed();
        var gateway = new ControlledGateway();
        using var service = OpenService(accountId, gateway);
        var attempt = await service.Service.StartInvoicePaymentAsync(invoice.InvoiceId, Command(Guid.NewGuid()));
        gateway.Verified = new(attempt.PaymentId, invoice.AmountDue, ProviderReference(attempt.PaymentId),
            PaymentGatewayCallbackOutcome.Success, ProviderTime);

        var first = await service.Service.ProcessCallbackAsync(new("?signed=fixture"));
        var duplicate = await service.Service.ProcessCallbackAsync(new("?signed=fixture"));

        await using var db = Open();
        var invoiceStatus = await db.Invoices.Where(row => row.InvoiceId == invoice.InvoiceId)
            .Select(row => row.Status).SingleAsync();
        var auditCount = await db.AuditLogs.CountAsync(row => row.EntityId == attempt.PaymentId);
        Assert.Multiple(() =>
        {
            Assert.That(first.Outcome, Is.EqualTo(PaymentApplicationOutcome.Applied));
            Assert.That(duplicate.Outcome, Is.EqualTo(PaymentApplicationOutcome.Duplicate));
            Assert.That(first.Payment!.PaidAt, Is.EqualTo(ProviderTime.ToUniversalTime()));
            Assert.That(invoiceStatus, Is.EqualTo("PAID"));
            Assert.That(auditCount, Is.EqualTo(1));
        });
    }

    [Test]
    public async Task INT_PAY_LateSuccessKeepsCancelledInvoiceAndReservationCancelled()
    {
        var (invoice, accountId) = await Seed();
        var gateway = new ControlledGateway();
        using var service = OpenService(accountId, gateway);
        var attempt = await service.Service.StartInvoicePaymentAsync(invoice.InvoiceId, Command(Guid.NewGuid()));
        await using (var mutate = Open())
        {
            await mutate.Database.ExecuteSqlInterpolatedAsync(
                $"UPDATE dbo.Invoice SET Status='CANCELLED' WHERE InvoiceId={invoice.InvoiceId}");
            await mutate.Database.ExecuteSqlInterpolatedAsync(
                $"UPDATE dbo.Reservation SET Status='CANCELLED' WHERE ReservationId={invoice.EntityId}");
        }
        gateway.Verified = new(attempt.PaymentId, invoice.AmountDue, ProviderReference(attempt.PaymentId),
            PaymentGatewayCallbackOutcome.Success, ProviderTime);

        var result = await service.Service.ProcessCallbackAsync(new("?signed=fixture"));

        await using var db = Open();
        var audit = await db.AuditLogs.SingleAsync(row => row.EntityId == attempt.PaymentId);
        var invoiceStatus = await db.Invoices.Where(row => row.InvoiceId == invoice.InvoiceId)
            .Select(row => row.Status).SingleAsync();
        var reservationStatus = await db.Reservations.Where(row => row.ReservationId == invoice.EntityId)
            .Select(row => row.Status).SingleAsync();
        Assert.Multiple(() =>
        {
            Assert.That(result.Outcome, Is.EqualTo(PaymentApplicationOutcome.Applied));
            Assert.That(result.Reason, Is.EqualTo("LATE_SUCCESS_ON_CANCELLED_INVOICE"));
            Assert.That(result.Payment!.Status, Is.EqualTo("SUCCESS"));
            Assert.That(invoiceStatus, Is.EqualTo("CANCELLED"));
            Assert.That(reservationStatus, Is.EqualTo("CANCELLED"));
            Assert.That(audit.NewValue, Does.Contain("LATE_SUCCESS_ON_CANCELLED_INVOICE"));
        });
    }

    private static async Task<(Invoice Invoice, Guid AccountId)> Seed()
    {
        await using var db = Open();
        var invoice = await SeedInvoiceAsync(db);
        var scope = await new PaymentRepository(db).GetInvoiceAsync(invoice.InvoiceId, default);
        return (invoice, scope!.CustomerUserAccountId);
    }

    private static StartPaymentCommand Command(Guid key) =>
        new(key, ReturnUrl, "203.0.113.10");

    private static string ProviderReference(Guid paymentId) =>
        new System.Numerics.BigInteger(paymentId.ToByteArray(), isUnsigned: true, isBigEndian: true)
            .ToString(System.Globalization.CultureInfo.InvariantCulture);

    private ServiceScope OpenService(Guid accountId, ControlledGateway gateway) =>
        new(provider.CreateScope(), accountId, gateway);

    private sealed class ServiceScope : IDisposable
    {
        private readonly IServiceScope scope;
        public PaymentService Service { get; }

        public ServiceScope(IServiceScope scope, Guid accountId, ControlledGateway gateway)
        {
            this.scope = scope;
            var db = scope.ServiceProvider.GetRequiredService<FrmsDbContext>();
            Service = new PaymentService(new PaymentRepository(db), gateway, new Current(accountId),
                scope.ServiceProvider.GetRequiredService<IAuthenticationService>(), new FixedClock());
        }

        public void Dispose() => scope.Dispose();
    }

    private sealed class Current(Guid accountId) : ICurrentUserContext
    {
        public bool IsAuthenticated => true;
        public Guid UserAccountId => accountId;
        public string Role => throw new AssertionException("Current database role must be used.");
    }

    private sealed class FixedClock : IClock
    {
        public DateTimeOffset UtcNow => Now;
        public TimeZoneInfo BusinessTimeZone => TimeZoneInfo.Utc;
        public DateTimeOffset ToBusinessTime(DateTimeOffset timestamp) => timestamp;
    }

    private sealed class ControlledGateway : IPaymentGateway
    {
        private int creates;
        public int Creates => creates;
        public PaymentGatewayCallbackResult? Verified;
        public Task EnsureConfiguredAsync(CancellationToken token) => Task.CompletedTask;
        public void ValidatePaymentRequest(PaymentGatewayPreflight request) { }

        public async Task<PaymentGatewayCreationResult> CreatePaymentAsync(
            PaymentGatewayRequest request, CancellationToken token)
        {
            Interlocked.Increment(ref creates);
            await Task.Yield();
            return new(PaymentGatewayCreationOutcome.SessionCreated,
                new PaymentGatewaySession(SessionUrl, null, Now.AddMinutes(15)), null);
        }

        public Task<PaymentGatewayCallbackResult> VerifyAndNormalizeCallbackAsync(
            PaymentGatewayCallbackRequest request, CancellationToken token) =>
            Task.FromResult(Verified ?? throw new AssertionException("Configure callback result."));
    }

    private sealed class UnusedPasswordHasher : IPasswordHasher
    {
        public string Hash(string password) => throw new NotSupportedException();
        public bool Verify(string password, string passwordHash) => throw new NotSupportedException();
    }

    private sealed class UnusedTokenService : ITokenService
    {
        public IssuedToken Generate(Guid id, string role) => throw new NotSupportedException();
    }
}
