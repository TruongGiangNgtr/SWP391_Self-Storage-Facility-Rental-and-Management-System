using Frms.DataAccess.Persistence;
using Frms.DataAccess.Repositories.Implementations;
using Frms.DataAccess.Repositories.Models;
using Microsoft.Data.SqlClient;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Infrastructure;
using Microsoft.EntityFrameworkCore.Migrations;
using static Frms.IntegrationTests.PaymentSqlTestSupport;

namespace Frms.IntegrationTests;

[TestFixture, NonParallelizable, Category("PayOsSql")]
public sealed class PayOsPersistenceTests
{
    [Test]
    public async Task DBT_PAYOS_ForwardMigrationPreservesLegacyRowsAndPermitsOnlyApprovedMethods()
    {
        await using var db = Open(Dedicated("Legacy"));
        var migrator = db.GetService<IMigrator>();
        await Empty(db);
        try
        {
            await migrator.MigrateAsync(InvoiceTimestampMigration);
            var invoice = await SeedInvoiceAsync(db);
            var first = Guid.NewGuid(); var second = Guid.NewGuid(); var key1 = Guid.NewGuid(); var key2 = Guid.NewGuid();
            await InsertPaymentAsync(db, first, invoice.InvoiceId, key1, "legacy-momo", method: "MOMO");
            await InsertPaymentAsync(db, second, invoice.InvoiceId, key2, "legacy-vnpay", method: "VNPAY");
            await migrator.MigrateAsync();
            var legacy = await db.Payments.AsNoTracking().OrderBy(p => p.PaymentMethod).ToListAsync();
            Assert.Multiple(() =>
            {
                Assert.That(legacy, Has.Count.EqualTo(2));
                Assert.That(legacy.Select(p => p.PaymentId), Is.EquivalentTo(new[] { first, second }));
                Assert.That(legacy.Select(p => p.IdempotencyKey), Is.EquivalentTo(new[] { key1, key2 }));
                Assert.That(legacy.Select(p => p.PaymentMethod), Is.EquivalentTo(new[] { "MOMO", "VNPAY" }));
                Assert.That(legacy.All(p => p.ProviderOrderCode is null && p.Amount == 12500 && p.Status == "PENDING" && p.PaidAt is null), Is.True);
                Assert.That(legacy.Select(p => p.TransactionCode), Is.EquivalentTo(new[] { "legacy-momo", "legacy-vnpay" }));
                Assert.That(legacy.All(p => p.CreatedAt == Now.UtcDateTime), Is.True);
            });
            var created = await new PaymentRepository(db).CreateOrGetAsync(invoice.InvoiceId, Guid.NewGuid(), Now, default);
            Assert.That(created.Attempt!.Detail.PaymentMethod, Is.EqualTo("PAYOS"));
            Assert.That(created.Attempt.ProviderOrderCode, Is.EqualTo(1000));
            var rejected = Assert.ThrowsAsync<SqlException>(() => InsertPaymentAsync(db, Guid.NewGuid(), invoice.InvoiceId, Guid.NewGuid(), method: "UNKNOWN"));
            Assert.That(rejected!.Number, Is.EqualTo(547));
            Assert.That(rejected.Message, Does.Contain("CK_Payment_Method"));
            Assert.That(await db.Payments.CountAsync(), Is.EqualTo(3));
        }
        finally { await Clean(db, migrator); }
    }

    [Test]
    public async Task DBT_PAYOS_FilteredUniqueOrderCodeAllowsLegacyNullsAndRejectsDuplicates()
    {
        await using var db = Open(Dedicated("Unique"));
        await Empty(db); var migrator = db.GetService<IMigrator>();
        try
        {
            await migrator.MigrateAsync();
            var invoice = await SeedInvoiceAsync(db);
            await InsertPaymentAsync(db, Guid.NewGuid(), invoice.InvoiceId, Guid.NewGuid(), method: "MOMO");
            await InsertPaymentAsync(db, Guid.NewGuid(), invoice.InvoiceId, Guid.NewGuid(), method: "VNPAY");
            var repository = new PaymentRepository(db);
            var first = await repository.CreateOrGetAsync(invoice.InvoiceId, Guid.NewGuid(), Now, default);
            var second = await repository.CreateOrGetAsync(invoice.InvoiceId, Guid.NewGuid(), Now, default);
            Assert.That(first.Attempt!.ProviderOrderCode, Is.Not.EqualTo(second.Attempt!.ProviderOrderCode));
            var rejected = Assert.ThrowsAsync<SqlException>(() => db.Database.ExecuteSqlInterpolatedAsync(
                $"UPDATE dbo.Payment SET ProviderOrderCode={first.Attempt.ProviderOrderCode} WHERE PaymentId={second.Attempt.Detail.PaymentId}"));
            Assert.That(rejected!.Number, Is.AnyOf(2601, 2627));
            Assert.That(rejected.Message, Does.Contain("IX_Payment_ProviderOrderCode"));
            Assert.That(await db.Payments.CountAsync(p => p.ProviderOrderCode == null), Is.EqualTo(2));
            Assert.That((await repository.GetByProviderOrderCodeAsync(first.Attempt.ProviderOrderCode!.Value, default))!.PaymentId, Is.EqualTo(first.Attempt.Detail.PaymentId));
            Assert.That(await repository.GetByProviderOrderCodeAsync(123, default), Is.Null);
        }
        finally { await Clean(db, migrator); }
    }

    [Test]
    public async Task CON_PAYOS_ConcurrentDistinctAttemptsReceiveUniqueSequenceCodesAndSameKeyReusesCode()
    {
        await using var db = Open(Dedicated("Concurrency"));
        await Empty(db); var migrator = db.GetService<IMigrator>();
        try
        {
            await migrator.MigrateAsync();
            var invoices = new List<Guid>();
            for (var i = 0; i < 8; i++) invoices.Add((await SeedInvoiceAsync(db)).InvoiceId);
            var results = await Task.WhenAll(invoices.Select(async id =>
            {
                await using var connection = Open(Dedicated("Concurrency"));
                return await new PaymentRepository(connection).CreateOrGetAsync(id, Guid.NewGuid(), Now, default);
            }));
            Assert.That(results.All(r => r.Outcome == PaymentAttemptOutcome.Created), Is.True);
            Assert.That(results.Select(r => r.Attempt!.ProviderOrderCode).Distinct().Count(), Is.EqualTo(8));
            Assert.That(results.All(r => r.Attempt!.ProviderOrderCode >= 1000), Is.True);
            var first = results[0].Attempt!;
            var retry = await new PaymentRepository(db).CreateOrGetAsync(first.Detail.InvoiceId, first.IdempotencyKey, Now, default);
            Assert.That(retry.Outcome, Is.EqualTo(PaymentAttemptOutcome.Existing));
            Assert.That(retry.Attempt!.ProviderOrderCode, Is.EqualTo(first.ProviderOrderCode));
            Assert.That(await db.Payments.CountAsync(), Is.EqualTo(8));
        }
        finally { await Clean(db, migrator); }
    }

    [Test]
    public async Task DBT_PAYOS_DowngradeIsBlockedWithoutLosingProviderEvidence()
    {
        await using var db = Open(Dedicated("Guard"));
        await Empty(db); var migrator = db.GetService<IMigrator>();
        try
        {
            await migrator.MigrateAsync();
            var invoice = await SeedInvoiceAsync(db);
            var attempt = (await new PaymentRepository(db).CreateOrGetAsync(invoice.InvoiceId, Guid.NewGuid(), Now, default)).Attempt!;
            var rejected = Assert.ThrowsAsync<SqlException>(() => migrator.MigrateAsync(InvoiceTimestampMigration));
            Assert.That(rejected!.Number, Is.EqualTo(51032));
            Assert.That(rejected.Message, Does.Contain("PAYOS rows"));
            var stored = await db.Payments.AsNoTracking().SingleAsync();
            Assert.That(stored.PaymentMethod, Is.EqualTo("PAYOS"));
            Assert.That(stored.ProviderOrderCode, Is.EqualTo(attempt.ProviderOrderCode));
            Assert.That(await db.Database.GetAppliedMigrationsAsync(), Does.Contain("20261008191750_PayOsPaymentGateway"));
        }
        finally { await Clean(db, migrator); }
    }

    [Test]
    public async Task DAL_PAYOS_FractionalVndDoesNotConsumeKeyOrSequence()
    {
        await using var db = Open(Dedicated("Fraction"));
        await Empty(db); var migrator = db.GetService<IMigrator>();
        try
        {
            await migrator.MigrateAsync();
            var fractional = await SeedInvoiceAsync(db, amount: 12500.50m);
            var result = await new PaymentRepository(db).CreateOrGetAsync(fractional.InvoiceId, Guid.NewGuid(), Now, default);
            Assert.That(result.Outcome, Is.EqualTo(PaymentAttemptOutcome.AmountUnsupported));
            Assert.That(await db.Payments.CountAsync(), Is.Zero);
            var eligible = await SeedInvoiceAsync(db);
            var accepted = await new PaymentRepository(db).CreateOrGetAsync(eligible.InvoiceId, Guid.NewGuid(), Now, default);
            Assert.That(accepted.Attempt!.ProviderOrderCode, Is.EqualTo(1000));
        }
        finally { await Clean(db, migrator); }
    }

    private static string Dedicated(string suffix)
    {
        var builder = new SqlConnectionStringBuilder(ConnectionString());
        builder.InitialCatalog += "_PayOs" + suffix;
        Assert.That(builder.InitialCatalog, Does.StartWith("Frms_Test_"));
        return builder.ConnectionString;
    }
    private static async Task Empty(FrmsDbContext db)
    {
        if (!await db.Database.CanConnectAsync()) return;
        Assert.That(await db.Database.SqlQueryRaw<int>("SELECT COUNT(*) AS Value FROM sys.tables WHERE name <> '__EFMigrationsHistory'").SingleAsync(), Is.Zero);
        Assert.That(await db.Database.GetAppliedMigrationsAsync(), Is.Empty);
    }
    private static async Task Clean(FrmsDbContext db, IMigrator migrator)
    {
        // Exact dedicated Frms_Test_* database created by this fixture only.
        await db.Database.ExecuteSqlRawAsync("DELETE dbo.Payment; DELETE dbo.Invoice;");
        await migrator.MigrateAsync(Migration.InitialDatabase);
    }
}
