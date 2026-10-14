namespace Frms.Business.Validators;

/// <summary>Pure validation for BOM-009, preserving existing error field/message priority.</summary>
public static class PolicyVersionValidator
{
    public static (string Field, string Message)? Validate(
        int depositTimeoutHours, int reservationVisitStartDay,
        int reservationVisitEndDay, int monthlyPaymentDueDay,
        int overdueStartDay, int lateFeeDivisorDays,
        int earlyReturnWaiveFeeUntilDay)
    {
        if (depositTimeoutHours <= 0)
            return ("depositTimeoutHours", "Deposit timeout hours must be greater than 0.");
        if (!IsValidDay(reservationVisitStartDay))
            return ("reservationVisitStartDay", "Reservation visit start day must be between 1 and 31.");
        if (!IsValidDay(reservationVisitEndDay))
            return ("reservationVisitEndDay", "Reservation visit end day must be between 1 and 31.");
        if (reservationVisitStartDay > reservationVisitEndDay)
            return ("reservationVisitEndDay", "Reservation visit end day must be greater than or equal to start day.");
        if (!IsValidDay(monthlyPaymentDueDay))
            return ("monthlyPaymentDueDay", "Monthly payment due day must be between 1 and 31.");
        if (!IsValidDay(overdueStartDay))
            return ("overdueStartDay", "Overdue start day must be between 1 and 31.");
        if (lateFeeDivisorDays <= 0)
            return ("lateFeeDivisorDays", "Late fee divisor days must be greater than 0.");
        if (!IsValidDay(earlyReturnWaiveFeeUntilDay))
            return ("earlyReturnWaiveFeeUntilDay", "Early return waive fee cutoff must be between 1 and 31.");
        return null;
    }

    private static bool IsValidDay(int value) => value is >= 1 and <= 31;
}
