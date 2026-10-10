using System.Text.Json.Serialization;

namespace Frms.Api.DTOs.Responses;

/// <summary>Represents the canonical VisitDetail schema.</summary>
public sealed record VisitDetailResponse(
    Guid VisitId,
    Guid EntityId,
    string VisitType,
    DateOnly VisitDate,
    DateOnly? ActualReturnDate,
    string Status,
    Guid? EmployeeId);

/// <summary>Represents one derived staff work-list item.</summary>
public sealed record StaffWorkItemResponse(
    string WorkType,
    Guid ReferenceId,

    [property: JsonIgnore(
        Condition = JsonIgnoreCondition.WhenWritingNull)]
    Guid? EntityId,

    [property: JsonIgnore(
        Condition = JsonIgnoreCondition.WhenWritingNull)]
    DateOnly? ScheduledDate,

    string Status,

    [property: JsonIgnore(
        Condition = JsonIgnoreCondition.WhenWritingNull)]
    StaffWorkItemCustomerResponse? Customer);

/// <summary>Represents the Contract portion returned after Complete Handover.</summary>
public sealed record HandoverContractResponse(
    Guid ContractId,
    string Status,
    Guid StorageUnitId,
    string StartMonth,
    string EndMonth);

/// <summary>Represents the Complete Handover result.</summary>
public sealed record CompleteHandoverResponse(
    HandoverContractResponse Contract,
    string ReservationStatus,
    string VisitStatus,
    string StorageUnitStatus);

/// <summary>Represents the Confirm Actual Return result.</summary>
public sealed record ConfirmActualReturnResponse(
    Guid VisitId,
    DateOnly ActualReturnDate,
    Guid InspectionId,
    string InspectionStatus,
    string StorageUnitStatus,
    string ReturnClassification);

public sealed record StaffWorkItemCustomerResponse(
    Guid CustomerId,
    string FullName,
    string PhoneNumber);


