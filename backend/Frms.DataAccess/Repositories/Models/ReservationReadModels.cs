namespace Frms.DataAccess.Repositories.Models;

public sealed record ReservationDetailRecord(
    Guid ReservationId,
    Guid CustomerId,
    Guid FacilityId,
    Guid UnitTypeId,
    Guid PolicyId,
    DateOnly StartMonth,
    DateOnly EndMonth,
    decimal LockedRentalPrice,
    decimal DepositAmount,
    string Status,
    DepositInvoiceRecord DepositInvoice,
    ReservationVisitRecord? ReservationVisit,
    DateTime CreatedAt);

public sealed record PagedReservationRecord(
    IReadOnlyList<ReservationDetailRecord> Items,
    int Page,
    int PageSize,
    int TotalItems,
    int TotalPages);
