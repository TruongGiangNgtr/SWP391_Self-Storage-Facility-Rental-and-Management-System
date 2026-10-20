using Frms.Business.Validators;

namespace Frms.UnitTests;

[TestFixture]
public sealed class PolicyVersionValidatorEdgeCaseTests
{
    public static IEnumerable<TestCaseData> InvalidPolicyInputs()
    {
        foreach (var value in new[] { int.MinValue, -1, 0 })
            yield return Case(value, 1, 31, 15, 16, 30, 31, "depositTimeoutHours", $"deposit_timeout_{value}");

        foreach (var value in new[] { int.MinValue, -1, 0, 32, int.MaxValue })
            yield return Case(24, value, 31, 15, 16, 30, 31, "reservationVisitStartDay", $"visit_start_{value}");
        foreach (var value in new[] { int.MinValue, -1, 0, 32, int.MaxValue })
            yield return Case(24, 1, value, 15, 16, 30, 31, "reservationVisitEndDay", $"visit_end_{value}");
        foreach (var value in new[] { int.MinValue, -1, 0, 32, int.MaxValue })
            yield return Case(24, 1, 31, value, 16, 30, 31, "monthlyPaymentDueDay", $"monthly_due_{value}");
        foreach (var value in new[] { int.MinValue, -1, 0, 32, int.MaxValue })
            yield return Case(24, 1, 31, 15, value, 30, 31, "overdueStartDay", $"overdue_start_{value}");
        foreach (var value in new[] { int.MinValue, -1, 0 })
            yield return Case(24, 1, 31, 15, 16, value, 31, "lateFeeDivisorDays", $"late_fee_divisor_{value}");
        foreach (var value in new[] { int.MinValue, -1, 0, 32, int.MaxValue })
            yield return Case(24, 1, 31, 15, 16, 30, value, "earlyReturnWaiveFeeUntilDay", $"early_return_cutoff_{value}");

        yield return Case(24, 2, 1, 15, 16, 30, 31, "reservationVisitEndDay", "visit_range_start_2_end_1");
        yield return Case(24, 31, 1, 15, 16, 30, 31, "reservationVisitEndDay", "visit_range_start_31_end_1");
        yield return Case(24, 10, 9, 15, 16, 30, 31, "reservationVisitEndDay", "visit_range_start_10_end_9");

        // Validation order is intentional: the first invalid field wins.
        yield return Case(0, 0, 0, 0, 0, 0, 0, "depositTimeoutHours", "multiple_invalid_fields_preserve_priority");
    }

    [TestCaseSource(nameof(InvalidPolicyInputs))]
    public void Validate_InvalidBoundaryOrRange_ReturnsExpectedField(
        int depositTimeoutHours, int visitStart, int visitEnd, int monthlyDue,
        int overdueStart, int lateFeeDivisor, int earlyReturnCutoff, string expectedField)
    {
        var result = PolicyVersionValidator.Validate(depositTimeoutHours, visitStart, visitEnd,
            monthlyDue, overdueStart, lateFeeDivisor, earlyReturnCutoff);
        Assert.That(result?.Field, Is.EqualTo(expectedField));
    }

    public static IEnumerable<TestCaseData> ValidBoundaryInputs()
    {
        yield return new TestCaseData(1, 1, 1, 1, 1, 1, 1).SetName("all_minimum_valid_values");
        yield return new TestCaseData(int.MaxValue, 1, 31, 31, 31, int.MaxValue, 31).SetName("maximum_days_and_large_positive_durations");
        yield return new TestCaseData(24, 1, 31, 15, 16, 30, 31).SetName("full_valid_range");
        yield return new TestCaseData(1, 31, 31, 31, 31, 1, 1).SetName("visit_start_equals_end_at_day_31");
        yield return new TestCaseData(1, 1, 1, 1, 1, 1, 31).SetName("visit_start_equals_end_at_day_1");
        yield return new TestCaseData(1, 15, 15, 15, 15, 1, 15).SetName("single_day_visit_range");
        yield return new TestCaseData(2, 1, 2, 2, 3, 2, 4).SetName("small_positive_durations");
        yield return new TestCaseData(720, 28, 29, 28, 29, 365, 30).SetName("late_month_values");
        yield return new TestCaseData(24, 30, 31, 30, 31, 30, 31).SetName("last_two_days_of_month");
        yield return new TestCaseData(999999, 10, 10, 10, 10, 999999, 10).SetName("large_positive_durations_same_day_range");
    }

    [TestCaseSource(nameof(ValidBoundaryInputs))]
    public void Validate_ValidBoundaryValues_ReturnsNoViolation(
        int depositTimeoutHours, int visitStart, int visitEnd, int monthlyDue,
        int overdueStart, int lateFeeDivisor, int earlyReturnCutoff) =>
        Assert.That(PolicyVersionValidator.Validate(depositTimeoutHours, visitStart, visitEnd,
            monthlyDue, overdueStart, lateFeeDivisor, earlyReturnCutoff), Is.Null);

    private static TestCaseData Case(int deposit, int start, int end, int monthly, int overdue,
        int divisor, int cutoff, string expectedField, string name) =>
        new TestCaseData(deposit, start, end, monthly, overdue, divisor, cutoff, expectedField).SetName(name);
}
