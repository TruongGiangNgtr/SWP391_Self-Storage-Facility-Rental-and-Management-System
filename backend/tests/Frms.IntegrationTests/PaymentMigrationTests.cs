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
            await migrator.MigrateAsync(PaymentMigration);
            Assert.That(await db.Database.GetAppliedMigrationsAsync(), Does.Contain(PaymentMigration));
            Assert.That(await ProcedureExistsAsync(db), Is.True);
            Assert.That(await db.Payments.CountAsync(), Is.Zero);
            Assert.That(await ScalarAsync<int>(db, "SELECT COUNT(*) AS [Value] FROM sys.tables"), Is.EqualTo(28)); // 27 entities + migration history
            Assert.That(await ScalarAsync<int>(db, """
                SELECT COUNT(*) AS [Value] FROM sys.default_constraints d
                JOIN sys.columns c ON c.object_id = d.parent_object_id AND c.column_id = d.parent_column_id
                WHERE d.parent_object_id = OBJECT_ID('dbo.Payment') AND c.name = 'IdempotencyKey'
                """), Is.Zero);

            await migrator.MigrateAsync(BaselineMigration);
            Assert.That(await ProcedureExistsAsync(db), Is.False);
            Assert.That(await ScalarAsync<bool>(db, "SELECT is_nullable AS [Value] FROM sys.columns WHERE object_id = OBJECT_ID('dbo.Payment') AND name = 'InvoiceId'"), Is.True);
            await migrator.MigrateAsync(PaymentMigration);
            Assert.That(await ProcedureExistsAsync(db), Is.True);
            Assert.That(await ScalarAsync<bool>(db, "SELECT is_nullable AS [Value] FROM sys.columns WHERE object_id = OBJECT_ID('dbo.Payment') AND name = 'InvoiceId'"), Is.False);
        }
        finally
        {
            // These dedicated migration databases must be empty before the fixture starts.
            // Drop only the test schema created by this fixture; never delete a database.
            await migrator.MigrateAsync(Migration.InitialDatabase);
        }
    }

    [TestCase("NullGuard", true, 51001)]
    [TestCase("KeyGuard", false, 51002)]
    public async Task DBT_PAY_M02_UnexpectedExistingRowsAbortUpgradeWithoutFabrication(string suffix, bool unlinked, int diagnostic)
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
            var exception = Assert.ThrowsAsync<SqlException>(async () => await migrator.MigrateAsync(PaymentMigration));
            Assert.Multiple(() =>
            {
                Assert.That(exception!.Number, Is.EqualTo(diagnostic));
                Assert.That(exception.Message, Does.Contain("Payment migration blocked"));
            });
            Assert.That(await db.Database.GetAppliedMigrationsAsync(), Does.Not.Contain(PaymentMigration));
            Assert.That(await db.Database.SqlQuery<Guid>($"SELECT PaymentId AS [Value] FROM dbo.Payment WHERE PaymentId = {paymentId}").SingleAsync(), Is.EqualTo(paymentId));
            Assert.That(await ScalarAsync<int>(db, "SELECT COUNT(*) AS [Value] FROM sys.columns WHERE object_id = OBJECT_ID('dbo.Payment') AND name = 'IdempotencyKey'"), Is.Zero);
        }
        finally
        {
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
        Assert.That(await ScalarAsync<int>(db, "SELECT COUNT(*) AS [Value] FROM sys.tables WHERE name <> '__EFMigrationsHistory'"),
            Is.Zero, "Migration tests require an empty dedicated database; existing data must not be overwritten.");
        Assert.That(await db.Database.GetAppliedMigrationsAsync(), Is.Empty);
    }

    private static Task<T> ScalarAsync<T>(FrmsDbContext db, string sql) => db.Database.SqlQueryRaw<T>(sql).SingleAsync();
    private static async Task<bool> ProcedureExistsAsync(FrmsDbContext db) =>
        await ScalarAsync<int>(db, "SELECT COUNT(*) AS [Value] FROM sys.procedures WHERE name = 'usp_ApplyPaymentResult'") == 1;
}
