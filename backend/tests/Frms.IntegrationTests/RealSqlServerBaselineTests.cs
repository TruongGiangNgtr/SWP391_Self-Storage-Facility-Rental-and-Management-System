using Frms.DataAccess.Persistence;
using Microsoft.Data.SqlClient;
using Microsoft.EntityFrameworkCore;

namespace Frms.IntegrationTests;

[TestFixture, NonParallelizable]
public sealed class RealSqlServerBaselineTests
{
    private FrmsDbContext database = null!;

    [SetUp]
    public async Task SetUp()
    {
        var connectionString = Environment.GetEnvironmentVariable("FRMS_TEST_CONNECTION_STRING");
        Assert.That(connectionString, Is.Not.Null.And.Not.Empty, "FRMS_TEST_CONNECTION_STRING is required for the SQL Server baseline gate.");
        Assert.That(new SqlConnectionStringBuilder(connectionString!).InitialCatalog, Does.StartWith("Frms_Test_"), "Only disposable Frms_Test_* databases may contain these fixtures.");
        database = new FrmsDbContext(new DbContextOptionsBuilder<FrmsDbContext>().UseSqlServer(connectionString).Options);
        await database.Database.MigrateAsync();
    }

    [TearDown]
    public async Task TearDown()
    {
        if (database is not null) await database.DisposeAsync();
    }

    [Test]
    public async Task DBT_PHASE0_001_DatabaseDefaults_ApplyFacilityUnitNotificationStatusAndUtcCreatedAt()
    {
        await using var transaction = await database.Database.BeginTransactionAsync();
        var facilityId = Guid.NewGuid();
        var unitTypeId = Guid.NewGuid();
        var unitId = Guid.NewGuid();
        var accountId = await InsertAccountAsync();
        var notificationId = Guid.NewGuid();
        var before = DateTime.UtcNow.AddSeconds(-1);
        await database.Database.ExecuteSqlInterpolatedAsync($"INSERT INTO Facility (FacilityId, Name, Address) VALUES ({facilityId}, N'Test default facility', N'Fixture only')");
        await database.Database.ExecuteSqlInterpolatedAsync($"INSERT INTO UnitType (UnitTypeId, Name, Mode, Size, RentalPrice) VALUES ({unitTypeId}, N'Test unit type', 'PUBLIC', N'Test size', {1m})");
        await database.Database.ExecuteSqlInterpolatedAsync($"INSERT INTO StorageUnit (StorageUnitId, FacilityId, UnitTypeId, UnitCode) VALUES ({unitId}, {facilityId}, {unitTypeId}, 'TEST-DEFAULT')");
        await database.Database.ExecuteSqlInterpolatedAsync($"INSERT INTO NotificationLog (NotificationLogId, UserAccountId, NotificationType, Content) VALUES ({notificationId}, {accountId}, 'TEST', N'Fixture only')");
        var facility = await database.Facilities.SingleAsync(row => row.FacilityId == facilityId);
        var unit = await database.StorageUnits.SingleAsync(row => row.StorageUnitId == unitId);
        var notification = await database.NotificationLogs.SingleAsync(row => row.NotificationLogId == notificationId);
        var policyId = Guid.NewGuid();
        await InsertPolicyAsync(policyId, 1, 31);
        var policy = await database.Policies.SingleAsync(row => row.PolicyId == policyId);
        Assert.Multiple(() =>
        {
            Assert.That(facility.Status, Is.EqualTo("INACTIVE"));
            Assert.That(unit.Status, Is.EqualTo("AVAILABLE"));
            Assert.That(notification.Status, Is.EqualTo("PENDING"));
            Assert.That(notification.SentAt, Is.Null);
            Assert.That(notification.CreatedAt, Is.InRange(before, DateTime.UtcNow.AddSeconds(1)));
            Assert.That(policy.CreatedAt, Is.InRange(before, DateTime.UtcNow.AddSeconds(1)));
        });
    }

    [TestCase(1, 31, true)]
    [TestCase(10, 10, true)]
    [TestCase(20, 19, false)]
    public async Task DBT_PHASE0_002_PolicyVisitDays_RejectReversedRangeAndAcceptEqualBoundary(int start, int end, bool accepted)
    {
        await using var transaction = await database.Database.BeginTransactionAsync();
        var id = Guid.NewGuid();
        if (accepted)
        {
            await InsertPolicyAsync(id, start, end);
            Assert.That(await database.Policies.AnyAsync(row => row.PolicyId == id), Is.True);
        }
        else
        {
            var exception = Assert.ThrowsAsync<SqlException>(async () => await InsertPolicyAsync(id, start, end));
            Assert.Multiple(() =>
            {
                Assert.That(exception!.Number, Is.EqualTo(547));
                Assert.That(exception.Message, Does.Contain("CK_Policy_Days"));
            });
        }
    }

    [TestCase("PENDING", false, true)]
    [TestCase("PENDING", true, false)]
    [TestCase("SENT", false, false)]
    [TestCase("SENT", true, true)]
    public async Task DBT_PHASE0_003_NotificationSentAt_MatchesDeliveryStatus(string status, bool hasSentAt, bool accepted)
    {
        await using var transaction = await database.Database.BeginTransactionAsync();
        var accountId = await InsertAccountAsync();
        var id = Guid.NewGuid();
        DateTime? sentAt = hasSentAt ? DateTime.UtcNow : null;
        async Task Insert() => await database.Database.ExecuteSqlInterpolatedAsync($"INSERT INTO NotificationLog (NotificationLogId, UserAccountId, NotificationType, Content, Status, SentAt) VALUES ({id}, {accountId}, 'TEST', N'Fixture only', {status}, {sentAt})");
        if (accepted)
        {
            await Insert();
            Assert.That(await database.NotificationLogs.AnyAsync(row => row.NotificationLogId == id), Is.True);
        }
        else
        {
            var exception = Assert.ThrowsAsync<SqlException>(async () => await Insert());
            Assert.Multiple(() =>
            {
                Assert.That(exception!.Number, Is.EqualTo(547));
                Assert.That(exception.Message, Does.Contain("CK_NotificationLog_SentAt"));
            });
        }
    }

    private Task<int> InsertPolicyAsync(Guid id, int start, int end) => database.Database.ExecuteSqlInterpolatedAsync($"INSERT INTO Policy (PolicyId, Version, Status, EffectiveFrom, DepositTimeoutHours, ReservationVisitStartDay, ReservationVisitEndDay, MonthlyPaymentDueDay, OverdueStartDay, LateFeeDivisorDays, EarlyReturnWaiveFeeUntilDay) SELECT {id}, MAX(Version)+1, 'INACTIVE', SYSUTCDATETIME(), 24, {start}, {end}, 5, 6, 30, 15 FROM Policy");

    private async Task<Guid> InsertAccountAsync()
    {
        var id = Guid.NewGuid();
        var email = $"constraint.{id:N}@example.invalid";
        var phone = "+84" + string.Concat(Enumerable.Range(0, 9).Select(_ => System.Security.Cryptography.RandomNumberGenerator.GetInt32(10)));
        var hash = BCrypt.Net.BCrypt.HashPassword(Guid.NewGuid().ToString("N"), 12);
        await database.Database.ExecuteSqlInterpolatedAsync($"INSERT INTO UserAccount (UserAccountId, RoleId, Email, PhoneNumber, PasswordHash) VALUES ({id}, {SeedIds.CustomerRole}, {email}, {phone}, {hash})");
        return id;
    }
}
