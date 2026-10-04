namespace Frms.Business.Abstractions.Time;

internal sealed class SystemClock : IClock
{
    private const string WindowsTimeZone = "SE Asia Standard Time";
    private const string IanaTimeZone = "Asia/Ho_Chi_Minh";

    public DateTimeOffset UtcNow => DateTimeOffset.UtcNow;
    public TimeZoneInfo BusinessTimeZone { get; } = ResolveBusinessTimeZone();
    public DateTimeOffset ToBusinessTime(DateTimeOffset utcTimestamp) => TimeZoneInfo.ConvertTime(utcTimestamp, BusinessTimeZone);

    private static TimeZoneInfo ResolveBusinessTimeZone()
    {
        try { return TimeZoneInfo.FindSystemTimeZoneById(IanaTimeZone); }
        catch (TimeZoneNotFoundException) { return TimeZoneInfo.FindSystemTimeZoneById(WindowsTimeZone); }
    }
}
