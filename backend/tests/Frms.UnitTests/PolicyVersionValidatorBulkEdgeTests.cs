using Frms.Business.Validators;

namespace Frms.UnitTests;

[TestFixture]
public sealed class PolicyVersionValidatorBulkEdgeTests
{
    public static IEnumerable<TestCaseData> PolicyCases()
    {
        // 25 distinct non-positive values for each duration field.
        foreach (var value in Enumerable.Range(-24, 25))
            yield return Case(value, 1, 31, 15, 16, 30, 31, "depositTimeoutHours", $"deposit_timeout_{value}");

        // 25 invalid values for each day field: -12..0 and 32..43.
        var invalidDays = Enumerable.Range(-12, 13).Concat(Enumerable.Range(32, 12));
        foreach (var value in invalidDays)
            yield return Case(24, value, 31, 15, 16, 30, 31, "reservationVisitStartDay", $"visit_start_{value}");
        foreach (var value in invalidDays)
            yield return Case(24, 1, value, 15, 16, 30, 31, "reservationVisitEndDay", $"visit_end_{value}");
        foreach (var value in invalidDays)
            yield return Case(24, 1, 31, value, 16, 30, 31, "monthlyPaymentDueDay", $"monthly_due_{value}");
        foreach (var value in invalidDays)
            yield return Case(24, 1, 31, 15, value, 30, 31, "overdueStartDay", $"overdue_start_{value}");
        foreach (var value in invalidDays)
            yield return Case(24, 1, 31, 15, 16, 30, value, "earlyReturnWaiveFeeUntilDay", $"early_return_cutoff_{value}");

        foreach (var value in Enumerable.Range(-24, 25))
            yield return Case(24, 1, 31, 15, 16, value, 31, "lateFeeDivisorDays", $"late_fee_divisor_{value}");

        // 28 distinct inverted but individually valid ranges.
        for (var start = 2; start <= 29; start++)
            yield return Case(24, start, start - 1, 15, 16, 30, 31,
                "reservationVisitEndDay", $"inverted_visit_range_{start}_{start - 1}");
    }

    [TestCaseSource(nameof(PolicyCases))]
    public void Validate_BulkInvalidBoundaryValues_ReturnsExpectedFirstField(
        int deposit, int start, int end, int monthly, int overdue, int divisor, int cutoff,
        string expectedField)
    {
        var result = PolicyVersionValidator.Validate(deposit, start, end, monthly, overdue, divisor, cutoff);
        Assert.That(result?.Field, Is.EqualTo(expectedField));
    }

    private static TestCaseData Case(int deposit, int start, int end, int monthly, int overdue,
        int divisor, int cutoff, string expectedField, string name) =>
        new TestCaseData(deposit, start, end, monthly, overdue, divisor, cutoff, expectedField).SetName(name);
}
