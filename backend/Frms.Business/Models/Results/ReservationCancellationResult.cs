namespace Frms.Business.Models.Results;

public sealed record ReservationCancellationResult(
    Guid ReservationId,
    Guid FacilityId,
    Guid UnitTypeId,
    Guid PolicyId,
    DateOnly StartMonth,
    DateOnly EndMonth,
    decimal LockedRentalPrice,
    decimal DepositAmount,
    string Status,
    ReservationDepositResult DepositInvoice,
    CancelledReservationVisitResult? ReservationVisit,
    DateTimeOffset CreatedAt);

public sealed record ReservationDepositResult(
    Guid InvoiceId,
    string Status,
    decimal AmountDue,
    DateTimeOffset DueDate);

public sealed record CancelledReservationVisitResult(
    Guid VisitId,
    string VisitType,
    DateOnly VisitDate,
    string Status);
