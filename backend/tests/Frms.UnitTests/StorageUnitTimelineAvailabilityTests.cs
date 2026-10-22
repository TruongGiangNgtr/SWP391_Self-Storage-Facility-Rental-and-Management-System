using System.Reflection;
using Frms.Business.Models.Results;
using Frms.Business.Services.Interfaces;
using Frms.DataAccess.Repositories.Models;
using NUnit.Framework;

namespace Frms.UnitTests;

[TestFixture]
public sealed class StorageUnitTimelineAvailabilityTests
{
    private static readonly DateTime MonthStart = new(2026, 10, 1, 0, 0, 0, DateTimeKind.Utc);
    private static readonly DateTime MonthEnd = MonthStart.AddMonths(1);

    [Test]
    public void NoStatusEvents_DefaultEntireMonthToAvailable()
    {
        var intervals = Build([]);
        Assert.That(intervals, Has.Count.EqualTo(1));
        Assert.Multiple(() =>
        {
            Assert.That(intervals[0].Status, Is.EqualTo("AVAILABLE"));
            Assert.That(intervals[0].From, Is.EqualTo(new DateTimeOffset(MonthStart)));
            Assert.That(intervals[0].To, Is.EqualTo(new DateTimeOffset(MonthEnd)));
        });
    }

    [Test]
    public void Changes_PreserveRecordedStatusAndFillOtherTimeWithAvailable()
    {
        var inUseAt = MonthStart.AddDays(19);
        var inspectionAt = MonthStart.AddDays(21);
        var intervals = Build([
            Event(inUseAt, "AVAILABLE", "IN_USE"),
            Event(inspectionAt, "IN_USE", "INSPECTION")
        ]);

        Assert.That(intervals.Select(x => x.Status),
            Is.EqualTo(new[] { "AVAILABLE", "IN_USE", "INSPECTION" }));
        Assert.Multiple(() =>
        {
            Assert.That(intervals[0].From, Is.EqualTo(new DateTimeOffset(MonthStart)));
            Assert.That(intervals[0].To, Is.EqualTo(new DateTimeOffset(inUseAt)));
            Assert.That(intervals[1].From, Is.EqualTo(new DateTimeOffset(inUseAt)));
            Assert.That(intervals[1].To, Is.EqualTo(new DateTimeOffset(inspectionAt)));
            Assert.That(intervals[2].From, Is.EqualTo(new DateTimeOffset(inspectionAt)));
            Assert.That(intervals[2].To, Is.EqualTo(new DateTimeOffset(MonthEnd)));
        });
    }

    [TestCase("AVAILABLE")]
    [TestCase("IN_USE")]
    [TestCase("INSPECTION")]
    [TestCase("MAINTENANCE")]
    public void PriorMonthStatus_ContinuesUntilNextRecordedChange(string status)
    {
        var intervals = Build([Event(MonthStart.AddDays(-5), "AVAILABLE", status)]);
        Assert.That(intervals, Has.Count.EqualTo(1));
        Assert.Multiple(() =>
        {
            Assert.That(intervals[0].Status, Is.EqualTo(status));
            Assert.That(intervals[0].From, Is.EqualTo(new DateTimeOffset(MonthStart)));
            Assert.That(intervals[0].To, Is.EqualTo(new DateTimeOffset(MonthEnd)));
        });
    }

    [Test]
    public void FutureMonthWithNoRecordedEvents_IsEntirelyAvailable()
    {
        var start = MonthEnd;
        var end = start.AddMonths(1);
        var intervals = Build([], start, end);
        Assert.That(intervals, Has.Count.EqualTo(1));
        Assert.Multiple(() =>
        {
            Assert.That(intervals[0].Status, Is.EqualTo("AVAILABLE"));
            Assert.That(intervals[0].From, Is.EqualTo(new DateTimeOffset(start)));
            Assert.That(intervals[0].To, Is.EqualTo(new DateTimeOffset(end)));
        });
    }

    [Test]
    public void BadEventStatus_FallsBackToAvailable()
    {
        var intervals = Build([new StorageUnitStatusAuditRecord(
            Guid.NewGuid(), Guid.NewGuid(), "CHANGE_STORAGE_UNIT_STATUS",
            "{\"status\":\"AVAILABLE\"}", "{\"status\":\"UNKNOWN\"}", MonthStart.AddDays(5))]);
        Assert.That(intervals.All(x => x.Status == "AVAILABLE"), Is.True);
        Assert.That(intervals[^1].To, Is.EqualTo(new DateTimeOffset(MonthEnd)));
    }

    private static StorageUnitStatusAuditRecord Event(DateTime at, string oldStatus, string status) =>
        new(Guid.NewGuid(), Guid.NewGuid(), "CHANGE_STORAGE_UNIT_STATUS",
            $"{{\"status\":\"{oldStatus}\"}}", $"{{\"status\":\"{status}\"}}", at);

    private static IReadOnlyList<StorageUnitStatusIntervalResult> Build(
        StorageUnitStatusAuditRecord[] events, DateTime? start = null, DateTime? end = null)
    {
        var type = typeof(IStorageUnitTimelineService).Assembly.GetType(
            "Frms.Business.Services.Implementations.StorageUnitTimelineService", throwOnError: true)!;
        var method = type.GetMethod("BuildIntervals", BindingFlags.NonPublic | BindingFlags.Static)!;
        return (IReadOnlyList<StorageUnitStatusIntervalResult>)method.Invoke(
            null, new object[] { events, start ?? MonthStart, end ?? MonthEnd })!;
    }
}
