namespace Frms.Api.DTOs.Responses;

/// <summary>Represents the canonical Contract renewal result.</summary>
public sealed record RenewContractResponse(
    Guid ContractId,
    string OldEndMonth,
    string NewEndMonth,
    decimal AppliedMonthlyPrice,
    string Status);

public sealed record ContractSummaryResponse(
    Guid ContractId,
    Guid FacilityId,
    Guid StorageUnitId,
    string UnitCode,
    Guid UnitTypeId,
    string UnitTypeName,
    string UnitTypeMode,
    string StartMonth,
    string EndMonth,
    string Status);

public sealed record ContractExtensionResponse(
    Guid ContractExtensionId,
    string OldEndMonth,
    string NewEndMonth,
    decimal AppliedMonthlyPrice,
    DateTimeOffset CreatedAt);

public sealed record ContractVisitResponse(
    Guid VisitId,
    string VisitType,
    DateOnly VisitDate,
    DateOnly? ActualReturnDate,
    string Status,
    Guid? EmployeeId);

public sealed record ContractInvoiceStatusResponse(
    Guid InvoiceId,
    string? BillingMonth,
    decimal AmountDue,
    string Status);

public sealed record ContractStorageUnitResponse(
    Guid StorageUnitId,
    string UnitCode,
    string? LocationInfo,
    string Status,
    Guid UnitTypeId,
    string UnitTypeName,
    string UnitTypeMode,
    string UnitTypeSize);

public sealed record ContractDetailResponse(
    Guid ContractId,
    Guid ReservationId,
    Guid FacilityId,
    Guid PolicyId,
    Guid? DiscountId,
    string StartMonth,
    string EndMonth,
    string Status,
    ContractStorageUnitResponse StorageUnit,
    IReadOnlyList<ContractExtensionResponse> Extensions,
    IReadOnlyList<ContractVisitResponse> Visits,
    IReadOnlyList<ContractInvoiceStatusResponse> RentalInvoices);

public sealed record ContractPaginationResponse(
    int Page,
    int PageSize,
    int TotalItems,
    int TotalPages);

public sealed record ContractListResponse(
    IReadOnlyList<ContractSummaryResponse> Data,
    ContractPaginationResponse Pagination);
