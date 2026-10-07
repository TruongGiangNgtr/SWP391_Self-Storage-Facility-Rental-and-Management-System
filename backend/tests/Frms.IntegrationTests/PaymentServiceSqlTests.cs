using Frms.Business.Abstractions.External;
using Frms.Business.Abstractions.Security;
using Frms.Business.Abstractions.Time;
using Frms.Business.DependencyInjection;
using Frms.Business.Exceptions;
using Frms.Business.Models.Commands;
using Frms.Business.Models.Results;
using Frms.Business.Services.Implementations;
using Frms.Business.Services.Interfaces;
using Frms.DataAccess.DependencyInjection;
using Frms.DataAccess.Persistence;
using Frms.DataAccess.Persistence.Entities;
using Frms.DataAccess.Repositories.Implementations;
using Frms.DataAccess.Repositories.Models;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using static Frms.IntegrationTests.PaymentSqlTestSupport;

namespace Frms.IntegrationTests;

[TestFixture, NonParallelizable, Category("PaymentServiceSql")]
public sealed class PaymentServiceSqlTests
{
    private ServiceProvider provider = null!;
    private const string SessionUrl = "https://payments.example.invalid/session?token=private-fixture";

    [OneTimeSetUp]
    public async Task Setup()
    {
        provider = new ServiceCollection().AddLogging().AddBusiness().AddDataAccess(ConnectionString())
            .AddSingleton<IPasswordHasher, UnusedPasswordHasher>().AddSingleton<ITokenService, UnusedTokenService>()
            .BuildServiceProvider();
        await using var db = Open();
        await db.Database.MigrateAsync();
    }

    [OneTimeTearDown] public void Cleanup() => provider?.Dispose();

    [Test]
    public async Task CON_PAY_Service_SameKeyAcrossIndependentContexts_CreatesOnePaymentAndOneSession()
    {
        var (invoice, scope) = await Seed();
        var key = Guid.NewGuid();
        var gateway = new ControlledGateway { BlockCreation = true };
        using var first = OpenService(scope.CustomerUserAccountId, gateway);
        using var second = OpenService(scope.CustomerUserAccountId, gateway);
        Assert.That(first.Database, Is.Not.SameAs(second.Database));
        gateway.AssertNoTransaction = () =>
        {
            Assert.That(first.Database.Database.CurrentTransaction, Is.Null);
            Assert.That(second.Database.Database.CurrentTransaction, Is.Null);
        };
        var a = first.Service.StartInvoicePaymentAsync(invoice.InvoiceId, Command(key));
        var b = second.Service.StartInvoicePaymentAsync(invoice.InvoiceId, Command(key));
        InvoicePaymentStartResult pending;
        try
        {
            await gateway.Entered.Task.WaitAsync(TimeSpan.FromSeconds(20));
            var loser = await Task.WhenAny(a, b).WaitAsync(TimeSpan.FromSeconds(20));
            pending = await loser;
            Assert.That(pending.Outcome, Is.EqualTo(PaymentStartOutcome.SessionUnavailable));
            Assert.That(pending.PaymentUrl, Is.Null);
            Assert.That(pending.Status, Is.EqualTo("PENDING"));
        }
        finally { gateway.Release.TrySetResult(); }
        var results = await Task.WhenAll(a, b).WaitAsync(TimeSpan.FromSeconds(20));
        Assert.That(results.Select(r => r.PaymentId).Distinct().Count(), Is.EqualTo(1));
        Assert.That(results.Count(r => r.Outcome == PaymentStartOutcome.SessionAvailable), Is.EqualTo(1));
        Assert.That(gateway.Creates, Is.EqualTo(1));
        await using var check = Open();
        var row = await check.Payments.SingleAsync(p => p.IdempotencyKey == key);
        Assert.That(row.PaymentId, Is.EqualTo(pending.PaymentId));
        Assert.That(row.Amount, Is.EqualTo(invoice.AmountDue));
        Assert.That(row.Status, Is.EqualTo("PENDING"));
        Assert.That(await check.Payments.CountAsync(p => p.InvoiceId == invoice.InvoiceId), Is.EqualTo(1));
        Assert.That(await check.Invoices.Where(i => i.InvoiceId == invoice.InvoiceId).Select(i => i.Status).SingleAsync(), Is.EqualTo("UNPAID"));
        Assert.That(await check.AuditLogs.CountAsync(x => x.EntityId == row.PaymentId), Is.Zero);
        using var retry = OpenService(scope.CustomerUserAccountId, gateway);
        var saved = await retry.Service.StartInvoicePaymentAsync(invoice.InvoiceId, new(key, "https://changed.example.invalid/return"));
        Assert.That(saved.PaymentId, Is.EqualTo(row.PaymentId));
        Assert.That(saved.PaymentUrl == SessionUrl, Is.True);
        Assert.That(saved.PaymentUrlExpiresAt, Is.Null);
        Assert.That(gateway.Creates, Is.EqualTo(1));
    }

    [Test]
    public async Task INT_PAY_Service_CallbackWhileCreating_ReturnsAuthoritativeTerminalWithoutStaleUrl()
    {
        var (invoice, scope) = await Seed();
        var gateway = new ControlledGateway { BlockCreation = true };
        var key = Guid.NewGuid();
        using var start = OpenService(scope.CustomerUserAccountId, gateway);
        var creating = start.Service.StartInvoicePaymentAsync(invoice.InvoiceId, Command(key));
        try
        {
            await gateway.Entered.Task.WaitAsync(TimeSpan.FromSeconds(20));
            using var callback = OpenService(Guid.Empty, gateway);
            gateway.Verified = Success(gateway.Request!.PaymentId);
            var applied = await callback.Service.ProcessCallbackAsync(new("untrusted-test-input"));
            Assert.That(applied.Outcome, Is.EqualTo(PaymentApplicationOutcome.Applied));
            Assert.That(applied.Payment!.PaidAt, Is.EqualTo(ProviderTime.ToUniversalTime()));
            Assert.That(applied.Payment.PaidAt!.Value.Offset, Is.EqualTo(TimeSpan.Zero));
        }
        finally { gateway.Release.TrySetResult(); }
        var result = await creating.WaitAsync(TimeSpan.FromSeconds(20));
        Assert.That(result.Status, Is.EqualTo("SUCCESS"));
        Assert.That(result.PaymentUrl, Is.Null);
        Assert.That(result.Outcome, Is.EqualTo(PaymentStartOutcome.Terminal));
        using var retry = OpenService(scope.CustomerUserAccountId, gateway);
        Assert.That((await retry.Service.StartInvoicePaymentAsync(invoice.InvoiceId, Command(key))).Status, Is.EqualTo("SUCCESS"));
        Assert.That(gateway.Creates, Is.EqualTo(1));
        await using var db = Open();
        Assert.That(await db.Payments.Where(p => p.PaymentId == result.PaymentId).Select(p => p.TransactionCode).SingleAsync(), Is.EqualTo(gateway.Verified!.TransactionCode));
        Assert.That(await db.Invoices.Where(i => i.InvoiceId == invoice.InvoiceId).Select(i => i.Status).SingleAsync(), Is.EqualTo("PAID"));
        Assert.That(await db.AuditLogs.CountAsync(x => x.EntityId == result.PaymentId && x.Action == "PAYMENT_RESULT"), Is.EqualTo(1));
    }

    [Test]
    public async Task INT_PAY_Service_VerificationRefusalThenExactMismatch_UsesSqlDiagnosticWithoutFinancialMutation()
    {
        var (invoice, scope) = await Seed();
        var gateway = new ControlledGateway();
        using var service = OpenService(scope.CustomerUserAccountId, gateway);
        var attempt = await service.Service.StartInvoicePaymentAsync(invoice.InvoiceId, Command(Guid.NewGuid()));
        gateway.Verified = Success(attempt.PaymentId) with { Status = PaymentGatewayCallbackOutcome.Unverified };
        Assert.That((await service.Service.ProcessCallbackAsync(new("untrusted-test-input"))).Outcome, Is.EqualTo(PaymentApplicationOutcome.VerificationRejected));
        await using var db = Open();
        Assert.That(await db.AuditLogs.CountAsync(a => a.EntityId == attempt.PaymentId), Is.Zero);
        gateway.Verified = Success(attempt.PaymentId) with { Amount = invoice.AmountDue + 0.0001m };
        var result = await service.Service.ProcessCallbackAsync(new("untrusted-test-input"));
        Assert.That(result.Outcome, Is.EqualTo(PaymentApplicationOutcome.AmountMismatch));
        Assert.That(result.Payment!.Status, Is.EqualTo("PENDING"));
        Assert.That(await db.Payments.Where(p => p.PaymentId == attempt.PaymentId).Select(p => p.PaidAt).SingleAsync(), Is.Null);
        Assert.That(await db.Invoices.Where(i => i.InvoiceId == invoice.InvoiceId).Select(i => i.Status).SingleAsync(), Is.EqualTo("UNPAID"));
        var audits = await db.AuditLogs.Where(a => a.EntityId == attempt.PaymentId).ToListAsync();
        Assert.That(audits, Has.Count.EqualTo(1));
        Assert.That(audits[0].NewValue, Does.Contain("AMOUNT_MISMATCH").And.Not.Contain("untrusted-test-input").And.Not.Contain("private-fixture"));
    }

    [TestCase("DEPOSIT"), TestCase("RENTAL_FEE")]
    public async Task INT_PAY_Service_LateSuccessAndDuplicate_PreserveCancelledLifecycleAndSingleAudit(string type)
    {
        var (invoice, scope) = await Seed(type);
        var gateway = new ControlledGateway();
        using var service = OpenService(scope.CustomerUserAccountId, gateway);
        var key = Guid.NewGuid();
        var attempt = await service.Service.StartInvoicePaymentAsync(invoice.InvoiceId, Command(key));
        await using var db = Open();
        var reservationId = type == "DEPOSIT" ? invoice.EntityId : await db.Contracts.Where(c => c.ContractId == invoice.EntityId).Select(c => c.ReservationId).SingleAsync();
        await db.Database.ExecuteSqlInterpolatedAsync($"UPDATE dbo.Invoice SET Status = 'CANCELLED' WHERE InvoiceId = {invoice.InvoiceId}");
        await db.Database.ExecuteSqlInterpolatedAsync($"UPDATE dbo.Reservation SET Status = 'CANCELLED' WHERE ReservationId = {reservationId}");
        gateway.Verified = Success(attempt.PaymentId);
        var first = await service.Service.ProcessCallbackAsync(new("untrusted-test-input"));
        var repeated = await service.Service.ProcessCallbackAsync(new("untrusted-test-input"));
        Assert.That(first.Outcome, Is.EqualTo(PaymentApplicationOutcome.Applied));
        Assert.That(first.Reason, Is.EqualTo("LATE_SUCCESS_ON_CANCELLED_INVOICE"));
        Assert.That(first.Payment!.Status, Is.EqualTo("SUCCESS"));
        Assert.That(repeated.Outcome, Is.EqualTo(PaymentApplicationOutcome.Duplicate));
        Assert.That(repeated.Payment, Is.EqualTo(first.Payment));
        Assert.That(await db.Invoices.Where(i => i.InvoiceId == invoice.InvoiceId).Select(i => i.Status).SingleAsync(), Is.EqualTo("CANCELLED"));
        Assert.That(await db.Reservations.Where(r => r.ReservationId == reservationId).Select(r => r.Status).SingleAsync(), Is.EqualTo("CANCELLED"));
        if (type == "RENTAL_FEE") Assert.That(await db.Contracts.Where(c => c.ContractId == invoice.EntityId).Select(c => c.Status).SingleAsync(), Is.EqualTo("ACTIVE"));
        var audit = await db.AuditLogs.Where(a => a.EntityId == attempt.PaymentId).SingleAsync();
        Assert.That(audit.Action, Is.EqualTo("PAYMENT_RESULT"));
        Assert.That(audit.NewValue, Does.Contain("LATE_SUCCESS_ON_CANCELLED_INVOICE").And.Not.Contain("private-fixture").And.Not.Contain("untrusted-test-input"));
        var existing = await service.Service.StartInvoicePaymentAsync(invoice.InvoiceId, Command(key));
        Assert.That(existing.Outcome, Is.EqualTo(PaymentStartOutcome.Terminal));
        Assert.That(existing.PaymentUrl, Is.Null);
        Assert.That(gateway.Creates, Is.EqualTo(1));
    }

    [Test]
    public async Task SEC_PAY_Service_CurrentDatabaseAssignmentRoleAndStatus_OverrideStaleClaims()
    {
        var (invoice, scope) = await Seed();
        var gateway = new ControlledGateway();
        using var customer = OpenService(scope.CustomerUserAccountId, gateway);
        var payment = await customer.Service.StartInvoicePaymentAsync(invoice.InvoiceId, Command(Guid.NewGuid()));
        await using var db = Open();
        var accountId = Guid.NewGuid(); var employeeId = Guid.NewGuid();
        db.AddRange(new UserAccount { UserAccountId = accountId, RoleId = SeedIds.FacilityStaffRole, Status = "ACTIVE",
            Email = $"{accountId:N}@example.invalid", PhoneNumber = accountId.ToString("N")[..25], PasswordHash = "unused-payment-fixture", CreatedAt = Now.UtcDateTime },
            new Employee { EmployeeId = employeeId, UserAccountId = accountId, FacilityId = scope.FacilityId, FullName = "Payment service fixture" });
        await db.SaveChangesAsync();
        using var employee = OpenService(accountId, gateway);
        Assert.That((await employee.Service.GetByIdAsync(payment.PaymentId)).PaymentId, Is.EqualTo(payment.PaymentId));
        await db.Database.ExecuteSqlInterpolatedAsync($"UPDATE dbo.Employee SET FacilityId = NULL WHERE EmployeeId = {employeeId}");
        Assert.That(Assert.ThrowsAsync<BusinessException>(() => employee.Service.GetByIdAsync(payment.PaymentId))!.Code, Is.EqualTo("FORBIDDEN"));
        await db.Database.ExecuteSqlInterpolatedAsync($"UPDATE dbo.UserAccount SET RoleId = {SeedIds.BusinessOperationsManagerRole} WHERE UserAccountId = {accountId}");
        Assert.That((await employee.Service.GetByIdAsync(payment.PaymentId)).PaymentId, Is.EqualTo(payment.PaymentId));
        await db.Database.ExecuteSqlInterpolatedAsync($"UPDATE dbo.UserAccount SET RoleId = {SeedIds.SystemAdministratorRole} WHERE UserAccountId = {accountId}");
        Assert.That(Assert.ThrowsAsync<BusinessException>(() => employee.Service.GetByIdAsync(payment.PaymentId))!.Code, Is.EqualTo("FORBIDDEN"));
        await db.Database.ExecuteSqlInterpolatedAsync($"UPDATE dbo.UserAccount SET Status = 'INACTIVE' WHERE UserAccountId = {accountId}");
        Assert.That(Assert.ThrowsAsync<BusinessException>(() => employee.Service.GetByIdAsync(payment.PaymentId))!.Code, Is.EqualTo("ACCOUNT_INACTIVE"));
    }

    [Test]
    public async Task SEC_PAY_Service_KeyFromAnotherCustomer_IsNonDisclosingAndNewKeyCreatesNewAttempt()
    {
        var (firstInvoice, firstScope) = await Seed();
        var (otherInvoice, otherScope) = await Seed();
        var key = Guid.NewGuid();
        var gateway = new ControlledGateway();
        using var first = OpenService(firstScope.CustomerUserAccountId, gateway);
        using var other = OpenService(otherScope.CustomerUserAccountId, gateway);
        var original = await first.Service.StartInvoicePaymentAsync(firstInvoice.InvoiceId, Command(key));
        var ownership = Assert.ThrowsAsync<BusinessException>(() => other.Service.StartInvoicePaymentAsync(firstInvoice.InvoiceId, Command(key)));
        Assert.That(ownership!.Code, Is.EqualTo("FORBIDDEN"));
        var conflict = Assert.ThrowsAsync<BusinessException>(() => other.Service.StartInvoicePaymentAsync(otherInvoice.InvoiceId, Command(key)));
        Assert.That(conflict!.Code, Is.EqualTo("PAYMENT_IDEMPOTENCY_CONFLICT"));
        Assert.That(conflict.SuggestedStatusCode, Is.EqualTo(409));
        Assert.That(conflict.ToString(), Does.Not.Contain(original.PaymentId.ToString()).And.Not.Contain(key.ToString()).And.Not.Contain("private-fixture"));
        Assert.That(gateway.Creates, Is.EqualTo(1));
        await using var db = Open();
        Assert.That(await db.Payments.CountAsync(p => p.InvoiceId == otherInvoice.InvoiceId), Is.Zero);
        var deliberateRetry = await first.Service.StartInvoicePaymentAsync(firstInvoice.InvoiceId, Command(Guid.NewGuid()));
        Assert.That(deliberateRetry.PaymentId, Is.Not.EqualTo(original.PaymentId));
        Assert.That(await db.Payments.CountAsync(p => p.InvoiceId == firstInvoice.InvoiceId), Is.EqualTo(2));
        Assert.That(gateway.Creates, Is.EqualTo(2));
        Assert.That(await db.Invoices.Where(i => i.InvoiceId == firstInvoice.InvoiceId).Select(i => i.Status).SingleAsync(), Is.EqualTo("UNPAID"));
    }

    private static async Task<(Invoice, PaymentInvoiceRecord)> Seed(string type = "DEPOSIT")
    {
        await using var db = Open();
        var invoice = await SeedInvoiceAsync(db, type);
        return (invoice, (await new PaymentRepository(db).GetInvoiceAsync(invoice.InvoiceId, default))!);
    }
    private static StartPaymentCommand Command(Guid key) => new(key, "https://app.example.invalid/return");
    private static PaymentGatewayCallbackResult Success(Guid id) => new(id, 125.50m, Guid.NewGuid().ToString("N"), PaymentGatewayCallbackOutcome.Success, ProviderTime);
    private ServiceScope OpenService(Guid accountId, ControlledGateway gateway) => new(provider.CreateScope(), accountId, gateway);

    private sealed class ServiceScope : IDisposable
    {
        private readonly IServiceScope scope;
        public FrmsDbContext Database { get; }
        public PaymentService Service { get; }
        public ServiceScope(IServiceScope scope, Guid accountId, ControlledGateway gateway)
        {
            this.scope = scope;
            Database = scope.ServiceProvider.GetRequiredService<FrmsDbContext>();
            Service = new(new PaymentRepository(Database), gateway, new Current(accountId),
                scope.ServiceProvider.GetRequiredService<IAuthenticationService>(), new FixedClock());
        }
        public void Dispose() => scope.Dispose();
    }
    private sealed class Current(Guid id) : ICurrentUserContext
    {
        public bool IsAuthenticated => id != Guid.Empty;
        public Guid UserAccountId => id;
        public string Role => throw new AssertionException("The current database role must be used.");
    }
    private sealed class FixedClock : IClock
    {
        public DateTimeOffset UtcNow => Now;
        public TimeZoneInfo BusinessTimeZone => TimeZoneInfo.Utc;
        public DateTimeOffset ToBusinessTime(DateTimeOffset timestamp) => timestamp;
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
    private sealed class ControlledGateway : IPaymentGateway
    {
        public readonly TaskCompletionSource Entered = new(TaskCreationOptions.RunContinuationsAsynchronously);
        public readonly TaskCompletionSource Release = new(TaskCreationOptions.RunContinuationsAsynchronously);
        private int creates;
        public int Creates => creates;
        public bool BlockCreation;
        public Action? AssertNoTransaction;
        public PaymentGatewayRequest? Request;
        public PaymentGatewayCallbackResult? Verified;
        public Task EnsureConfiguredAsync(CancellationToken ct) { ct.ThrowIfCancellationRequested(); return Task.CompletedTask; }
        public async Task<PaymentGatewayCreationResult> CreatePaymentAsync(PaymentGatewayRequest request, CancellationToken ct)
        {
            Interlocked.Increment(ref creates);
            Request = request;
            AssertNoTransaction?.Invoke();
            Entered.TrySetResult();
            if (BlockCreation) await Release.Task.WaitAsync(TimeSpan.FromSeconds(25), ct);
            return new(PaymentGatewayCreationOutcome.SessionCreated, new(SessionUrl, Guid.NewGuid().ToString("N"), null), null);
        }
        public Task<PaymentGatewayCallbackResult> VerifyAndNormalizeCallbackAsync(PaymentGatewayCallbackRequest request, CancellationToken ct)
        {
            ct.ThrowIfCancellationRequested();
            return Task.FromResult(Verified ?? throw new AssertionException("Configure a verified fake outcome explicitly."));
        }
    }
}
