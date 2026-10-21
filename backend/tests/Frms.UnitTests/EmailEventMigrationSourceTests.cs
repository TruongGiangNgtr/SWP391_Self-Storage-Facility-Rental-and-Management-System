using NUnit.Framework;

namespace Frms.UnitTests;

// Pg014 SQL verification against the checked-in source; DB behavior needs integration tests.
[TestFixture]
public sealed class EmailEventMigrationSourceTests
{
    [Test]
    public void MigrationUsesOptInGateAndTransactionalQueueRoutine()
    {
        var sql = ReadSql();
        Assert.That(sql, Does.Contain("frms.email_events_enabled"));
        Assert.That(sql, Does.Contain("coalesce(current_setting('frms.email_events_enabled', true) = 'on', false)"));
        Assert.That(sql, Does.Contain("usp_QueueNotification"));
    }

    [TestCase("Reservation")]
    [TestCase("Invoice")]
    [TestCase("Visit")]
    [TestCase("Contract")]
    [TestCase("SupportTicket")]
    [TestCase("DepositSettlement")]
    public void SourceIncludesEachBusinessEventTable(string name)
    {
        Assert.That(ReadSql(), Does.Contain("ON public.\"" + name + "\""));
    }

    [Test]
    public void NoCredentialsOrPaymentUrlsInQueuedContent()
    {
        var sql = ReadSql();
        Assert.That(sql, Does.Not.Contain("PasswordHash"));
        Assert.That(sql, Does.Not.Contain("PaymentUrl"));
        Assert.That(sql, Does.Not.Contain("CCCD"));
    }

    private static string ReadSql()
    {
        var dir = new DirectoryInfo(AppContext.BaseDirectory);
        while (dir is not null)
        {
            var path = Path.Combine(dir.FullName, "backend", "sql", "postgresql", "EmailEventNotifications.sql");
            if (File.Exists(path)) return File.ReadAllText(path);
            dir = dir.Parent;
        }
        throw new FileNotFoundException("Run tests from the source checkout containing EmailEventNotifications.sql.");
    }
}
