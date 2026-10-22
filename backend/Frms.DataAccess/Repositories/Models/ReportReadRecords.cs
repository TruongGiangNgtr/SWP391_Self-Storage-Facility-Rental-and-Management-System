namespace Frms.DataAccess.Repositories.Models;

// Read-only report projections. Status counts are always CURRENT physical state.
public sealed record ReportFacilityLookup(Guid FacilityId, string Name);
public sealed record ReportUnitTypeLookup(Guid UnitTypeId, string Name, string Size);
public sealed record ReportStatusGroup(
    Guid FacilityId, Guid UnitTypeId, int TotalUnits,
    int AvailableUnits, int InUseUnits, int InspectionUnits, int MaintenanceUnits);
public sealed record ReportRevenueGroup(
    Guid FacilityId, Guid UnitTypeId, decimal FirstMonthRecognizedRevenue,
    decimal SubsequentPaidRentalRevenue);
public sealed record ReportUnitDetail(
    Guid StorageUnitId, string UnitCode, Guid UnitTypeId, string UnitTypeName,
    string UnitTypeSize, string? LocationInfo, string Status,
    Guid? ContractId, Guid? CustomerId, string? CustomerName, DateOnly? EndMonth);
