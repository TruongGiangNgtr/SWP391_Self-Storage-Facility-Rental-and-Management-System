namespace Frms.Business.Models.Commands;

public sealed record CreateStorageUnitCommand(
    Guid UnitTypeId,
    string UnitCode,
    string? LocationInfo);

public sealed record UpdateStorageUnitCommand(
    Guid UnitTypeId,
    string? LocationInfo);