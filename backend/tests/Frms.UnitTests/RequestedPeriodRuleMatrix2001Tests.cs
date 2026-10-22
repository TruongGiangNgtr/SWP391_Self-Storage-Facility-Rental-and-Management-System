using System.Globalization;
using Frms.Business.Exceptions;
using Frms.Business.Rules;

namespace Frms.UnitTests;

[TestFixture]
public sealed class RequestedPeriodRuleMatrix2001Tests
{
    public static IEnumerable<TestCaseData> Cases()
    {
        var id = 2001;
        for (var index = 0; index < 48; index++)
        {
            var start = new DateOnly(2020, 1, 1).AddMonths(index);
            var end = start.AddMonths(Math.Min(index % 4, 47 - index));
            yield return new TestCaseData(start.ToString("yyyy-MM", CultureInfo.InvariantCulture), end.ToString("yyyy-MM", CultureInfo.InvariantCulture), true)
                .SetName($"TC_{id++}_valid_month_range");
        }
        yield return new TestCaseData(null, null, true).SetName($"TC_{id++}_both_months_null");
        yield return new TestCaseData("  ", "\t", true).SetName($"TC_{id++}_both_months_whitespace");

        var invalid = new (string? Start, string? End)[]
        {
            ("2026-01", null), ("2026-02", ""), ("2026-03", "   "), ("2026-04", "\t"),
            (null, "2026-01"), ("", "2026-02"), ("   ", "2026-03"), ("\t", "2026-04"),
            ("2026-1", "2026-02"), ("26-01", "2026-02"), ("2026/01", "2026-02"), ("2026-01-01", "2026-02"),
            ("abcd-01", "2026-02"), ("0000-01", "2026-02"), ("+2026-01", "2026-02"), ("2026- 1", "2026-02"),
            ("2026-00", "2026-02"), ("2026-13", "2026-02"), ("2026-99", "2026-02"), ("2026-02 ", "2026-03"),
            ("2026-01", "2026-1"), ("2026-01", "26-02"), ("2026-01", "2026/02"), ("2026-01", "2026-02-01"),
            ("2026-01", "abcd-02"), ("2026-01", "0000-02"), ("2026-01", "+2026-02"), ("2026-01", "2026- 2"),
            ("2026-01", "2026-00"), ("2026-01", "2026-13"), ("2026-01", "2026-99"), ("2026-01", "2026-02 "),
            ("2026-02", "2026-01"), ("2026-03", "2026-02"), ("2025-12", "2025-11"), ("2024-01", "2023-12"),
            ("2020-01", "2019-12"), ("2030-06", "2030-05"), ("1999-12", "1999-11"), ("2026-12", "2026-11"),
            ("2026-13", "2026-13"), ("not-a-month", "also-not-a-month"), ("", "2026-01"),
            ("2026-01", ""), (" 2026-01", "2026-02"), ("2026-01", " 2026-02"), ("2026--01", "2026-02"),
            ("2026-01", "2026--02"), ("2026-01;DROP", "2026-02")
        };
        foreach (var item in invalid)
            yield return new TestCaseData(item.Start, item.End, false).SetName($"TC_{id++}_invalid_month_range");
        if (id != 2101) throw new InvalidOperationException($"Expected 100 cases; generated {id - 2001}.");
    }

    [TestCaseSource(nameof(Cases))]
    public void Parse_RequestedMonthRange_HandlesBoundaryInput(string? start, string? end, bool valid)
    {
        if (!valid)
        {
            var error = Assert.Throws<BusinessException>(() => RequestedPeriodRule.Parse(start, end));
            Assert.That(error!.Code, Is.EqualTo("INVALID_MONTH_RANGE"));
            Assert.That(error.SuggestedStatusCode, Is.EqualTo(400));
            return;
        }

        var actual = RequestedPeriodRule.Parse(start, end);
        if (string.IsNullOrWhiteSpace(start) && string.IsNullOrWhiteSpace(end))
        {
            Assert.Multiple(() =>
            {
                Assert.That(actual.StartMonth, Is.Null);
                Assert.That(actual.EndMonth, Is.Null);
            });
            return;
        }
        Assert.Multiple(() =>
        {
            Assert.That(actual.StartMonth, Is.EqualTo(DateOnly.ParseExact($"{start}-01", "yyyy-MM-dd", CultureInfo.InvariantCulture)));
            Assert.That(actual.EndMonth, Is.EqualTo(DateOnly.ParseExact($"{end}-01", "yyyy-MM-dd", CultureInfo.InvariantCulture)));
        });
    }
}
