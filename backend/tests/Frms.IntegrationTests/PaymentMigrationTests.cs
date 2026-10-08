using Frms.DataAccess.Persistence;
using Microsoft.Data.SqlClient;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Infrastructure;
using Microsoft.EntityFrameworkCore.Migrations;
using static Frms.IntegrationTests.PaymentSqlTestSupport;

namespace Frms.IntegrationTests;

[TestFixture, NonParallelizable, Category("PaymentMigration")]
public sealed class PaymentMigrationTests
{
    [Test]
    public async Task DBT_PAY_M01_EmptyDatabaseUpgradeDowngradeAndRedeploy()
    {
        await using var db = Open(MigrationConnection("Roundtrip"));
        await RequireEmptyDatabaseAsync(db);
        var migrator = db.GetService<IMigrator>();
        try
        {
            await migrator.MigrateAsync();
            Assert.That(await db.Database.GetAppliedMigrationsAsync(), Does.Contain(VnPayMigration));
            Assert.That(await db.Database.GetAppliedMigrationsAsync(), Does.Contain(InvoiceTimestampMigration));
            Assert.That(await ProcedureExistsAsync(db), Is.True);
            Assert.That(await db.Payments.CountAsync(), Is.Zero);

            await migrator.MigrateAsync(PaymentMigration);
            Assert.That(await db.Database.GetAppliedMigrationsAsync(), Does.Not.Contain(VnPayMigration));
            await migrator.MigrateAsync();
            Assert.That(await db.Database.GetAppliedMigrationsAsync(), Does.Contain(VnPayMigration));
        }
        finally
        {
            await db.Database.ExecuteSqlRawAsync("DELETE dbo.Payment");
            await migrator.MigrateAsync(Migration.InitialDatabase);
        }
    }

    [Test]
    public async Task DBT_PAY_M02_VnPayMigrationAcceptsVnPayAndHistoricalMomoButRejectsOtherMethods()
    {
        await using var db = Open(MigrationConnection("MethodConstraint"));
        await RequireEmptyDatabaseAsync(db);
        var migrator = db.GetService<IMigrator>();
        try
        {
            await migrator.MigrateAsync();
            var invoice = await SeedInvoiceAsync(db);
            await InsertPaymentAsync(db, Guid.NewGuid(), invoice.InvoiceId, Guid.NewGuid(), method: "VNPAY");
            await InsertPaymentAsync(db, Guid.NewGuid(), invoice.InvoiceId, Guid.NewGuid(), method: "MOMO");
            var rejected = Assert.ThrowsAsync<SqlException>(() => InsertPaymentAsync(
                db, Guid.NewGuid(), invoice.InvoiceId, Guid.NewGuid(), method: "CASH"));

            Assert.That(rejected!.Number, Is.EqualTo(547));
            Assert.That(rejected.Message, Does.Contain("CK_Payment_Method"));
            Assert.That(await db.Payments.CountAsync(), Is.EqualTo(2));
        }
        finally
        {
            await db.Database.ExecuteSqlRawAsync("DELETE dbo.Payment");
            await migrator.MigrateAsync(Migration.InitialDatabase);
        }
    }

    [Test]
    public async Task DBT_PAY_M03_DowngradeFailsClearlyWhenVnPayRowsExist()
    {
        await using var db = Open(MigrationConnection("DowngradeGuard"));
        await RequireEmptyDatabaseAsync(db);
        var migrator = db.GetService<IMigrator>();
        Guid? paymentId = null;
        try
        {
            await migrator.MigrateAsync();
            var invoice = await SeedInvoiceAsync(db);
            paymentId = Guid.NewGuid();
            await InsertPaymentAsync(db, paymentId.Value, invoice.InvoiceId, Guid.NewGuid(), method: "VNPAY");

            var blocked = Assert.ThrowsAsync<SqlException>(() => migrator.MigrateAsync(PaymentMigration));

            Assert.That(blocked!.Number, Is.EqualTo(51030));
            Assert.That(blocked.Message, Does.Contain("VNPAY rows"));
            Assert.That(await db.Payments.AnyAsync(payment => payment.PaymentId == paymentId), Is.True);
        }
        finally
        {
            if (paymentId.HasValue)
                await db.Database.ExecuteSqlInterpolatedAsync($"DELETE dbo.Payment WHERE PaymentId={paymentId.Value}");
            await migrator.MigrateAsync(Migration.InitialDatabase);
        }
    }

    [TestCase("NullGuard", true, 51001)]
    [TestCase("KeyGuard", false, 51002)]
    public async Task DBT_PAY_M04_UnexpectedExistingRowsAbortPlan3UpgradeWithoutFabrication(
        string suffix, bool unlinked, int diagnostic)
    {
        await using var db = Open(MigrationConnection(suffix));
        await RequireEmptyDatabaseAsync(db);
        var migrator = db.GetService<IMigrator>();
        try
        {
            await migrator.MigrateAsync(BaselineMigration);
            var invoice = await SeedInvoiceAsync(db);
            var paymentId = Guid.NewGuid();
            Guid? invoiceId = unlinked ? null : invoice.InvoiceId;
            await db.Database.ExecuteSqlInterpolatedAsync($"""
                INSERT dbo.Payment (PaymentId, InvoiceId, Amount, PaymentMethod, Status, CreatedAt)
                VALUES ({paymentId}, {invoiceId}, 125.50, 'MOMO', 'PENDING', {Now.UtcDateTime})
                """);

            var exception = Assert.ThrowsAsync<SqlException>(() => migrator.MigrateAsync(PaymentMigration));

            Assert.That(exception!.Number, Is.EqualTo(diagnostic));
            Assert.That(exception.Message, Does.Contain("Payment migration blocked"));
            Assert.That(await db.Database.GetAppliedMigrationsAsync(), Does.Not.Contain(PaymentMigration));
        }
        finally
        {
            await migrator.MigrateAsync(Migration.InitialDatabase);
        }
    }

    [Test]
    public async Task DBT_PAY_M05_InvoiceTimestampMigrationPreservesHistoricalRowsWithoutFabricatingTime()
    {
        await using var db = Open(MigrationConnection("InvoiceTimestampHistory"));
        await RequireEmptyDatabaseAsync(db);
        var migrator = db.GetService<IMigrator>();
        try
        {
            await migrator.MigrateAsync(VnPayMigration);
            var invoice = await SeedInvoiceAsync(db, status: "PAID");

            await migrator.MigrateAsync();

            var historical = await db.Invoices.AsNoTracking().SingleAsync(row => row.InvoiceId == invoice.InvoiceId);
            Assert.Multiple(() =>
            {
                Assert.That(historical.Status, Is.EqualTo("PAID"));
                Assert.That(historical.AmountDue, Is.EqualTo(invoice.AmountDue));
                Assert.That(historical.PaidAt, Is.Null);
            });
            await migrator.MigrateAsync(VnPayMigration);
            Assert.That(await ScalarAsync<int>(db,
                "SELECT CASE WHEN COL_LENGTH('dbo.Invoice', 'PaidAt') IS NULL THEN 0 ELSE 1 END AS [Value]"), Is.Zero);
            Assert.That(await ProcedureExistsAsync(db), Is.True);
            await migrator.MigrateAsync();
            Assert.That(await db.Invoices.Where(row => row.InvoiceId == invoice.InvoiceId).Select(row => row.PaidAt).SingleAsync(), Is.Null);
        }
        finally
        {
            await migrator.MigrateAsync(Migration.InitialDatabase);
        }
    }

    [Test]
    public async Task DBT_PAY_M06_InvoiceTimestampEvidenceBlocksDowngradeWithoutChangingDataOrProcedure()
    {
        await using var db = Open(MigrationConnection("InvoiceTimestampGuard"));
        await RequireEmptyDatabaseAsync(db);
        var migrator = db.GetService<IMigrator>();
        try
        {
            await migrator.MigrateAsync();
            var invoice = await SeedInvoiceAsync(db, status: "PAID");
            await db.Database.ExecuteSqlInterpolatedAsync($"UPDATE dbo.Invoice SET PaidAt={ProviderTime.UtcDateTime} WHERE InvoiceId={invoice.InvoiceId}");

            var blocked = Assert.ThrowsAsync<SqlException>(() => migrator.MigrateAsync(VnPayMigration));

            Assert.That(blocked!.Number, Is.EqualTo(51031));
            Assert.That(await db.Database.GetAppliedMigrationsAsync(), Does.Contain(InvoiceTimestampMigration));
            var stored = await db.Invoices.AsNoTracking().SingleAsync(row => row.InvoiceId == invoice.InvoiceId);
            Assert.That(stored.Status, Is.EqualTo("PAID"));
            Assert.That(stored.PaidAt, Is.EqualTo(ProviderTime.UtcDateTime));
            Assert.That(await ScalarAsync<string>(db,
                "SELECT OBJECT_DEFINITION(OBJECT_ID('dbo.usp_ApplyPaymentResult')) AS [Value]"),
                Does.Contain("UPDATE dbo.Invoice SET Status = 'PAID', PaidAt = @VerifiedPaidAtUtc"));
        }
        finally
        {
            // Dedicated disposable database: remove fixture evidence before exercising rollback.
            await db.Database.ExecuteSqlRawAsync("DELETE dbo.Invoice");
            await migrator.MigrateAsync(Migration.InitialDatabase);
        }
    }

    private static string MigrationConnection(string suffix)
    {
        var builder = new SqlConnectionStringBuilder(ConnectionString());
        builder.InitialCatalog += "_" + suffix;
        Assert.That(builder.InitialCatalog.Length, Is.LessThanOrEqualTo(128));
        return builder.ConnectionString;
    }

    private static async Task RequireEmptyDatabaseAsync(FrmsDbContext db)
    {
        if (!await db.Database.CanConnectAsync()) return;
        Assert.That(await ScalarAsync<int>(db,
            "SELECT COUNT(*) AS [Value] FROM sys.tables WHERE name <> '__EFMigrationsHistory'"),
            Is.Zero, "Migration tests require an empty dedicated database.");
        Assert.That(await db.Database.GetAppliedMigrationsAsync(), Is.Empty);
    }

    private static Task<T> ScalarAsync<T>(FrmsDbContext db, string sql) =>
        db.Database.SqlQueryRaw<T>(sql).SingleAsync();

    private static async Task<bool> ProcedureExistsAsync(FrmsDbContext db) =>
        await ScalarAsync<int>(db,
            "SELECT COUNT(*) AS [Value] FROM sys.procedures WHERE name = 'usp_ApplyPaymentResult'") == 1;
}
