namespace Frms.Business.Abstractions.Time;

public interface IClock
{
    DateTimeOffset UtcNow { get; }
    TimeZoneInfo BusinessTimeZone { get; }
    DateTimeOffset ToBusinessTime(DateTimeOffset utcTimestamp);
}
