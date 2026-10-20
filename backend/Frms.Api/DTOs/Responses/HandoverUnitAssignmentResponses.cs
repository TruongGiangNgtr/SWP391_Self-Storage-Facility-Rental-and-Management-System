using Frms.DataAccess.Repositories.Models;

namespace Frms.Api.DTOs.Responses;

public sealed record HandoverUnitAssignmentResponse(
    Guid AssignmentId,
    Guid ReservationId,
    Guid StorageUnitId,
    string UnitCode,
    string? LocationInfo,
    Guid AssignedByEmployeeId,
    DateTimeOffset AssignedAt,
    string Status) {
    public static HandoverUnitAssignmentResponse From(HandoverUnitAssignmentRecord value) => new(
        value.AssignmentId, value.ReservationId, value.StorageUnitId,
        value.UnitCode, value.LocationInfo, value.AssignedByEmployeeId,
        new DateTimeOffset(DateTime.SpecifyKind(value.AssignedAt, DateTimeKind.Utc)),
        value.Status);
}
