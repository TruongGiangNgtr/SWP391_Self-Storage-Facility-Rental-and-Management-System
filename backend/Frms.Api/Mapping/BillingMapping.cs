using Frms.Api.DTOs.Responses;
using Frms.Business.Models.Results;

namespace Frms.Api.Mapping;

internal static class BillingMapping
{
    public static InvoiceDetailResponse ToResponse(this InvoiceResult x) => new(
        x.InvoiceId, x.EntityId, x.InvoiceType, x.BillingMonth?.ToString("yyyy-MM", System.Globalization.CultureInfo.InvariantCulture),
        x.BaseAmount, x.DiscountId, x.DiscountAmount, x.AmountDue, x.DueDate, x.Status, x.PaidAt);

    public static ContractBillingSummaryResponse ToResponse(this ContractBillingResult x) => new(
        x.ContractId, x.Invoices.Select(ToResponse).ToArray(),
        x.LateFees.Select(f => new LateFeeDetailResponse(f.LateFeeId, f.InvoiceId, f.OverdueDays,
            f.Amount, f.CalculatedAt)).ToArray());
}
