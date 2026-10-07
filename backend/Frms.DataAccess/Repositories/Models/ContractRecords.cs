namespace Frms.DataAccess.Repositories.Models;

public sealed record ContractSummaryRecord(
    Guid ContractId,
    Guid FacilityId,
    Guid StorageUnitId,
    string UnitCode,
    Guid UnitTypeId,
    string UnitTypeName,
    string UnitTypeMode,
    DateOnly StartMonth,
    DateOnly EndMonth,
    string Status);

public sealed record ContractExtensionRecord(
    Guid ContractExtensionId,
    DateOnly OldEndMonth,
    DateOnly NewEndMonth,
    decimal AppliedMonthlyPrice,
    DateTime CreatedAt);

public sealed record ContractVisitRecord(
    Guid VisitId,
    string VisitType,
    DateOnly VisitDate,
    DateTime? ActualReturnDate,
    string Status,
    Guid? EmployeeId);

public sealed record ContractInvoiceRecord(
    Guid InvoiceId,
    DateOnly? BillingMonth,
    decimal AmountDue,
    string Status);

public sealed record ContractDetailRecord(
    Guid ContractId,
    Guid ReservationId,
    Guid FacilityId,
    Guid StorageUnitId,
    Guid PolicyId,
    Guid? DiscountId,
    DateOnly StartMonth,
    DateOnly EndMonth,
    string Status,
    string UnitCode,
    string? LocationInfo,
    string StorageUnitStatus,
    Guid UnitTypeId,
    string UnitTypeName,
    string UnitTypeMode,
    string UnitTypeSize,
    IReadOnlyList<ContractExtensionRecord> Extensions,
    IReadOnlyList<ContractVisitRecord> Visits,
    IReadOnlyList<ContractInvoiceRecord> RentalInvoices);

public sealed record ContractPageRecord(
    IReadOnlyList<ContractSummaryRecord> Items,
    int Page,
    int PageSize,
    int TotalItems,
    int TotalPages);