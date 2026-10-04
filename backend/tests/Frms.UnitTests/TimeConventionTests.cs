using Frms.Business.Abstractions.Time;
namespace Frms.UnitTests;

[TestFixture]
public sealed class TimeConventionTests
{
    [Test]
    public void ToBusinessTime_UtcInstant_ConvertsToAsiaHoChiMinhWithoutChangingInstant()
    {
        IClock clock = new SystemClock(); var utc = new DateTimeOffset(2026, 10, 4, 0, 30, 0, TimeSpan.Zero); var business = clock.ToBusinessTime(utc);
        Assert.Multiple(() => { Assert.That(business.Hour, Is.EqualTo(7)); Assert.That(business.Offset, Is.EqualTo(TimeSpan.FromHours(7))); Assert.That(business.UtcDateTime, Is.EqualTo(utc.UtcDateTime)); });
    }
}
