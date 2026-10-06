using Frms.Business.Abstractions.Security;
using Frms.Business.Exceptions;
using Frms.Business.Models.Commands;
using Frms.Business.Services.Interfaces;
using Frms.DataAccess.Repositories.Interfaces;
using Frms.DataAccess.Repositories.Models;
using Frms.DataAccess.StoredProcedures;

namespace Frms.Business.Services.Implementations;

internal sealed class ReservationService(
    IReservationRepository repository,
    ICurrentUserContext currentUser)
    : IReservationService
{
    public async Task<CreatedReservationRecord> CreateAsync(
        CreateReservationCommand command,
        CancellationToken cancellationToken = default)
    {
        if (!currentUser.IsAuthenticated)
        {
            throw new BusinessException(
                "UNAUTHORIZED",
                "Authentication is required.",
                401);
        }

        var customerId =
            await repository.GetCustomerIdByUserAccountIdAsync(
                currentUser.UserAccountId,
                cancellationToken);

        if (customerId is null)
        {
            throw new BusinessException(
                "RESOURCE_NOT_FOUND",
                "Customer profile was not found.",
                404);
        }

        try
        {
            return await repository.CreateAsync(
                customerId.Value,
                command.FacilityId,
                command.UnitTypeId,
                command.StartMonth,
                command.EndMonth,
                cancellationToken);
        }
        catch (StoredProcedureBusinessException ex)
        {
            switch (ex.Code)
            {
                case "INVALID_MONTH_RANGE":
                    throw new BusinessException(
                        ex.Code,
                        "The requested month range is invalid.",
                        400);

                case "ACCOUNT_INACTIVE":
                    throw new BusinessException(
                        ex.Code,
                        "The account is inactive.",
                        403);

                case "FACILITY_INACTIVE":
                    throw new BusinessException(
                        ex.Code,
                        "The facility is inactive.",
                        409);

                case "RESOURCE_NOT_FOUND":
                    throw new BusinessException(
                        ex.Code,
                        "The requested resource was not found.",
                        404);

                case "CAPACITY_NOT_AVAILABLE":
                    throw new BusinessException(
                        ex.Code,
                        "Storage capacity is not available for the requested period.",
                        409);

                default:
                    throw;
            }
        }
    }

    public async Task<ConfirmedReservationRecord> ConfirmAsync(
        ConfirmReservationCommand command,
        CancellationToken cancellationToken = default)
    {
        if (!currentUser.IsAuthenticated)
        {
            throw new BusinessException(
                "UNAUTHORIZED",
                "Authentication is required.",
                401);
        }

        var customerId =
            await repository.GetCustomerIdByUserAccountIdAsync(
                currentUser.UserAccountId,
                cancellationToken);

        if (customerId is null)
        {
            throw new BusinessException(
                "RESOURCE_NOT_FOUND",
                "Customer profile was not found.",
                404);
        }

        try
        {
            return await repository.ConfirmAsync(
                customerId.Value,
                command.ReservationId,
                command.ReservationVisitDate,
                cancellationToken);
        }
        catch (StoredProcedureBusinessException ex)
        {
            switch (ex.Code)
            {
                case "RESOURCE_NOT_FOUND":
                    throw new BusinessException(
                        ex.Code,
                        "Reservation was not found.",
                        404);

                case "DEPOSIT_NOT_PAID":
                    throw new BusinessException(
                        ex.Code,
                        "Reservation deposit has not been paid.",
                        409);

                case "VISIT_DATE_OUT_OF_POLICY":
                    throw new BusinessException(
                        ex.Code,
                        "Reservation Visit date is outside the captured Policy window.",
                        400);

                case "RESERVATION_INVALID_STATUS":
                    throw new BusinessException(
                        ex.Code,
                        "Reservation cannot be confirmed from its current status.",
                        409);

                default:
                    throw;
            }
        }
    }
}