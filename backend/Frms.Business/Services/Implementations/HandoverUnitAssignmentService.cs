using Frms.Business.Abstractions.Security;
using Frms.Business.Exceptions;
using Frms.Business.Services.Interfaces;
using Frms.DataAccess.Repositories.Interfaces;
using Frms.DataAccess.Repositories.Models;
using Frms.DataAccess.StoredProcedures;

namespace Frms.Business.Services.Implementations;

internal sealed class HandoverUnitAssignmentService(
    IHandoverUnitAssignmentRepository repository,
    IFacilityAuthorizationService facilityAuthorizationService,
    ICurrentUserContext currentUser) : IHandoverUnitAssignmentService {

    public async Task<HandoverUnitAssignmentRecord?> GetActiveAsync(Guid reservationId,
        CancellationToken cancellationToken = default) {
        EnsureRole(allowStaff: true);
        await EnsureAuthorizedReservationAsync(reservationId, cancellationToken);
        return await repository.GetActiveAsync(reservationId, cancellationToken);
    }

    public async Task<HandoverUnitAssignmentRecord> AssignAsync(Guid reservationId,
        Guid storageUnitId, CancellationToken cancellationToken = default) {
        EnsureRole(allowStaff: false);
        if (storageUnitId == Guid.Empty)
            throw new BusinessException("VALIDATION_ERROR", "StorageUnitId is required.", 400);
        await EnsureAuthorizedReservationAsync(reservationId, cancellationToken);
        try {
            await repository.AssignAsync(reservationId, storageUnitId, currentUser.UserAccountId,
                cancellationToken);
        }
        catch (StoredProcedureBusinessException ex) when (MapSqlError(ex.Code) is not null) {
            throw MapSqlError(ex.Code)!;
        }
        // Re-read authoritative database selection; no client-side optimistic assignment.
        return await repository.GetActiveAsync(reservationId, cancellationToken)
            ?? throw new BusinessException("HANDOVER_ASSIGNMENT_NOT_FOUND", "Assignment was not persisted.", 409);
    }

    public async Task ReleaseAsync(Guid reservationId, CancellationToken cancellationToken = default) {
        EnsureRole(allowStaff: false);
        await EnsureAuthorizedReservationAsync(reservationId, cancellationToken);
        try {
            await repository.ReleaseAsync(reservationId, currentUser.UserAccountId, cancellationToken);
        }
        catch (StoredProcedureBusinessException ex) when (MapSqlError(ex.Code) is not null) {
            throw MapSqlError(ex.Code)!;
        }
    }

    private void EnsureRole(bool allowStaff) {
        if (!currentUser.IsAuthenticated)
            throw new BusinessException("UNAUTHORIZED", "Authentication is required.", 401);
        if (currentUser.Role != "FACILITY_MANAGER" &&
            !(allowStaff && currentUser.Role == "FACILITY_STAFF"))
            throw new BusinessException("FORBIDDEN", "Role is not authorized for assignment.", 403);
    }

    private async Task EnsureAuthorizedReservationAsync(Guid reservationId,
        CancellationToken cancellationToken) {
        if (reservationId == Guid.Empty)
            throw new BusinessException("VALIDATION_ERROR", "ReservationId is required.", 400);
        var reservation = await repository.GetReservationAsync(reservationId, cancellationToken);
        if (reservation is null)
            throw new BusinessException("RESOURCE_NOT_FOUND", "Reservation was not found.", 404);
        await facilityAuthorizationService.EnsureSameFacilityAsync(reservation.FacilityId, cancellationToken);
    }

    private static BusinessException? MapSqlError(string code) => code switch {
        "FORBIDDEN" => new("FORBIDDEN", "Manager is not authorized for this Facility.", 403),
        "RESOURCE_NOT_FOUND" => new("RESOURCE_NOT_FOUND", "Reservation or StorageUnit not found.", 404),
        "RESERVATION_INVALID_STATUS" => new(code, "Reservation is not confirmed or already handed over.", 409),
        "VISIT_INVALID_STATUS" => new(code, "Reservation Visit must be CHECKED_IN.", 409),
        "UNIT_FACILITY_TYPE_MISMATCH" => new(code, "Unit does not match Facility and UnitType.", 409),
        "UNIT_NOT_AVAILABLE" => new(code, "Unit is not AVAILABLE.", 409),
        "HANDOVER_UNIT_ALREADY_ASSIGNED" => new(code, "Unit already assigned to another Reservation.", 409),
        _ => null
    };
}
