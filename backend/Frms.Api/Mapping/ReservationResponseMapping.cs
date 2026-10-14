using System.Globalization;
using Frms.Api.DTOs.Responses;
using Frms.DataAccess.Repositories.Models;

namespace Frms.Api.Mapping;

internal static class ReservationResponseMapping
{
    public static ReservationDetailResponse ToResponse(this ReservationDetailRecord result) => new(
        result.ReservationId,
        result.FacilityId,
        result.UnitTypeId,
        result.PolicyId,
        result.StartMonth.ToString("yyyy-MM", CultureInfo.InvariantCulture),
        result.EndMonth.ToString("yyyy-MM", CultureInfo.InvariantCulture),
        result.LockedRentalPrice,
        result.DepositAmount,
        result.Status,
        new DepositInvoiceSummaryResponse(
            result.DepositInvoice.InvoiceId,
            result.DepositInvoice.Status,
            result.DepositInvoice.AmountDue,
            ToUtcOffset(result.DepositInvoice.DueDate)),
        result.ReservationVisit is null ? null : new ReservationVisitSummaryResponse(
            result.ReservationVisit.VisitId,
            result.ReservationVisit.VisitType,
            result.ReservationVisit.VisitDate,
            result.ReservationVisit.Status),
        ToUtcOffset(result.CreatedAt)) { CustomerId = result.CustomerId };

    private static DateTimeOffset ToUtcOffset(DateTime value) =>
        new(DateTime.SpecifyKind(value, DateTimeKind.Utc));
}
