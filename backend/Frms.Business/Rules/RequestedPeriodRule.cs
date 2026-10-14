using System.Globalization;
using Frms.Business.Exceptions;

namespace Frms.Business.Rules;

internal static class RequestedPeriodRule
{
    internal sealed record Period(DateOnly? StartMonth, DateOnly? EndMonth);

    internal static Period Parse(string? startMonth, string? endMonth)
    {
        var hasStart = !string.IsNullOrWhiteSpace(startMonth);
        var hasEnd = !string.IsNullOrWhiteSpace(endMonth);
        if (!hasStart && !hasEnd)
            return new Period(null, null);
        if (hasStart != hasEnd)
            throw InvalidMonthRange();
        if (!TryParseMonth(startMonth!, out var start) ||
            !TryParseMonth(endMonth!, out var end) || end < start)
            throw InvalidMonthRange();
        return new Period(start, end);
    }

    private static bool TryParseMonth(string value, out DateOnly month) =>
        DateOnly.TryParseExact($"{value}-01", "yyyy-MM-dd",
            CultureInfo.InvariantCulture, DateTimeStyles.None, out month);

    private static BusinessException InvalidMonthRange() => new(
        "INVALID_MONTH_RANGE", "The requested month range is invalid.", 400);
}
