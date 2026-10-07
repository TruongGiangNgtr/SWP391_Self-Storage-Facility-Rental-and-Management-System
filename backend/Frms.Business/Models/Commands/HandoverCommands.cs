namespace Frms.Business.Models.Commands;

public sealed record CompleteHandoverCommand(
    Guid ReservationId,
    Guid VisitId,
    Guid StorageUnitId,
    Guid? DiscountId);
