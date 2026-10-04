namespace Frms.Api.Authorization;

/// <summary>Contains the five role names locked by FRMS SRS V10.</summary>
public static class RoleNames
{
    public const string Customer = "CUSTOMER";
    public const string FacilityStaff = "FACILITY_STAFF";
    public const string FacilityManager = "FACILITY_MANAGER";
    public const string BusinessOperationsManager = "BUSINESS_OPERATIONS_MANAGER";
    public const string SystemAdministrator = "SYSTEM_ADMINISTRATOR";

    public static IReadOnlyList<string> All { get; } =
    [
        Customer,
        FacilityStaff,
        FacilityManager,
        BusinessOperationsManager,
        SystemAdministrator,
    ];
}
