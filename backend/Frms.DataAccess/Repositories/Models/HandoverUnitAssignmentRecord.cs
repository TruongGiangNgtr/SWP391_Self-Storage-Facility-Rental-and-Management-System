namespace Frms.DataAccess.Repositories.Models;

public sealed record HandoverUnitAssignmentRecord(
    Guid AssignmentId,
    Guid ReservationId,
    Guid StorageUnitId,
    string UnitCode,
    string? LocationInfo,
    Guid AssignedByEmployeeId,
    DateTime AssignedAt,
    string Status);
