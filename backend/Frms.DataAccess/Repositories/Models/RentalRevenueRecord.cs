namespace Frms.DataAccess.Repositories.Models;

// CALC-REP-02; values are persisted snapshots, not current catalogue pricing.
public sealed record RentalRevenueRecord(
    decimal FirstMonthRecognizedRevenue,
    decimal SubsequentPaidRentalRevenue)
{
    public decimal RentalRevenue => FirstMonthRecognizedRevenue + SubsequentPaidRentalRevenue;
}
