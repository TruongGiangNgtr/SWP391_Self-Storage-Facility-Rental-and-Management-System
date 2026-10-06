namespace Frms.DataAccess.Repositories.Models;

public sealed record DepositInvoiceRecord(
    Guid InvoiceId,
    string Status,
    decimal AmountDue,
    DateTime DueDate);

public sealed record CreatedReservationRecord(
    Guid ReservationId,
    Guid FacilityId,
    Guid UnitTypeId,
    Guid PolicyId,
    DateOnly StartMonth,
    DateOnly EndMonth,
    decimal LockedRentalPrice,
    decimal DepositAmount,
    string Status,
    DepositInvoiceRecord DepositInvoice,
    DateTime CreatedAt);

public sealed record ReservationVisitRecord(
    Guid VisitId,
    string VisitType,
    DateOnly VisitDate,
    string Status);

public sealed record ConfirmedReservationRecord(
    Guid ReservationId,
    string Status,
    ReservationVisitRecord ReservationVisit);