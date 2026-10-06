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