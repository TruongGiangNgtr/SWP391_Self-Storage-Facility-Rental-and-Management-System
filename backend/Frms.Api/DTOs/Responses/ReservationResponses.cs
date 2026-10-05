namespace Frms.Api.DTOs.Responses;

/// <summary>Represents the Deposit invoice portion of ReservationDetail.</summary>
public sealed record DepositInvoiceSummaryResponse(
    Guid InvoiceId,
    string Status,
    decimal AmountDue,
    DateTimeOffset DueDate);

/// <summary>Represents the RESERVATION Visit portion of ReservationDetail.</summary>
public sealed record ReservationVisitSummaryResponse(
    Guid VisitId,
    string VisitType,
    DateOnly VisitDate,
    string Status);

/// <summary>Represents the canonical ReservationDetail schema.</summary>
public sealed record ReservationDetailResponse(
    Guid ReservationId,
    Guid FacilityId,
    Guid UnitTypeId,
    Guid PolicyId,
    string StartMonth,
    string EndMonth,
    decimal LockedRentalPrice,
    decimal DepositAmount,
    string Status,
    DepositInvoiceSummaryResponse DepositInvoice,
    ReservationVisitSummaryResponse? ReservationVisit,
    DateTimeOffset CreatedAt);

/// <summary>Represents the result of confirming a Reservation.</summary>
public sealed record ConfirmReservationResponse(
    Guid ReservationId,
    string Status,
    ReservationVisitSummaryResponse ReservationVisit);
