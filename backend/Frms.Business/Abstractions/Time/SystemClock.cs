namespace Frms.Business.Abstractions.Time;

internal sealed class SystemClock : IClock {
    public SystemClock() : this("Asia/Ho_Chi_Minh") {
    }

    public SystemClock(string timeZoneId) {
        if (string.IsNullOrWhiteSpace(timeZoneId))
            throw new ArgumentException(
                "Business timezone is required.",
                nameof(timeZoneId));

        BusinessTimeZone = ResolveTimeZone(timeZoneId);
    }

    public DateTimeOffset UtcNow => DateTimeOffset.UtcNow;

    public TimeZoneInfo BusinessTimeZone { get; }

    public DateTimeOffset ToBusinessTime(
        DateTimeOffset utcTimestamp) {
        return TimeZoneInfo.ConvertTime(
            utcTimestamp,
            BusinessTimeZone);
    }

    private static TimeZoneInfo ResolveTimeZone(
        string timeZoneId) {
        try {
            return TimeZoneInfo.FindSystemTimeZoneById(
                timeZoneId);
        }
        catch (TimeZoneNotFoundException)
            when (timeZoneId == "Asia/Ho_Chi_Minh") {
            return TimeZoneInfo.FindSystemTimeZoneById(
                "SE Asia Standard Time");
        }
    }
}
