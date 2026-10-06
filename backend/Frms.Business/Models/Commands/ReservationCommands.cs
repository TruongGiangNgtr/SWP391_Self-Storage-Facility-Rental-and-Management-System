namespace Frms.Business.Models.Commands;

public sealed record CreateReservationCommand(
    Guid FacilityId,
    Guid UnitTypeId,
    DateOnly StartMonth,
    DateOnly EndMonth);