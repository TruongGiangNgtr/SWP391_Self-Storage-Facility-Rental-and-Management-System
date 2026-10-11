using Frms.DataAccess.Persistence;
using Frms.DataAccess.Repositories.Implementations;
using Frms.DataAccess.Repositories.Models;
using Microsoft.Data.SqlClient;
using Microsoft.EntityFrameworkCore;
using static Frms.IntegrationTests.PaymentSqlTestSupport;

namespace Frms.IntegrationTests;

[TestFixture, NonParallelizable, Category("PayOsSql")]
public sealed class PayOsPersistenceTests
{
    [Test]
    public async Task DBT_PAYOS_FilteredUniqueOrderCodeAllowsLegacyNullsAndRejectsDuplicates()
    {
        await using var db = Open(Dedicated("Unique"));
        await Empty(db);
        try
        {
            await SqlScriptTestDatabase.EnsureAsync(db);
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
        finally { await SqlScriptTestDatabase.DropCreatedAsync(db); }
    }

    [Test]
    public async Task CON_PAYOS_ConcurrentDistinctAttemptsReceiveUniqueSequenceCodesAndSameKeyReusesCode()
    {
        await using var db = Open(Dedicated("Concurrency"));
        await Empty(db);
        try
        {
            await SqlScriptTestDatabase.EnsureAsync(db);
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
        finally { await SqlScriptTestDatabase.DropCreatedAsync(db); }
    }

    [Test]
    public async Task DAL_PAYOS_FractionalVndDoesNotConsumeKeyOrSequence()
    {
        await using var db = Open(Dedicated("Fraction"));
        await Empty(db);
        try
        {
            await SqlScriptTestDatabase.EnsureAsync(db);
            var fractional = await SeedInvoiceAsync(db, amount: 12500.50m);
            var result = await new PaymentRepository(db).CreateOrGetAsync(fractional.InvoiceId, Guid.NewGuid(), Now, default);
            Assert.That(result.Outcome, Is.EqualTo(PaymentAttemptOutcome.AmountUnsupported));
            Assert.That(await db.Payments.CountAsync(), Is.Zero);
            var eligible = await SeedInvoiceAsync(db);
            var accepted = await new PaymentRepository(db).CreateOrGetAsync(eligible.InvoiceId, Guid.NewGuid(), Now, default);
            Assert.That(accepted.Attempt!.ProviderOrderCode, Is.EqualTo(1000));
        }
        finally { await SqlScriptTestDatabase.DropCreatedAsync(db); }
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
    }
}
