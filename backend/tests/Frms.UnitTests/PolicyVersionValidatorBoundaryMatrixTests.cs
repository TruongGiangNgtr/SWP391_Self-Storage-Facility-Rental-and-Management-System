using Frms.Business.Validators;

namespace Frms.UnitTests;

[TestFixture, Category("PolicyVersionValidation")]
public sealed class PolicyVersionValidatorBoundaryMatrixTests
{
    public static IEnumerable<TestCaseData> Cases()
    {
        for (var value = -10; value <= 0; value++)
            yield return Case(value, 1, 31, 15, 16, 30, 31, "depositTimeoutHours", $"deposit_{value}");
        foreach (var value in InvalidDays())
            yield return Case(24, value, 31, 15, 16, 30, 31, "reservationVisitStartDay", $"visit_start_{value}");
        foreach (var value in InvalidDays())
            yield return Case(24, 1, value, 15, 16, 30, 31, "reservationVisitEndDay", $"visit_end_{value}");
        foreach (var value in InvalidDays())
            yield return Case(24, 1, 31, value, 16, 30, 31, "monthlyPaymentDueDay", $"monthly_due_{value}");
        foreach (var value in InvalidDays())
            yield return Case(24, 1, 31, 15, value, 30, 31, "overdueStartDay", $"overdue_start_{value}");
        for (var value = -10; value <= 0; value++)
            yield return Case(24, 1, 31, 15, 16, value, 31, "lateFeeDivisorDays", $"late_fee_divisor_{value}");
        foreach (var value in InvalidDays())
            yield return Case(24, 1, 31, 15, 16, 30, value, "earlyReturnWaiveFeeUntilDay", $"early_return_cutoff_{value}");
        for (var start = 17; start <= 31; start++)
            yield return Case(24, start, start - 1, 15, 16, 30, 31, "reservationVisitEndDay", $"reversed_visit_range_{start}");
        var validPairs = new[] { (1, 1), (1, 31), (31, 31), (2, 2), (15, 15), (30, 31), (7, 20), (20, 30) };
        for (var i = 0; i < validPairs.Length; i++)
            yield return new TestCaseData(1, validPairs[i].Item1, validPairs[i].Item2, 1, 1, 1, 1, (string?)null)
                .SetName($"valid_boundary_policy_{i + 1}");
    }

    [TestCaseSource(nameof(Cases))]
    public void Validate_BoundaryMatrixReportsFirstInvalidField(
        int depositTimeoutHours, int reservationVisitStartDay, int reservationVisitEndDay,
        int monthlyPaymentDueDay, int overdueStartDay, int lateFeeDivisorDays,
        int earlyReturnWaiveFeeUntilDay, string? expectedField)
    {
        var actual = PolicyVersionValidator.Validate(depositTimeoutHours, reservationVisitStartDay,
            reservationVisitEndDay, monthlyPaymentDueDay, overdueStartDay, lateFeeDivisorDays,
            earlyReturnWaiveFeeUntilDay);
        Assert.That(actual?.Field, Is.EqualTo(expectedField));
    }

    private static IEnumerable<int> InvalidDays()
    {
        for (var value = -5; value <= 0; value++) yield return value;
        for (var value = 32; value <= 36; value++) yield return value;
    }

    private static TestCaseData Case(int timeout, int start, int end, int due, int overdue,
        int divisor, int waive, string expectedField, string name) =>
        new TestCaseData(timeout, start, end, due, overdue, divisor, waive, expectedField).SetName(name);
}
