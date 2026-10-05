namespace Frms.Api.DTOs.Responses;

/// <summary>Represents the Facility Operations report example in SRS V10.</summary>
public sealed record FacilityOperationsReportResponse(
    Guid FacilityId,
    DateTimeOffset AsOf,
    int AvailableUnits,
    int InUseUnits,
    int InspectionUnits,
    int MaintenanceUnits,
    decimal UsageRate,
    int OverdueContractCount);

/// <summary>Represents the Business Overview report example in SRS V10.</summary>
public sealed record BusinessOverviewReportResponse(
    int FacilityCount,
    int ActiveContractCount,
    decimal UsageRate,
    decimal RentalRevenue);
