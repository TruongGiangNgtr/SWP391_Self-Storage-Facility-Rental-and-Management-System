using Frms.DataAccess.Repositories.Models;

namespace Frms.Business.Models.Results;

public sealed record ReportPagination(int Page, int PageSize, int TotalItems, int TotalPages);
public sealed record ReportDailyStatus(
    DateOnly Date, int AvailableUnits, int InUseUnits, int InspectionUnits,
    int MaintenanceUnits, int KnownUnits, int UnknownUnits, decimal? UsageRate);
public sealed record ReportUnitTypeUtilization(
    Guid UnitTypeId, string Name, string Size, int TotalUnits, int AvailableUnits,
    int InUseUnits, int InspectionUnits, int MaintenanceUnits, decimal UsageRate);
public sealed record FacilityUnitReadReport(
    Guid FacilityId, string Month, DateTimeOffset AsOf, string InventoryScope,
    bool HistoricalInventorySupported, IReadOnlyList<ReportUnitTypeUtilization> UnitTypes,
    IReadOnlyList<ReportUnitDetail> Inventory, ReportPagination Pagination,
    IReadOnlyList<ReportDailyStatus> DailyTrend,
    decimal? AverageOccupancy, DateOnly? PeakDay);
public sealed record ReportSummary(
    int FacilityCount, int UnitTypeCount, int TotalUnits, int AvailableUnits,
    int InUseUnits, int InspectionUnits, int MaintenanceUnits, decimal UsageRate,
    decimal FirstMonthRecognizedRevenue, decimal SubsequentPaidRentalRevenue,
    decimal RentalRevenue);
public sealed record BusinessReportGroup(
    Guid? FacilityId, string? FacilityName, Guid? UnitTypeId, string? UnitTypeName,
    string? UnitTypeSize, int TotalUnits, int AvailableUnits, int InUseUnits,
    int InspectionUnits, int MaintenanceUnits, decimal UsageRate,
    decimal FirstMonthRecognizedRevenue, decimal SubsequentPaidRentalRevenue,
    decimal RentalRevenue);
public sealed record BusinessReportBreakdown(
    string View, int Year, int? Quarter, DateTimeOffset AsOf, ReportSummary Summary,
    IReadOnlyList<BusinessReportGroup> Rows, ReportPagination Pagination);
public sealed record ReportRevenueMonth(
    string Month, decimal FirstMonthRecognizedRevenue,
    decimal SubsequentPaidRentalRevenue, decimal RentalRevenue);
public sealed record BusinessReportMonthly(
    int Year, int? Quarter, Guid? FacilityId, Guid? UnitTypeId,
    decimal FirstMonthRecognizedRevenue, decimal SubsequentPaidRentalRevenue,
    decimal RentalRevenue, IReadOnlyList<ReportRevenueMonth> Months);
