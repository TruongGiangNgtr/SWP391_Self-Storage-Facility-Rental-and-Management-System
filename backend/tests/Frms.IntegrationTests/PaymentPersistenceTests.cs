using Frms.DataAccess.Persistence;
using Frms.DataAccess.Persistence.Entities;
using Frms.DataAccess.Repositories.Implementations;
using Frms.DataAccess.Repositories.Models;
using Microsoft.Data.SqlClient;
using Microsoft.EntityFrameworkCore;
using static Frms.IntegrationTests.PaymentSqlTestSupport;

namespace Frms.IntegrationTests;

[TestFixture, NonParallelizable, Category("PaymentSql")]
public sealed partial class PaymentPersistenceTests
{
    private FrmsDbContext db = null!;
    private PaymentRepository repository = null!;

    [OneTimeSetUp]
    public async Task MigrateApprovedTestDatabase()
    {
        await using var migrationDb = Open();
        await migrationDb.Database.MigrateAsync();
    }

    [SetUp]
    public void SetUp()
    {
        db = Open();
        repository = new PaymentRepository(db);
    }

    [TearDown]
    public async Task TearDown() => await db.DisposeAsync();

    [TestCase(true, false)]
    [TestCase(false, true)]
    public async Task DBT_PAY_001_NullRequiredIdentityIsRejected(bool nullInvoice, bool nullKey)
    {
        var invoice = await SeedInvoiceAsync(db);
        var exception = Assert.ThrowsAsync<SqlException>(async () => await InsertPaymentAsync(db,
            Guid.NewGuid(), nullInvoice ? null : invoice.InvoiceId, nullKey ? null : Guid.NewGuid()));
        Assert.That(exception!.Number, Is.EqualTo(515));
    }

    [TestCase("IdempotencyKey")]
    [TestCase("TransactionCode")]
    public async Task DBT_PAY_002_DuplicateUniqueValueIsRejected(string field)
    {
        var invoice = await SeedInvoiceAsync(db);
        var key = Guid.NewGuid();
        var reference = Guid.NewGuid().ToString("N");
        await InsertPaymentAsync(db, Guid.NewGuid(), invoice.InvoiceId, key, reference);
        var exception = Assert.ThrowsAsync<SqlException>(async () => await InsertPaymentAsync(db,
            Guid.NewGuid(), invoice.InvoiceId, field == "IdempotencyKey" ? key : Guid.NewGuid(),
            field == "TransactionCode" ? reference : Guid.NewGuid().ToString("N")));
        Assert.That(exception!.Number, Is.AnyOf(2601, 2627));
    }

    [Test]
    public async Task DBT_PAY_003_MultipleNullReferencesArePermitted()
    {
        var invoice = await SeedInvoiceAsync(db);
        await InsertPaymentAsync(db, Guid.NewGuid(), invoice.InvoiceId, Guid.NewGuid());
        await InsertPaymentAsync(db, Guid.NewGuid(), invoice.InvoiceId, Guid.NewGuid());
        Assert.That(await db.Payments.CountAsync(row => row.InvoiceId == invoice.InvoiceId && row.TransactionCode == null), Is.EqualTo(2));
    }

    [TestCase("UNKNOWN", 1, false, "MOMO", "CK_Payment_Status")]
    [TestCase("PENDING", 0, false, "MOMO", "CK_Payment_Amount")]
    [TestCase("PENDING", -1, false, "MOMO", "CK_Payment_Amount")]
    [TestCase("SUCCESS", 1, false, "MOMO", "CK_Payment_PaidAt")]
    [TestCase("PENDING", 1, true, "MOMO", "CK_Payment_PaidAt")]
    [TestCase("FAILED", 1, true, "MOMO", "CK_Payment_PaidAt")]
    [TestCase("PENDING", 1, false, "CASH", "CK_Payment_Method")]
    public async Task DBT_PAY_004_InvalidValuesAreRejected(string status, int amount, bool paid, string method, string constraint)
    {
        var invoice = await SeedInvoiceAsync(db);
        var exception = Assert.ThrowsAsync<SqlException>(async () => await InsertPaymentAsync(db,
            Guid.NewGuid(), invoice.InvoiceId, Guid.NewGuid(), status: status, amount: amount,
            paidAt: paid ? Now.UtcDateTime : null, method: method));
        Assert.Multiple(() =>
        {
            Assert.That(exception!.Number, Is.EqualTo(547));
            Assert.That(exception.Message, Does.Contain(constraint));
        });
    }

    [Test]
    public async Task DBT_PAY_005_ExpiryRequiresUrlAndUrlIsBounded()
    {
        var attempt = await CreateAsync();
        var id = attempt.Detail.PaymentId;
        var expiryError = Assert.ThrowsAsync<SqlException>(async () => await db.Database.ExecuteSqlInterpolatedAsync(
            $"UPDATE dbo.Payment SET PaymentUrlExpiresAt = {Now.UtcDateTime} WHERE PaymentId = {id}"));
        var longUrl = new string('x', 2049);
        var lengthError = Assert.ThrowsAsync<SqlException>(async () => await db.Database.ExecuteSqlInterpolatedAsync(
            $"UPDATE dbo.Payment SET PaymentUrl = {longUrl} WHERE PaymentId = {id}"));
        Assert.Multiple(() =>
        {
            Assert.That(expiryError!.Number, Is.EqualTo(547));
            Assert.That(lengthError!.Number, Is.AnyOf(8152, 2628));
        });
    }

    [Test]
    public async Task DAL_PAY_001_SameKeyReturnsOneAttemptAndNewKeyCreatesAnother()
    {
        var invoice = await SeedInvoiceAsync(db);
        var key = Guid.NewGuid();
        var first = await repository.CreateOrGetAsync(invoice.InvoiceId, key, Now, default);
        var retry = await repository.CreateOrGetAsync(invoice.InvoiceId, key, Now, default);
        Assert.Multiple(() =>
        {
            Assert.That(first.Outcome, Is.EqualTo(PaymentAttemptOutcome.Created));
            Assert.That(retry.Outcome, Is.EqualTo(PaymentAttemptOutcome.Existing));
            Assert.That(retry.Attempt, Is.EqualTo(first.Attempt));
            Assert.That(first.Attempt!.Detail.Amount, Is.EqualTo(invoice.AmountDue));
        });
        Assert.That(await db.Payments.CountAsync(row => row.InvoiceId == invoice.InvoiceId), Is.EqualTo(1));
        var newAttempt = await repository.CreateOrGetAsync(invoice.InvoiceId, Guid.NewGuid(), Now, default);
        Assert.That(newAttempt.Attempt!.Detail.PaymentId, Is.Not.EqualTo(first.Attempt!.Detail.PaymentId));
        Assert.That(await db.Payments.CountAsync(row => row.InvoiceId == invoice.InvoiceId), Is.EqualTo(2));
    }

    [Test]
    public async Task DAL_PAY_002_KeyFromAnotherCustomerReturnsConflictWithoutAttempt()
    {
        var first = await CreateAsync();
        var otherInvoice = await SeedInvoiceAsync(db);
        var conflict = await repository.CreateOrGetAsync(otherInvoice.InvoiceId, first.IdempotencyKey, Now, default);
        var lookup = await repository.GetByIdempotencyKeyAsync(otherInvoice.InvoiceId, first.IdempotencyKey, Now, default);
        Assert.Multiple(() =>
        {
            Assert.That(conflict.Outcome, Is.EqualTo(PaymentAttemptOutcome.IdempotencyConflict));
            Assert.That(conflict.Attempt, Is.Null);
            Assert.That(lookup.Outcome, Is.EqualTo(PaymentAttemptOutcome.IdempotencyConflict));
            Assert.That(lookup.Attempt, Is.Null);
        });
        Assert.That(await db.Payments.CountAsync(row => row.InvoiceId == otherInvoice.InvoiceId), Is.Zero);
    }

    [TestCase("DEPOSIT", "PAID", 125, false)]
    [TestCase("DEPOSIT", "CANCELLED", 125, false)]
    [TestCase("DEPOSIT", "UNPAID", 0, false)]
    [TestCase("RENTAL_FEE", "UNPAID", 125, true)]
    public async Task DAL_PAY_003_IneligibleInvoiceCannotCreateAttempt(string type, string status, int amount, bool firstMonth)
    {
        var invoice = await SeedInvoiceAsync(db, type, status, amount, firstMonth);
        var result = await repository.CreateOrGetAsync(invoice.InvoiceId, Guid.NewGuid(), Now, default);
        Assert.That(result.Outcome, Is.EqualTo(PaymentAttemptOutcome.InvoiceNotPayable));
        Assert.That(await db.Payments.AnyAsync(row => row.InvoiceId == invoice.InvoiceId), Is.False);
    }

    [TestCase("DEPOSIT", "UNPAID")]
    [TestCase("DEPOSIT", "OVERDUE")]
    [TestCase("RENTAL_FEE", "UNPAID")]
    [TestCase("RENTAL_FEE", "OVERDUE")]
    public async Task DAL_PAY_004_EligibleInvoiceReturnsOwnershipForAllReadRoles(string type, string status)
    {
        var invoice = await SeedInvoiceAsync(db, type, status);
        var scope = await repository.GetInvoiceAsync(invoice.InvoiceId, default);
        var created = await repository.CreateOrGetAsync(invoice.InvoiceId, Guid.NewGuid(), Now, default);
        var detail = await repository.GetByIdAsync(created.Attempt!.Detail.PaymentId, default);
        var expectedCustomer = type == "DEPOSIT"
            ? await db.Reservations.Where(row => row.ReservationId == invoice.EntityId).Select(row => row.CustomerId).SingleAsync()
            : await db.Contracts.Where(row => row.ContractId == invoice.EntityId).Select(row => row.CustomerId).SingleAsync();
        var expectedFacility = type == "DEPOSIT"
            ? await db.Reservations.Where(row => row.ReservationId == invoice.EntityId).Select(row => row.FacilityId).SingleAsync()
            : await db.Contracts.Where(row => row.ContractId == invoice.EntityId).Select(row => row.FacilityId).SingleAsync();
        var expectedAccount = await db.Customers.Where(row => row.CustomerId == expectedCustomer).Select(row => row.UserAccountId).SingleAsync();
        Assert.Multiple(() =>
        {
            Assert.That(created.Outcome, Is.EqualTo(PaymentAttemptOutcome.Created));
            Assert.That(detail!.CustomerId, Is.EqualTo(expectedCustomer));
            Assert.That(detail.CustomerUserAccountId, Is.EqualTo(expectedAccount));
            Assert.That(detail.FacilityId, Is.EqualTo(expectedFacility));
            Assert.That(scope!.CustomerId, Is.EqualTo(expectedCustomer));
            Assert.That(scope.FacilityId, Is.EqualTo(expectedFacility));
        });
    }

    [Test]
    public async Task DAL_PAY_005_StoredSessionIsReusedAndExcludedFromDetailAndLogs()
    {
        var created = await CreateAsync();
        var url = "https://provider.example.invalid/session/" + Guid.NewGuid().ToString("N");
        var reference = Guid.NewGuid().ToString("N");
        var logs = new List<string>();
        await using var sessionDb = Open(log: logs.Add);
        var sessionRepository = new PaymentRepository(sessionDb);
        await sessionRepository.SaveSessionAsync(created.Detail.PaymentId, new(reference, url, Now.AddHours(1)), Now, default);
        var saved = await sessionRepository.SaveSessionAsync(created.Detail.PaymentId,
            new("replacement", "https://provider.example.invalid/replacement", null), Now, default);
        var read = await sessionRepository.GetByIdempotencyKeyAsync(created.Detail.InvoiceId, created.IdempotencyKey, Now, default);
        var detail = await sessionRepository.GetByIdAsync(created.Detail.PaymentId, default);
        Assert.Multiple(() =>
        {
            Assert.That(saved.Attempt!.PaymentUrl, Is.EqualTo(url));
            Assert.That(saved.Attempt.Detail.TransactionCode, Is.EqualTo(reference));
            Assert.That(saved.Attempt.PaymentUrlExpiresAt, Is.EqualTo(Now.AddHours(1)));
            Assert.That(read.Attempt, Is.EqualTo(saved.Attempt));
            Assert.That(typeof(PaymentDetailRecord).GetProperties().Select(property => property.Name),
                Does.Not.Contain("PaymentUrl").And.Not.Contain("IdempotencyKey").And.Not.Contain("PaymentUrlExpiresAt"));
            Assert.That(detail!.ToString(), Does.Not.Contain(url));
            Assert.That(saved.ToString(), Does.Not.Contain(url));
            Assert.That(string.Join('\n', logs), Does.Not.Contain(url));
        });
    }

    [Test]
    public async Task DAL_PAY_006_KnownExpiryConflictsAtBoundaryAndNullExpiryDoesNot()
    {
        var known = await CreateAsync();
        await repository.SaveSessionAsync(known.Detail.PaymentId, new(null, "https://provider.example.invalid/pay", Now), Now.AddSeconds(-1), default);
        var expired = await repository.CreateOrGetAsync(known.Detail.InvoiceId, known.IdempotencyKey, Now, default);
        Assert.That(expired.Outcome, Is.EqualTo(PaymentAttemptOutcome.SessionExpired));
        Assert.That(expired.Attempt!.Detail.PaymentId, Is.EqualTo(known.Detail.PaymentId));
        var unknown = await CreateAsync();
        await repository.SaveSessionAsync(unknown.Detail.PaymentId, new(null, "https://provider.example.invalid/pay", null), Now, default);
        var retained = await repository.CreateOrGetAsync(unknown.Detail.InvoiceId, unknown.IdempotencyKey, Now.AddYears(1), default);
        Assert.That(retained.Outcome, Is.EqualTo(PaymentAttemptOutcome.Existing));
    }

    [Test]
    public async Task CON_PAY_001_ConcurrentSameKeyCreatesExactlyOnePayment()
    {
        var invoice = await SeedInvoiceAsync(db);
        var key = Guid.NewGuid();
        var results = await RaceAsync(8, repo => repo.CreateOrGetAsync(invoice.InvoiceId, key, Now, default));
        Assert.Multiple(() =>
        {
            Assert.That(results.Count(result => result.Outcome == PaymentAttemptOutcome.Created), Is.EqualTo(1));
            Assert.That(results.Select(result => result.Attempt!.Detail.PaymentId).Distinct().Count(), Is.EqualTo(1));
        });
        Assert.That(await db.Payments.CountAsync(row => row.IdempotencyKey == key), Is.EqualTo(1));
    }

    [Test]
    public async Task CON_PAY_002_ConcurrentKeyAcrossInvoicesDoesNotLeakWinningPayment()
    {
        var invoices = new[] { await SeedInvoiceAsync(db), await SeedInvoiceAsync(db) };
        var key = Guid.NewGuid();
        var next = -1;
        var results = await RaceAsync(2, repo => repo.CreateOrGetAsync(invoices[Interlocked.Increment(ref next)].InvoiceId, key, Now, default));
        Assert.Multiple(() =>
        {
            Assert.That(results.Count(result => result.Outcome == PaymentAttemptOutcome.Created), Is.EqualTo(1));
            Assert.That(results.Count(result => result.Outcome == PaymentAttemptOutcome.IdempotencyConflict && result.Attempt is null), Is.EqualTo(1));
        });
        Assert.That(await db.Payments.CountAsync(row => row.IdempotencyKey == key), Is.EqualTo(1));
    }

    private async Task<PaymentAttemptRecord> CreateAsync(string type = "DEPOSIT", string invoiceStatus = "UNPAID")
    {
        var invoice = await SeedInvoiceAsync(db, type, invoiceStatus);
        var result = await repository.CreateOrGetAsync(invoice.InvoiceId, Guid.NewGuid(), Now, default);
        Assert.That(result.Outcome, Is.EqualTo(PaymentAttemptOutcome.Created));
        return result.Attempt!;
    }

    private static async Task<T[]> RaceAsync<T>(int count, Func<PaymentRepository, Task<T>> operation)
    {
        var ready = new TaskCompletionSource(TaskCreationOptions.RunContinuationsAsynchronously);
        var waiting = 0;
        var tasks = Enumerable.Range(0, count).Select(async _ =>
        {
            await using var connection = Open();
            await connection.Database.OpenConnectionAsync();
            if (Interlocked.Increment(ref waiting) == count) ready.SetResult();
            await ready.Task;
            return await operation(new PaymentRepository(connection));
        });
        return await Task.WhenAll(tasks).WaitAsync(TimeSpan.FromSeconds(45));
    }
}
