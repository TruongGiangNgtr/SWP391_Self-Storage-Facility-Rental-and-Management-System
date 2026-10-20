namespace Frms.Api.DTOs.Responses;

public sealed record FacilityRevenueReportResponse(
    Guid FacilityId, string Month, decimal FirstMonthRecognizedRevenue,
    decimal SubsequentPaidRentalRevenue, decimal RentalRevenue);
