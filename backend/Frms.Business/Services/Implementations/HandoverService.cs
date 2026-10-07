using Frms.Business.Exceptions;
using Frms.Business.Models.Commands;
using Frms.Business.Services.Interfaces;
using Frms.DataAccess.Repositories.Interfaces;
using Frms.DataAccess.Repositories.Models;
using Frms.DataAccess.StoredProcedures;

namespace Frms.Business.Services.Implementations;

internal sealed class HandoverService(
    IHandoverRepository repository,
    IFacilityAuthorizationService facilityAuthorizationService)
    : IHandoverService
{
    public async Task<CompletedHandoverRecord> CompleteAsync(
        CompleteHandoverCommand command,
        CancellationToken cancellationToken = default)
    {
        if (command.ReservationId == Guid.Empty ||
            command.VisitId == Guid.Empty ||
            command.StorageUnitId == Guid.Empty ||
            command.FirstMonthPaymentId == Guid.Empty)
        {
            throw new BusinessException(
                "VALIDATION_ERROR",
                "Reservation, Visit, StorageUnit and Payment identifiers are required.",
                400);
        }

        var reservation =
            await repository.GetReservationAsync(
                command.ReservationId,
                cancellationToken);

        if (reservation is null)
        {
            throw new BusinessException(
                "RESOURCE_NOT_FOUND",
                "Reservation was not found.",
                404);
        }

        await facilityAuthorizationService.EnsureSameFacilityAsync(
            reservation.FacilityId,
            cancellationToken);

        try
        {
            return await repository.CompleteAsync(
                command.ReservationId,
                command.VisitId,
                command.StorageUnitId,
                command.FirstMonthPaymentId,
                command.DiscountId,
                cancellationToken);
        }
        catch (StoredProcedureBusinessException ex)
        {
            switch (ex.Code)
            {
                case "RESERVATION_INVALID_STATUS":
                    throw new BusinessException(
                        ex.Code,
                        "Reservation cannot complete handover from its current status.",
                        409);

                case "VISIT_INVALID_STATUS":
                    throw new BusinessException(
                        ex.Code,
                        "Visit is not in a valid state for handover.",
                        409);

                case "UNIT_NOT_AVAILABLE":
                    throw new BusinessException(
                        ex.Code,
                        "The selected StorageUnit is not available.",
                        409);

                case "UNIT_FACILITY_TYPE_MISMATCH":
                    throw new BusinessException(
                        ex.Code,
                        "The selected StorageUnit does not match the Reservation Facility and Unit Type.",
                        409);

                case "FIRST_MONTH_PAYMENT_NOT_SUCCESS":
                    throw new BusinessException(
                        ex.Code,
                        "The first-month payment has not succeeded.",
                        409);

                case "DISCOUNT_NOT_OWNED_BY_CUSTOMER":
                    throw new BusinessException(
                        ex.Code,
                        "The selected Discount does not belong to the Customer.",
                        409);

                case "DISCOUNT_NOT_VALID":
                    throw new BusinessException(
                        ex.Code,
                        "The selected Discount is not valid.",
                        409);

                case "FORBIDDEN":
                    throw new BusinessException(
                        ex.Code,
                        "You are not authorized to complete this handover.",
                        403);

                case "RESOURCE_NOT_FOUND":
                    throw new BusinessException(
                        ex.Code,
                        "A required handover resource was not found.",
                        404);

                default:
                    throw;
            }
        }
    }
}