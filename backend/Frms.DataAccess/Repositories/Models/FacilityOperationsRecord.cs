namespace Frms.DataAccess.Repositories.Models;

public sealed record FacilityOperationsRecord(
    int AvailableUnits,
    int InUseUnits,
    int InspectionUnits,
    int MaintenanceUnits,
    int TotalUnits,
    int OverdueContractCount);
