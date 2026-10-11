//using Frms.DataAccess.DependencyInjection;
//using Frms.DataAccess.Persistence;
//using Frms.DataAccess.Persistence.Entities;
//using Frms.DataAccess.Repositories.Interfaces;
//using Frms.DataAccess.StoredProcedures;
//using Microsoft.Data.SqlClient;
//using Microsoft.EntityFrameworkCore;
//using Microsoft.EntityFrameworkCore.Migrations.Operations;
//using Frms.DataAccess.Migrations;
//using Microsoft.Extensions.DependencyInjection;

//namespace Frms.IntegrationTests;

//[TestFixture, Category("ReservationCancellationSql"), NonParallelizable]
//public sealed class ReservationCancellationSqlTests
//{
//    private string connectionString = null!;
//    private FrmsDbContext database = null!;
//    private Fixture? fixture;
//    private Guid? extraFacilityId;

//    [OneTimeSetUp]
//    public async Task PrepareApprovedTestDatabase()
//    {
//        var configured = Environment.GetEnvironmentVariable("FRMS_TEST_CONNECTION_STRING");
//        Assert.That(configured, Is.Not.Null.And.Not.Empty,
//            "FRMS_TEST_CONNECTION_STRING is required; no SQL Server database is inferred.");
//        Assert.That(new SqlConnectionStringBuilder(configured!).InitialCatalog, Does.StartWith("Frms_Test_"),
//            "Only an approved disposable Frms_Test_* database may be used.");
//        connectionString = configured!;
//        await using var context = Context();
//        await SqlScriptTestDatabase.EnsureAsync(context);
//    }

//    [SetUp]
//    public void SetUp() => database = Context();

//    [TearDown]
//    public async Task TearDown()
//    {
//        if (database is null) return;
//        if (fixture is { } f)
//        {
//            // Only uniquely identified fixture rows in the guarded disposable DB.
//            await database.Database.ExecuteSqlInterpolatedAsync($"DELETE FROM Visit WHERE VisitId = {f.VisitId} OR EntityId IN (SELECT ContractId FROM Contract WHERE ReservationId = {f.ReservationId})");
//            await database.Database.ExecuteSqlInterpolatedAsync($"DELETE FROM Contract WHERE ReservationId = {f.ReservationId}");
//            await database.Database.ExecuteSqlInterpolatedAsync($"DELETE FROM Invoice WHERE EntityId = {f.ReservationId}");
//            await database.Database.ExecuteSqlInterpolatedAsync($"DELETE FROM Visit WHERE EntityId = {f.ReservationId}");
//            await database.Database.ExecuteSqlInterpolatedAsync($"DELETE FROM Reservation WHERE ReservationId = {f.ReservationId}");
//            await database.Database.ExecuteSqlInterpolatedAsync($"DELETE FROM StorageUnit WHERE StorageUnitId = {f.UnitId}");
//            await database.Database.ExecuteSqlInterpolatedAsync($"DELETE FROM Employee WHERE EmployeeId = {f.EmployeeId}");
//            await database.Database.ExecuteSqlInterpolatedAsync($"DELETE FROM Customer WHERE CustomerId = {f.CustomerId}");
//            await database.Database.ExecuteSqlInterpolatedAsync($"DELETE FROM UserAccount WHERE UserAccountId IN ({f.AccountId}, {f.StaffAccountId})");
//            await database.Database.ExecuteSqlInterpolatedAsync($"DELETE FROM UnitType WHERE UnitTypeId = {f.UnitTypeId}");
//            await database.Database.ExecuteSqlInterpolatedAsync($"DELETE FROM Facility WHERE FacilityId = {f.FacilityId}");
//            await database.Database.ExecuteSqlInterpolatedAsync($"DELETE FROM Policy WHERE PolicyId = {f.PolicyId}");
//            fixture = null;
//        }
//        if (extraFacilityId is { } otherFacility)
//        {
//            await database.Database.ExecuteSqlInterpolatedAsync($"DELETE FROM Facility WHERE FacilityId = {otherFacility}");
//            extraFacilityId = null;
//        }
//        await database.DisposeAsync();
//    }

//    [TestCase("PENDING_DEPOSIT", "UNPAID", false)]
//    [TestCase("CONFIRMED", "PAID", true)]
//    public async Task DBT_RES005_001_CancelReleasesOnlyHoldAndScheduledVisitAndRetryDoesNotDoubleRelease(
//        string status, string invoiceStatus, bool hasVisit)
//    {
//        var f = await Seed(status, invoiceStatus, hasVisit ? "SCHEDULED" : null);
//        var before = await AvailableCapacity(f);
//        var first = await Cancel(f);
//        var second = await Cancel(f);
//        var after = await AvailableCapacity(f);
//        var invoice = await database.Invoices.AsNoTracking().SingleAsync(row => row.EntityId == f.ReservationId);
//        Assert.Multiple(() =>
//        {
//            Assert.That(first.Status, Is.EqualTo("CANCELLED"));
//            Assert.That(second.Status, Is.EqualTo("CANCELLED"));
//            Assert.That(first.ReservationVisit?.Status, Is.EqualTo(hasVisit ? "CANCELLED" : null));
//            Assert.That(second, Is.EqualTo(first));
//            Assert.That(before, Is.Zero);
//            Assert.That(after, Is.EqualTo(1));
//            Assert.That(invoice.Status, Is.EqualTo(invoiceStatus));
//            Assert.That(invoice.PaidAt, Is.EqualTo(invoiceStatus == "PAID" ? f.RecordedAt : (DateTime?)null));
//            Assert.That(invoice.BaseAmount, Is.EqualTo(120000.25m));
//            Assert.That(invoice.AmountDue, Is.EqualTo(120000.25m));
//            Assert.That(invoice.DiscountAmount, Is.Zero);
//        });
//        Assert.That(await database.StorageUnits.Where(row => row.StorageUnitId == f.UnitId).Select(row => row.Status).SingleAsync(), Is.EqualTo("AVAILABLE"));
//        Assert.That(await database.Contracts.AnyAsync(row => row.ReservationId == f.ReservationId), Is.False);
//        Assert.That(await database.Payments.AnyAsync(row => row.InvoiceId == invoice.InvoiceId), Is.False);
//    }

//    [TestCase("CHECKED_IN")]
//    [TestCase("CHECKED_OUT")]
//    public async Task DBT_RES005_002_CheckedInOrOutRejectsWithoutPartialChanges(string visitStatus)
//    {
//        var f = await Seed("CONFIRMED", "PAID", visitStatus);
//        var error = Assert.ThrowsAsync<StoredProcedureBusinessException>(() => Cancel(f));
//        Assert.That(error!.Code, Is.EqualTo("VISIT_INVALID_STATUS"));
//        await AssertStates(f, "CONFIRMED", visitStatus);
//    }

//    [Test]
//    public async Task DBT_RES005_003_CompletedReservationCannotBeCancelled()
//    {
//        var f = await Seed("COMPLETED", "PAID", "CHECKED_OUT");
//        var error = Assert.ThrowsAsync<StoredProcedureBusinessException>(() => Cancel(f));
//        Assert.That(error!.Code, Is.EqualTo("RESERVATION_INVALID_STATUS"));
//        await AssertStates(f, "COMPLETED", "CHECKED_OUT");
//    }

//    [Test]
//    public async Task DBT_RES005_004_ExistingContractBlocksCancellationEvenWithStaleReservationState()
//    {
//        var f = await Seed("CONFIRMED", "PAID", "CANCELLED");
//        database.Contracts.Add(new Contract
//        {
//            ContractId = Guid.NewGuid(), ReservationId = f.ReservationId, CustomerId = f.CustomerId,
//            FacilityId = f.FacilityId, StorageUnitId = f.UnitId, PolicyId = f.PolicyId,
//            StartMonth = f.StartMonth, EndMonth = f.StartMonth.AddMonths(2), Status = "ACTIVE"
//        });
//        await database.SaveChangesAsync();
//        var error = Assert.ThrowsAsync<StoredProcedureBusinessException>(() => Cancel(f));
//        Assert.That(error!.Code, Is.EqualTo("RESERVATION_INVALID_STATUS"));
//        await AssertStates(f, "CONFIRMED", "CANCELLED");
//        Assert.That(await database.Contracts.CountAsync(row => row.ReservationId == f.ReservationId), Is.EqualTo(1));
//    }

//    [TestCase("owner", "RESOURCE_NOT_FOUND")]
//    [TestCase("account", "RESOURCE_NOT_FOUND")]
//    [TestCase("missing", "RESOURCE_NOT_FOUND")]
//    [TestCase("inactive", "ACCOUNT_INACTIVE")]
//    [TestCase("role", "FORBIDDEN")]
//    public async Task SEC_RES005_005_ProcedureRechecksOwnershipAndActiveRole(string partition, string code)
//    {
//        var f = await Seed("CONFIRMED", "PAID", "SCHEDULED");
//        if (partition == "inactive")
//            await database.Database.ExecuteSqlInterpolatedAsync($"UPDATE UserAccount SET Status = 'INACTIVE' WHERE UserAccountId = {f.AccountId}");
//        if (partition == "role")
//            await database.Database.ExecuteSqlInterpolatedAsync($"UPDATE UserAccount SET RoleId = {SeedIds.SystemAdministratorRole} WHERE UserAccountId = {f.AccountId}");
//        var error = Assert.ThrowsAsync<StoredProcedureBusinessException>(() => Cancel(f,
//            partition == "account" ? Guid.NewGuid() : f.AccountId,
//            partition == "owner" ? Guid.NewGuid() : f.CustomerId,
//            partition == "missing" ? Guid.NewGuid() : f.ReservationId));
//        Assert.That(error!.Code, Is.EqualTo(code));
//        await AssertStates(f, "CONFIRMED", "SCHEDULED");
//    }

//    [Test]
//    public async Task DBT_RES005_006_WriteFailureRollsBackBothReservationAndVisit()
//    {
//        var f = await Seed("CONFIRMED", "PAID", "SCHEDULED");
//        var trigger = "TR_ReservationCancelFixture_" + f.ReservationId.ToString("N");
//        // Identifier and GUID are generated test constants, never user input.
//        var sql = $"CREATE TRIGGER dbo.[{trigger}] ON dbo.Reservation AFTER UPDATE AS BEGIN IF EXISTS (SELECT 1 FROM inserted WHERE ReservationId = '{f.ReservationId:D}' AND Status = 'CANCELLED') THROW 51290, 'Fixture write failure', 1; END;";
//        await database.Database.ExecuteSqlRawAsync(sql);
//        try
//        {
//            var error = Assert.ThrowsAsync<SqlException>(() => Cancel(f));
//            Assert.That(error!.Number, Is.EqualTo(51290));
//            await AssertStates(f, "CONFIRMED", "SCHEDULED");
//        }
//        finally {
//            var dropSql = "DROP TRIGGER dbo.[" + trigger + "];";
//            await database.Database.ExecuteSqlRawAsync(dropSql);
//        }
//    }

//    [Test]
//    public async Task CON_RES005_007_ConcurrentCancellationIsIdempotent()
//    {
//        var f = await Seed("CONFIRMED", "PAID", "SCHEDULED");
//        var results = await Task.WhenAll(Enumerable.Range(0, 6).Select(_ => Cancel(f)));
//        Assert.That(results.Select(row => row.Status), Is.All.EqualTo("CANCELLED"));
//        await AssertStates(f, "CANCELLED", "CANCELLED");
//        Assert.That(await AvailableCapacity(f), Is.EqualTo(1));
//    }

//    [Test]
//    public async Task CON_RES005_008_CancelVersusRealConfirmDoesNotReviveHold()
//    {
//        await RequireProcedure("usp_ConfirmReservation");
//        var f = await Seed("PENDING_DEPOSIT", "PAID", null);
//        using var scope = RepositoryScope();
//        var repository = scope.ServiceProvider.GetRequiredService<IReservationRepository>();
//        var confirm = Observe(() => repository.ConfirmAsync(f.CustomerId, f.ReservationId, f.StartMonth, default));
//        var cancel = Observe(() => Cancel(f));
//        var outcomes = await Task.WhenAll(confirm, cancel);
//        Assert.That(outcomes[1], Is.Null, "Cancellation remains eligible if confirm wins first.");
//        if (outcomes[0] is { } error) AssertConflict(error, "RESERVATION_INVALID_STATUS");
//        Assert.That(await database.Reservations.Where(row => row.ReservationId == f.ReservationId).Select(row => row.Status).SingleAsync(), Is.EqualTo("CANCELLED"));
//        var visits = await database.Visits.Where(row => row.EntityId == f.ReservationId).Select(row => row.Status).ToArrayAsync();
//        Assert.That(visits, Is.All.EqualTo("CANCELLED"));
//        Assert.That(visits.Length, Is.LessThanOrEqualTo(1));
//        Assert.That(await AvailableCapacity(f), Is.EqualTo(1));
//    }

//    [Test]
//    public async Task CON_RES005_009_CancelVersusRealCheckInHasOneLegalWinner()
//    {
//        await RequireProcedure("usp_CheckInVisit");
//        var f = await Seed("CONFIRMED", "PAID", "SCHEDULED", todayVisit: true);
//        using var scope = RepositoryScope();
//        var visits = scope.ServiceProvider.GetRequiredService<IVisitRepository>();
//        var outcomes = await Task.WhenAll(Observe(() => Cancel(f)),
//            Observe(() => visits.CheckInAsync(f.VisitId, f.EmployeeId, default)));
//        Assert.That(outcomes.Count(error => error is null), Is.EqualTo(1));
//        AssertConflict(outcomes.Single(error => error is not null)!, "VISIT_INVALID_STATUS");
//        if (outcomes[0] is null) await AssertStates(f, "CANCELLED", "CANCELLED");
//        else await AssertStates(f, "CONFIRMED", "CHECKED_IN");
//        Assert.That(await database.Contracts.AnyAsync(row => row.ReservationId == f.ReservationId), Is.False);
//    }

//    [Test]
//    public async Task CON_RES005_010_CancelCannotRacePastRealCheckedInHandover()
//    {
//        await RequireProcedure("usp_CompleteHandover");
//        var f = await Seed("CONFIRMED", "PAID", "CHECKED_IN", todayVisit: true);
//        using var scope = RepositoryScope();
//        var handover = scope.ServiceProvider.GetRequiredService<IHandoverRepository>();
//        var outcomes = await Task.WhenAll(Observe(() => Cancel(f)),
//            Observe(() => handover.CompleteAsync(f.ReservationId, f.VisitId, f.UnitId, f.EmployeeId, null, default)));
//        Assert.That(outcomes[0], Is.TypeOf<StoredProcedureBusinessException>());
//        AssertConflict(outcomes[0]!, "VISIT_INVALID_STATUS", "RESERVATION_INVALID_STATUS");
//        Assert.That(outcomes[1], Is.Null);
//        await AssertStates(f, "COMPLETED", "CHECKED_OUT");
//        Assert.That(await database.Contracts.CountAsync(row => row.ReservationId == f.ReservationId), Is.EqualTo(1));
//        Assert.That(await database.StorageUnits.Where(row => row.StorageUnitId == f.UnitId).Select(row => row.Status).SingleAsync(), Is.EqualTo("IN_USE"));
//    }

//    [Test]
//    public async Task DBT_RES004_001_ConfirmUsesCapturedPolicyAndCreatesOneVisit()
//    {
//        var f = await Seed("PENDING_DEPOSIT", "PAID", null);
//        var first = await ConfirmProcedure(f.ReservationId, f.StartMonth);
//        // A retry cannot silently change the saved schedule, even with a new date.
//        var retry = await ConfirmProcedure(f.ReservationId, f.StartMonth.AddDays(1));
//        var visit = await database.Visits.AsNoTracking().SingleAsync(row => row.EntityId == f.ReservationId);
//        Assert.Multiple(() =>
//        {
//            Assert.That(first.Status, Is.EqualTo("CONFIRMED"));
//            Assert.That(retry, Is.EqualTo(first));
//            Assert.That(visit.VisitId, Is.EqualTo(first.VisitId));
//            Assert.That(visit.VisitType, Is.EqualTo("RESERVATION"));
//            Assert.That(visit.VisitDate, Is.EqualTo(f.StartMonth));
//            Assert.That(visit.Status, Is.EqualTo("SCHEDULED"));
//            Assert.That(visit.EmployeeId, Is.Null);
//        });
//        var reservation = await database.Reservations.AsNoTracking().SingleAsync(row => row.ReservationId == f.ReservationId);
//        Assert.That(reservation.Status, Is.EqualTo("CONFIRMED"));
//        Assert.That(reservation.LockedRentalPrice, Is.EqualTo(120000.25m));
//        Assert.That(await AvailableCapacity(f), Is.Zero);
//        Assert.That(await database.Contracts.AnyAsync(row => row.ReservationId == f.ReservationId), Is.False);
//        Assert.That(await database.StorageUnits.Where(row => row.StorageUnitId == f.UnitId).Select(row => row.Status).SingleAsync(), Is.EqualTo("AVAILABLE"));
//        Assert.That(await database.Invoices.Where(row => row.EntityId == f.ReservationId).Select(row => row.Status).SingleAsync(), Is.EqualTo("PAID"));
//    }

//    [TestCase("UNPAID")]
//    [TestCase("CANCELLED")]
//    public async Task DBT_RES004_002_UnpaidDepositCannotConfirm(string invoiceStatus)
//    {
//        var f = await Seed("PENDING_DEPOSIT", invoiceStatus, null);
//        var error = Assert.ThrowsAsync<SqlException>(() => ConfirmProcedure(f.ReservationId, f.StartMonth));
//        Assert.That(error!.Number, Is.EqualTo(51108));
//        Assert.That(await database.Reservations.Where(row => row.ReservationId == f.ReservationId).Select(row => row.Status).SingleAsync(), Is.EqualTo("PENDING_DEPOSIT"));
//        Assert.That(await database.Visits.AnyAsync(row => row.EntityId == f.ReservationId), Is.False);
//    }

//    [TestCase(1, false)]
//    [TestCase(2, true)]
//    [TestCase(5, true)]
//    [TestCase(6, false)]
//    [TestCase(0, false)]
//    [TestCase(33, false)]
//    public async Task DBT_RES004_003_CapturedWindowEnforcesBothBoundariesAndMonth(int day, bool allowed)
//    {
//        var f = await Seed("PENDING_DEPOSIT", "PAID", null);
//        await database.Database.ExecuteSqlInterpolatedAsync($"UPDATE Policy SET ReservationVisitStartDay = 2, ReservationVisitEndDay = 5 WHERE PolicyId = {f.PolicyId}");
//        var date = f.StartMonth.AddDays(day - 1);
//        if (allowed)
//        {
//            var confirmed = await ConfirmProcedure(f.ReservationId, date);
//            Assert.That(confirmed.Status, Is.EqualTo("CONFIRMED"));
//            Assert.That(await database.Visits.Where(row => row.VisitId == confirmed.VisitId).Select(row => row.VisitDate).SingleAsync(), Is.EqualTo(date));
//        }
//        else
//        {
//            var error = Assert.ThrowsAsync<SqlException>(() => ConfirmProcedure(f.ReservationId, date));
//            Assert.That(error!.Number, Is.EqualTo(51109));
//            Assert.That(await database.Reservations.Where(row => row.ReservationId == f.ReservationId).Select(row => row.Status).SingleAsync(), Is.EqualTo("PENDING_DEPOSIT"));
//            Assert.That(await database.Visits.AnyAsync(row => row.EntityId == f.ReservationId), Is.False);
//        }
//    }

//    [TestCase(2027, 28)]
//    [TestCase(2028, 29)]
//    public async Task DBT_RES004_004_ShortMonthClampsCapturedDays(int year, int lastDay)
//    {
//        var f = await Seed("PENDING_DEPOSIT", "PAID", null, startMonth: new DateOnly(year, 2, 1));
//        await database.Database.ExecuteSqlInterpolatedAsync($"UPDATE Policy SET ReservationVisitStartDay = 30, ReservationVisitEndDay = 31 WHERE PolicyId = {f.PolicyId}");
//        var date = new DateOnly(year, 2, lastDay);
//        var result = await ConfirmProcedure(f.ReservationId, date);
//        Assert.That(await database.Visits.Where(row => row.VisitId == result.VisitId).Select(row => row.VisitDate).SingleAsync(), Is.EqualTo(date));
//    }

//    [TestCase("CANCELLED")]
//    [TestCase("COMPLETED")]
//    public async Task DBT_RES004_005_TerminalReservationCannotBeRevived(string status)
//    {
//        var f = await Seed(status, "PAID", null);
//        var error = Assert.ThrowsAsync<SqlException>(() => ConfirmProcedure(f.ReservationId, f.StartMonth));
//        Assert.That(error!.Number, Is.EqualTo(51107));
//        Assert.That(await database.Reservations.Where(row => row.ReservationId == f.ReservationId).Select(row => row.Status).SingleAsync(), Is.EqualTo(status));
//        Assert.That(await database.Visits.AnyAsync(row => row.EntityId == f.ReservationId), Is.False);
//    }

//    [Test]
//    public async Task CON_RES004_006_ConcurrentConfirmCreatesExactlyOneVisit()
//    {
//        var f = await Seed("PENDING_DEPOSIT", "PAID", null);
//        var results = await Task.WhenAll(Enumerable.Range(0, 6).Select(_ => ConfirmProcedure(f.ReservationId, f.StartMonth)));
//        Assert.That(results.Select(row => row.VisitId).Distinct().Count(), Is.EqualTo(1));
//        Assert.That(results.Select(row => row.Status), Is.All.EqualTo("CONFIRMED"));
//        Assert.That(await database.Visits.CountAsync(row => row.EntityId == f.ReservationId), Is.EqualTo(1));
//        Assert.That(await AvailableCapacity(f), Is.Zero);
//    }

//    [Test]
//    public async Task DBT_RES004_007_ConfirmWriteFailureRollsBackVisitAndReservation()
//    {
//        var f = await Seed("PENDING_DEPOSIT", "PAID", null);
//        var trigger = "TR_ConfirmFixture_" + f.ReservationId.ToString("N");
//        var sql = $"CREATE TRIGGER dbo.[{trigger}] ON dbo.Reservation AFTER UPDATE AS BEGIN IF EXISTS (SELECT 1 FROM inserted WHERE ReservationId = '{f.ReservationId:D}' AND Status = 'CONFIRMED') THROW 51390, 'Fixture write failure', 1; END;";
//        await database.Database.ExecuteSqlRawAsync(sql);
//        try
//        {
//            var error = Assert.ThrowsAsync<SqlException>(() => ConfirmProcedure(f.ReservationId, f.StartMonth));
//            Assert.That(error!.Number, Is.EqualTo(51390));
//            Assert.That(await database.Reservations.Where(row => row.ReservationId == f.ReservationId).Select(row => row.Status).SingleAsync(), Is.EqualTo("PENDING_DEPOSIT"));
//            Assert.That(await database.Visits.AnyAsync(row => row.EntityId == f.ReservationId), Is.False);
//        }
//        finally
//        {
//            var drop = "DROP TRIGGER dbo.[" + trigger + "];";
//            await database.Database.ExecuteSqlRawAsync(drop);
//        }
//    }

//    [TestCase("RESERVATION")]
//    [TestCase("ACCESS")]
//    [TestCase("RETURN")]
//    public async Task DBT_OPS002_001_CheckInTodayAssignsActualStaffOnly(string type)
//    {
//        var f = await Seed("CONFIRMED", "PAID", "SCHEDULED", todayVisit: true);
//        await PrepareVisitType(f, type);
//        using var scope = RepositoryScope();
//        var result = await scope.ServiceProvider.GetRequiredService<IVisitRepository>().CheckInAsync(f.VisitId, f.EmployeeId, default);
//        Assert.Multiple(() =>
//        {
//            Assert.That(result.Status, Is.EqualTo("CHECKED_IN"));
//            Assert.That(result.EmployeeId, Is.EqualTo(f.EmployeeId));
//            Assert.That(result.VisitType, Is.EqualTo(type));
//            Assert.That(result.ActualReturnDate, Is.Null);
//        });
//        Assert.That(await database.Reservations.Where(row => row.ReservationId == f.ReservationId).Select(row => row.Status).SingleAsync(), Is.EqualTo("CONFIRMED"));
//        Assert.That(await database.StorageUnits.Where(row => row.StorageUnitId == f.UnitId).Select(row => row.Status).SingleAsync(), Is.EqualTo("AVAILABLE"));
//        Assert.That(await database.Invoices.Where(row => row.EntityId == f.ReservationId).Select(row => row.Status).SingleAsync(), Is.EqualTo("PAID"));
//    }

//    [TestCase("RESERVATION", -1)]
//    [TestCase("RESERVATION", 1)]
//    [TestCase("ACCESS", -1)]
//    [TestCase("ACCESS", 1)]
//    [TestCase("RETURN", -1)]
//    [TestCase("RETURN", 1)]
//    public async Task DBT_OPS002_002_CheckInRejectsEarlyOrLateDateWithoutAssigningActor(string type, int offset)
//    {
//        var f = await Seed("CONFIRMED", "PAID", "SCHEDULED", todayVisit: true);
//        await PrepareVisitType(f, type);
//        // Offset from SQL Server's business date, not a client-provided clock.
//        var serverToday = await database.Database.SqlQuery<DateTime>($"SELECT CONVERT(date, DATEADD(hour, 7, SYSUTCDATETIME())) AS [Value]").SingleAsync();
//        var date = DateOnly.FromDateTime(serverToday).AddDays(offset);
//        await database.Database.ExecuteSqlInterpolatedAsync($"UPDATE Visit SET VisitDate = {date} WHERE VisitId = {f.VisitId}");
//        await AssertCheckInRejected(f, "VISIT_INVALID_STATUS");
//    }

//    [TestCase("inactive")]
//    [TestCase("role")]
//    [TestCase("facility")]
//    [TestCase("missing")]
//    public async Task SEC_OPS002_003_CheckInRechecksActiveStaffAndFacility(string partition)
//    {
//        var f = await Seed("CONFIRMED", "PAID", "SCHEDULED", todayVisit: true);
//        if (partition == "inactive")
//            await database.Database.ExecuteSqlInterpolatedAsync($"UPDATE UserAccount SET Status = 'INACTIVE' WHERE UserAccountId = {f.StaffAccountId}");
//        if (partition == "role")
//            await database.Database.ExecuteSqlInterpolatedAsync($"UPDATE UserAccount SET RoleId = {SeedIds.FacilityManagerRole} WHERE UserAccountId = {f.StaffAccountId}");
//        if (partition == "facility")
//        {
//            extraFacilityId = Guid.NewGuid();
//            database.Facilities.Add(new Facility { FacilityId = extraFacilityId.Value, Name = "Other fixture facility", Address = "Test only", Status = "ACTIVE" });
//            await database.SaveChangesAsync();
//            await database.Database.ExecuteSqlInterpolatedAsync($"UPDATE Employee SET FacilityId = {extraFacilityId.Value} WHERE EmployeeId = {f.EmployeeId}");
//        }
//        await AssertCheckInRejected(f, "FORBIDDEN", partition == "missing" ? Guid.NewGuid() : null);
//    }

//    [TestCase("RESERVATION", "SCHEDULED", "CANCELLED")]
//    [TestCase("RESERVATION", "CHECKED_OUT", "CONFIRMED")]
//    [TestCase("RESERVATION", "CANCELLED", "CONFIRMED")]
//    [TestCase("ACCESS", "SCHEDULED", "COMPLETED")]
//    [TestCase("RETURN", "SCHEDULED", "TERMINATED")]
//    public async Task DBT_OPS002_004_CheckInRejectsInvalidVisitOrParentState(string type, string visitStatus, string parentStatus)
//    {
//        var f = await Seed(type == "RESERVATION" ? parentStatus : "CONFIRMED", "PAID", visitStatus, todayVisit: true);
//        await PrepareVisitType(f, type, parentStatus);
//        using var scope = RepositoryScope();
//        var error = Assert.ThrowsAsync<StoredProcedureBusinessException>(() => scope.ServiceProvider.GetRequiredService<IVisitRepository>().CheckInAsync(f.VisitId, f.EmployeeId, default));
//        Assert.That(error!.Code, Is.EqualTo("VISIT_INVALID_STATUS"));
//        var visit = await database.Visits.AsNoTracking().SingleAsync(row => row.VisitId == f.VisitId);
//        Assert.That(visit.Status, Is.EqualTo(visitStatus));
//        Assert.That(visit.EmployeeId, Is.Null);
//    }

//    [Test]
//    public async Task DBT_OPS002_005_OrphanVisitCannotAssignActor()
//    {
//        var f = await Seed("CONFIRMED", "PAID", "SCHEDULED", todayVisit: true);
//        await database.Database.ExecuteSqlInterpolatedAsync($"UPDATE Visit SET EntityId = {Guid.NewGuid()} WHERE VisitId = {f.VisitId}");
//        await AssertCheckInRejected(f, "VISIT_ENTITY_MISMATCH");
//    }

//    [Test]
//    public async Task DBT_OPS002_007_CheckInWriteFailureRollsBackStateAndActor()
//    {
//        var f = await Seed("CONFIRMED", "PAID", "SCHEDULED", todayVisit: true);
//        var trigger = "TR_CheckInFixture_" + f.VisitId.ToString("N");
//        var sql = $"CREATE TRIGGER dbo.[{trigger}] ON dbo.Visit AFTER UPDATE AS BEGIN IF EXISTS (SELECT 1 FROM inserted WHERE VisitId = '{f.VisitId:D}' AND Status = 'CHECKED_IN') THROW 51391, 'Fixture write failure', 1; END;";
//        await database.Database.ExecuteSqlRawAsync(sql);
//        try
//        {
//            using var scope = RepositoryScope();
//            var error = Assert.ThrowsAsync<SqlException>(() => scope.ServiceProvider.GetRequiredService<IVisitRepository>().CheckInAsync(f.VisitId, f.EmployeeId, default));
//            Assert.That(error!.Number, Is.EqualTo(51391));
//            var visit = await database.Visits.AsNoTracking().SingleAsync(row => row.VisitId == f.VisitId);
//            Assert.That(visit.Status, Is.EqualTo("SCHEDULED"));
//            Assert.That(visit.EmployeeId, Is.Null);
//        }
//        finally
//        {
//            var drop = "DROP TRIGGER dbo.[" + trigger + "];";
//            await database.Database.ExecuteSqlRawAsync(drop);
//        }
//    }

//    [Test]
//    public async Task DBT_RES004_008_ExistingVisitCannotBeReplacedDuringConfirmation()
//    {
//        var f = await Seed("PENDING_DEPOSIT", "PAID", "CANCELLED");
//        var error = Assert.ThrowsAsync<SqlException>(() => ConfirmProcedure(f.ReservationId, f.StartMonth));
//        Assert.That(error!.Number, Is.EqualTo(51110));
//        await AssertStates(f, "PENDING_DEPOSIT", "CANCELLED");
//        Assert.That(await database.Visits.CountAsync(row => row.EntityId == f.ReservationId), Is.EqualTo(1));
//    }

//    [Test]
//    public async Task CON_OPS002_006_ConcurrentCheckInHasOneWinnerAndRetryPreservesActor()
//    {
//        var f = await Seed("CONFIRMED", "PAID", "SCHEDULED", todayVisit: true);
//        async Task CheckIn()
//        {
//            await using var scope = RepositoryScope();
//            await scope.ServiceProvider.GetRequiredService<IVisitRepository>().CheckInAsync(f.VisitId, f.EmployeeId, default);
//        }
//        var outcomes = await Task.WhenAll(Enumerable.Range(0, 4).Select(_ => Observe(CheckIn)));
//        Assert.That(outcomes.Count(error => error is null), Is.EqualTo(1));
//        foreach (var error in outcomes.Where(error => error is not null)) AssertConflict(error!, "VISIT_INVALID_STATUS");
//        var retry = Assert.ThrowsAsync<StoredProcedureBusinessException>(CheckIn);
//        Assert.That(retry!.Code, Is.EqualTo("VISIT_INVALID_STATUS"));
//        var visit = await database.Visits.AsNoTracking().SingleAsync(row => row.VisitId == f.VisitId);
//        Assert.That(visit.Status, Is.EqualTo("CHECKED_IN"));
//        Assert.That(visit.EmployeeId, Is.EqualTo(f.EmployeeId));
//    }

//    [Test]
//    public async Task DBT_RES005_011_ForwardAlignmentReplacesLegacySignatureWithoutChangingHistory()
//    {
//        var f = await Seed("CONFIRMED", "PAID", "SCHEDULED");
//        var historyCount = await database.Database.SqlQueryRaw<int>(
//            "SELECT COUNT(*) AS [Value] FROM dbo.__EFMigrationsHistory").SingleAsync();
//        await using var transaction = await database.Database.BeginTransactionAsync();
//        try
//        {
//            // Simulate the one-parameter legacy definition deployed by main.
//            // DDL and the diagnostic cancellation are rolled back together.
//            await database.Database.ExecuteSqlRawAsync("""
//                CREATE OR ALTER PROCEDURE dbo.usp_CancelReservation
//                    @ReservationId uniqueidentifier
//                AS
//                BEGIN
//                    THROW 51899, 'Legacy fixture must be replaced before calling cancel.', 1;
//                END;
//                """);
//            Assert.That(await database.Database.SqlQueryRaw<int>(
//                "SELECT COUNT(*) AS [Value] FROM sys.parameters WHERE object_id=OBJECT_ID(N'dbo.usp_CancelReservation')").SingleAsync(), Is.EqualTo(1));

//            foreach (var operation in new Cwp04ReservationProcedures().UpOperations.Cast<SqlOperation>())
//                await database.Database.ExecuteSqlRawAsync(operation.Sql);

//            Assert.That(await database.Database.SqlQueryRaw<int>(
//                "SELECT COUNT(*) AS [Value] FROM sys.parameters WHERE object_id=OBJECT_ID(N'dbo.usp_CancelReservation')").SingleAsync(), Is.EqualTo(3));
//            await database.Database.ExecuteSqlInterpolatedAsync(
//                $"EXEC dbo.usp_CancelReservation @UserAccountId={f.AccountId}, @CustomerId={f.CustomerId}, @ReservationId={f.ReservationId}");
//            await AssertStates(f, "CANCELLED", "CANCELLED");
//            var invoice = await database.Invoices.AsNoTracking().SingleAsync(row => row.EntityId == f.ReservationId);
//            Assert.Multiple(() =>
//            {
//                Assert.That(invoice.Status, Is.EqualTo("PAID"));
//                Assert.That(invoice.AmountDue, Is.EqualTo(120000.25m));
//                Assert.That(invoice.PaidAt, Is.EqualTo(f.RecordedAt));
//            });
//            Assert.That(await database.Database.SqlQueryRaw<int>(
//                "SELECT COUNT(*) AS [Value] FROM dbo.__EFMigrationsHistory").SingleAsync(), Is.EqualTo(historyCount));
//            Assert.That(await database.StorageUnits.Where(row => row.StorageUnitId == f.UnitId)
//                .Select(row => row.Status).SingleAsync(), Is.EqualTo("AVAILABLE"));
//        }
//        finally
//        {
//            await transaction.RollbackAsync();
//        }
//        await AssertStates(f, "CONFIRMED", "SCHEDULED");
//        Assert.That(await database.Database.SqlQueryRaw<int>(
//            "SELECT COUNT(*) AS [Value] FROM sys.parameters WHERE object_id=OBJECT_ID(N'dbo.usp_CancelReservation')").SingleAsync(), Is.EqualTo(3));
//    }

//    [TestCase(-1)]
//    [TestCase(1)]
//    public async Task SEC_OPS002_008_LegacyClockArgumentCannotBypassServerDate(int offset)
//    {
//        var f = await Seed("CONFIRMED", "PAID", "SCHEDULED", todayVisit: true);
//        var today = await database.Database.SqlQuery<DateTime>(
//            $"SELECT CONVERT(date, DATEADD(hour, 7, SYSUTCDATETIME())) AS [Value]").SingleAsync();
//        var visitDate = DateOnly.FromDateTime(today).AddDays(offset);
//        await database.Database.ExecuteSqlInterpolatedAsync(
//            $"UPDATE Visit SET VisitDate={visitDate} WHERE VisitId={f.VisitId}");
//        var spoofedClock = visitDate.ToDateTime(new TimeOnly(5, 0));
//        var error = Assert.ThrowsAsync<SqlException>(() => database.Database.ExecuteSqlInterpolatedAsync(
//            $"EXEC dbo.usp_CheckInVisit @VisitId={f.VisitId}, @EmployeeId={f.EmployeeId}, @NowUtc={spoofedClock}"));
//        Assert.That(error!.Number, Is.EqualTo(51303));
//        var visit = await database.Visits.AsNoTracking().SingleAsync(row => row.VisitId == f.VisitId);
//        Assert.Multiple(() =>
//        {
//            Assert.That(visit.Status, Is.EqualTo("SCHEDULED"));
//            Assert.That(visit.EmployeeId, Is.Null);
//            Assert.That(visit.ActualReturnDate, Is.Null);
//        });
//    }

//    private async Task<(Guid VisitId, string Status)> ConfirmProcedure(Guid reservationId, DateOnly date)
//    {
//        await using var connection = new SqlConnection(connectionString);
//        await connection.OpenAsync();
//        await using var command = connection.CreateCommand();
//        command.CommandType = System.Data.CommandType.StoredProcedure;
//        command.CommandText = "dbo.usp_ConfirmReservation";
//        command.Parameters.Add(new SqlParameter("@ReservationId", System.Data.SqlDbType.UniqueIdentifier) { Value = reservationId });
//        command.Parameters.Add(new SqlParameter("@ReservationVisitDate", System.Data.SqlDbType.Date) { Value = date.ToDateTime(TimeOnly.MinValue) });
//        await using var reader = await command.ExecuteReaderAsync();
//        Assert.That(await reader.ReadAsync(), Is.True);
//        return (reader.GetGuid(reader.GetOrdinal("VisitId")), reader.GetString(reader.GetOrdinal("Status")));
//    }

//    private async Task AssertCheckInRejected(Fixture f, string code, Guid? employee = null)
//    {
//        using var scope = RepositoryScope();
//        var error = Assert.ThrowsAsync<StoredProcedureBusinessException>(() => scope.ServiceProvider.GetRequiredService<IVisitRepository>().CheckInAsync(f.VisitId, employee ?? f.EmployeeId, default));
//        Assert.That(error!.Code, Is.EqualTo(code));
//        var visit = await database.Visits.AsNoTracking().SingleAsync(row => row.VisitId == f.VisitId);
//        Assert.That(visit.Status, Is.EqualTo("SCHEDULED"));
//        Assert.That(visit.EmployeeId, Is.Null);
//        Assert.That(visit.ActualReturnDate, Is.Null);
//    }

//    private async Task PrepareVisitType(Fixture f, string type, string contractStatus = "ACTIVE")
//    {
//        if (type == "RESERVATION") return;
//        var contractId = Guid.NewGuid();
//        database.Contracts.Add(new Contract
//        {
//            ContractId = contractId, ReservationId = f.ReservationId, CustomerId = f.CustomerId,
//            FacilityId = f.FacilityId, StorageUnitId = f.UnitId, PolicyId = f.PolicyId,
//            StartMonth = f.StartMonth, EndMonth = f.StartMonth.AddMonths(2), Status = contractStatus
//        });
//        await database.SaveChangesAsync();
//        await database.Database.ExecuteSqlInterpolatedAsync($"UPDATE Visit SET VisitType = {type}, EntityId = {contractId} WHERE VisitId = {f.VisitId}");
//        database.ChangeTracker.Clear();
//    }

//    private FrmsDbContext Context() => new(new DbContextOptionsBuilder<FrmsDbContext>().UseSqlServer(connectionString).Options);

//    private OwnedScope RepositoryScope()
//    {
//        var services = new ServiceCollection();
//        services.AddDataAccess(connectionString);
//        return new OwnedScope(services.BuildServiceProvider());
//    }

//    private async Task<Frms.DataAccess.Repositories.Models.ReservationDetailRecord> Cancel(Fixture f,
//        Guid? account = null, Guid? customer = null, Guid? reservation = null)
//    {
//        await using var scope = RepositoryScope();
//        return await scope.ServiceProvider.GetRequiredService<IReservationRepository>().CancelAsync(
//            account ?? f.AccountId, customer ?? f.CustomerId, reservation ?? f.ReservationId, default);
//    }

//    private async Task<int> AvailableCapacity(Fixture f)
//    {
//        await using var scope = RepositoryScope();
//        var capacities = await scope.ServiceProvider.GetRequiredService<ICapacityRepository>()
//            .GetAvailableCapacitiesAsync(f.FacilityId, [f.UnitTypeId], f.StartMonth, f.StartMonth.AddMonths(2), default);
//        return capacities[f.UnitTypeId];
//    }

//    private async Task AssertStates(Fixture f, string reservation, string visit)
//    {
//        Assert.That(await database.Reservations.Where(row => row.ReservationId == f.ReservationId).Select(row => row.Status).SingleAsync(), Is.EqualTo(reservation));
//        Assert.That(await database.Visits.Where(row => row.VisitId == f.VisitId).Select(row => row.Status).SingleAsync(), Is.EqualTo(visit));
//    }

//    private async Task RequireProcedure(string name) => Assert.That(await ProcedureExists(name), Is.True,
//        $"The real deployed {name} dependency is required; no replacement procedure is created by this test.");

//    private Task<bool> ProcedureExists(string name) => database.Database
//        .SqlQuery<bool>($"SELECT CAST(CASE WHEN OBJECT_ID({"dbo." + name}, 'P') IS NULL THEN 0 ELSE 1 END AS bit) AS [Value]").SingleAsync();

//    private static async Task<Exception?> Observe(Func<Task> action)
//    {
//        try { await action(); return null; }
//        catch (Exception error) { return error; }
//    }

//    private static void AssertConflict(Exception error, params string[] codes)
//    {
//        if (error is StoredProcedureBusinessException business)
//            Assert.That(codes, Does.Contain(business.Code));
//        else if (error is SqlException sql)
//            Assert.That(sql.Number, Is.EqualTo(1205), "Only a transaction deadlock is an allowed raw SQL race failure.");
//        else Assert.Fail($"Unexpected race failure type: {error.GetType().Name}");
//    }

//    private async Task<Fixture> Seed(string status, string invoiceStatus, string? visitStatus, bool todayVisit = false, DateOnly? startMonth = null)
//    {
//        var today = DateOnly.FromDateTime(DateTime.UtcNow.AddHours(7));
//        var month = startMonth ?? new DateOnly(today.Year, today.Month, 1).AddMonths(todayVisit ? 0 : 2);
//        var f = new Fixture(Guid.NewGuid(), Guid.NewGuid(), Guid.NewGuid(), Guid.NewGuid(),
//            Guid.NewGuid(), Guid.NewGuid(), Guid.NewGuid(), Guid.NewGuid(), Guid.NewGuid(), Guid.NewGuid(),
//            month, new DateTime(2026, 10, 10, 3, 0, 0, DateTimeKind.Utc));
//        fixture = f;
//        database.Policies.Add(new Policy {
//            PolicyId = f.PolicyId, Version = await database.Policies.MaxAsync(row => row.Version) + 1,
//            Status = "INACTIVE", EffectiveFrom = f.RecordedAt, DepositTimeoutHours = 1,
//            ReservationVisitStartDay = 1, ReservationVisitEndDay = 31, MonthlyPaymentDueDay = 5,
//            OverdueStartDay = 6, LateFeeDivisorDays = 31, EarlyReturnWaiveFeeUntilDay = 5,
//            CreatedAt = f.RecordedAt
//        });
//        database.UserAccounts.AddRange(Account(f.AccountId, SeedIds.CustomerRole), Account(f.StaffAccountId, SeedIds.FacilityStaffRole));
//        database.Customers.Add(new Customer { CustomerId = f.CustomerId, UserAccountId = f.AccountId, FullName = "Cancellation fixture" });
//        database.Facilities.Add(new Facility { FacilityId = f.FacilityId, Name = "Cancellation fixture", Address = "Test only", Status = "ACTIVE" });
//        database.Employees.Add(new Employee { EmployeeId = f.EmployeeId, UserAccountId = f.StaffAccountId, FacilityId = f.FacilityId, FullName = "Test Staff" });
//        database.UnitTypes.Add(new UnitType { UnitTypeId = f.UnitTypeId, Name = "Cancellation fixture", Mode = "PRIVATE", Size = "Test", RentalPrice = 999999m });
//        database.StorageUnits.Add(new StorageUnit { StorageUnitId = f.UnitId, FacilityId = f.FacilityId, UnitTypeId = f.UnitTypeId, UnitCode = "FIXTURE", Status = "AVAILABLE" });
//        database.Reservations.Add(new Reservation
//        {
//            ReservationId = f.ReservationId, CustomerId = f.CustomerId, FacilityId = f.FacilityId,
//            UnitTypeId = f.UnitTypeId, PolicyId = f.PolicyId, StartMonth = month,
//            EndMonth = month.AddMonths(2), LockedRentalPrice = 120000.25m, DepositAmount = 120000.25m,
//            Status = status, CreatedAt = f.RecordedAt
//        });
//        database.Invoices.Add(new Invoice
//        {
//            InvoiceId = Guid.NewGuid(), EntityId = f.ReservationId, InvoiceType = "DEPOSIT",
//            BaseAmount = 120000.25m, DiscountAmount = 0m, AmountDue = 120000.25m,
//            DueDate = f.RecordedAt.AddHours(1), Status = invoiceStatus,
//            PaidAt = invoiceStatus == "PAID" ? f.RecordedAt : null, CreatedAt = f.RecordedAt
//        });
//        if (visitStatus is not null) database.Visits.Add(new Visit
//        {
//            VisitId = f.VisitId, EntityId = f.ReservationId, VisitType = "RESERVATION",
//            VisitDate = todayVisit ? today : month, Status = visitStatus
//        });
//        await database.SaveChangesAsync();
//        database.ChangeTracker.Clear();
//        return f;
//    }

//    private static UserAccount Account(Guid id, Guid role) => new()
//    {
//        UserAccountId = id, RoleId = role, Email = $"cancel.{id:N}@example.invalid",
//        PhoneNumber = "+84" + string.Concat(Enumerable.Range(0, 9).Select(_ => System.Security.Cryptography.RandomNumberGenerator.GetInt32(10))),
//        PasswordHash = "fixture-not-a-login-hash", Status = "ACTIVE"
//    };

//    private sealed record Fixture(Guid AccountId, Guid CustomerId, Guid ReservationId, Guid FacilityId,
//        Guid UnitTypeId, Guid UnitId, Guid VisitId, Guid StaffAccountId, Guid EmployeeId, Guid PolicyId,
//        DateOnly StartMonth, DateTime RecordedAt);

//    private sealed class OwnedScope(ServiceProvider root) : IDisposable, IAsyncDisposable
//    {
//        private readonly AsyncServiceScope scope = root.CreateAsyncScope();
//        public IServiceProvider ServiceProvider => scope.ServiceProvider;
//        public void Dispose() { scope.Dispose(); root.Dispose(); }
//        public async ValueTask DisposeAsync() { await scope.DisposeAsync(); await root.DisposeAsync(); }
//    }
//}
