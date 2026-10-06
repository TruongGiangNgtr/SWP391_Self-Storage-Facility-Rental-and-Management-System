using Frms.Business.Abstractions.Security;
using Frms.Business.Exceptions;
using Frms.Business.Services.Interfaces;
using Frms.DataAccess.Persistence.Entities;
using Frms.DataAccess.Repositories.Interfaces;
using Frms.DataAccess.StoredProcedures;
using Frms.Business.Abstractions.Time;

namespace Frms.Business.Services.Implementations;

internal sealed class VisitService(
    IVisitRepository repository,
    ICurrentUserContext currentUser,
    IFacilityAuthorizationService facilityAuthorizationService,
    IClock clock)
    : IVisitService
{
    public async Task<(IReadOnlyList<Visit> Items, int TotalItems)> ListOwnAsync(
        int page,
        int pageSize,
        CancellationToken cancellationToken = default)
    {
        if (page < 1 || pageSize < 1 || pageSize > 100)
        {
            throw new BusinessException(
                "VALIDATION_ERROR",
                "Page must be at least 1 and pageSize must be between 1 and 100.",
                400);
        }

        var customerId = await GetCustomerIdAsync(cancellationToken);

        var totalItems = await repository.CountOwnedAsync(
            customerId,
            cancellationToken);

        var items = await repository.ListOwnedAsync(
            customerId,
            (page - 1) * pageSize,
            pageSize,
            cancellationToken);

        return (items, totalItems);
    }

    public async Task<Visit> GetOwnAsync(
        Guid visitId,
        CancellationToken cancellationToken = default)
    {
        var customerId = await GetCustomerIdAsync(cancellationToken);

        var visit = await repository.GetOwnedByIdAsync(
            customerId,
            visitId,
            cancellationToken);

        if (visit is null)
        {
            throw new BusinessException(
                "RESOURCE_NOT_FOUND",
                "Visit was not found.",
                404);
        }

        return visit;
    }

    public async Task<Visit> RescheduleAsync(
        Guid visitId,
        DateOnly visitDate,
        CancellationToken cancellationToken = default)
    {
        var customerId = await GetCustomerIdAsync(cancellationToken);

        var visit = await repository.GetOwnedByIdAsync(
            customerId,
            visitId,
            cancellationToken);

        if (visit is null)
        {
            throw new BusinessException(
                "RESOURCE_NOT_FOUND",
                "Visit was not found.",
                404);
        }

        if (visit.Status != "SCHEDULED")
        {
            throw new BusinessException(
                "VISIT_INVALID_STATUS",
                "Only a scheduled Visit may be rescheduled.",
                409);
        }

        // CWP-05 owns RESERVATION Visit scheduling rules.
        // ACCESS / RETURN type-specific rules are extended in their owning features.
        if (visit.VisitType == "RESERVATION")
        {
            var reservation =
                await repository.GetOwnedReservationForVisitAsync(
                    customerId,
                    visitId,
                    cancellationToken);

            if (reservation is null)
            {
                throw new BusinessException(
                    "VISIT_ENTITY_MISMATCH",
                    "Visit does not reference a valid Reservation.",
                    409);
            }

            var policy = await repository.GetPolicyAsync(
                reservation.PolicyId,
                cancellationToken);

            if (policy is null)
            {
                throw new BusinessException(
                    "RESOURCE_NOT_FOUND",
                    "Reservation policy was not found.",
                    404);
            }

            var daysInMonth = DateTime.DaysInMonth(
                reservation.StartMonth.Year,
                reservation.StartMonth.Month);

            var startDay = Math.Min(
                policy.ReservationVisitStartDay,
                daysInMonth);

            var endDay = Math.Min(
                policy.ReservationVisitEndDay,
                daysInMonth);

            var allowedFrom = new DateOnly(
                reservation.StartMonth.Year,
                reservation.StartMonth.Month,
                startDay);

            var allowedTo = new DateOnly(
                reservation.StartMonth.Year,
                reservation.StartMonth.Month,
                endDay);

            if (visitDate < allowedFrom || visitDate > allowedTo)
            {
                throw new BusinessException(
                    "VISIT_DATE_OUT_OF_POLICY",
                    "Reservation Visit date is outside the captured Policy window.",
                    400);
            }
        }

        if (visit.VisitType == "ACCESS")
        {
            var contract =
                await repository.GetOwnedContractForVisitAsync(
                    customerId,
                    visitId,
                    cancellationToken);

            if (contract is null)
            {
                throw new BusinessException(
                    "VISIT_ENTITY_MISMATCH",
                    "ACCESS Visit does not reference a valid Contract.",
                    409);
            }

            if (contract.Status != "ACTIVE")
            {
                throw new BusinessException(
                    "CONTRACT_NOT_ACTIVE",
                    "ACCESS Visit requires an active Contract.",
                    409);
            }

            ValidateAccessVisitDate(
                contract,
                visitDate);

            if (await repository.HasPendingReturnVisitAsync(
                    contract.ContractId,
                    cancellationToken))
            {
                throw new BusinessException(
                    "RETURN_VISIT_PENDING",
                    "A pending RETURN Visit blocks ACCESS Visit rescheduling.",
                    409);
            }
        }

        await repository.UpdateVisitDateAsync(
            visitId,
            visitDate,
            cancellationToken);

        return await GetOwnedRequiredAsync(
            customerId,
            visitId,
            cancellationToken);
    }

    public async Task<Visit> CancelAsync(
        Guid visitId,
        string reason,
        CancellationToken cancellationToken = default)
    {
        var customerId = await GetCustomerIdAsync(cancellationToken);

        if (string.IsNullOrWhiteSpace(reason))
        {
            throw new BusinessException(
                "VALIDATION_ERROR",
                "Cancellation reason is required.",
                400);
        }

        try
        {
            return await repository.CancelAsync(
                customerId,
                visitId,
                reason.Trim(),
                cancellationToken);
        }
        catch (StoredProcedureBusinessException ex)
        {
            switch (ex.Code)
            {
                case "RESOURCE_NOT_FOUND":
                    throw new BusinessException(
                        ex.Code,
                        "Visit was not found.",
                        404);

                case "VISIT_INVALID_STATUS":
                    throw new BusinessException(
                        ex.Code,
                        "Only a scheduled Visit may be cancelled.",
                        409);

                default:
                    throw;
            }
        }
    }

    private async Task<Guid> GetCustomerIdAsync(
        CancellationToken cancellationToken)
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

        return customerId.Value;
    }

    private async Task<Visit> GetOwnedRequiredAsync(
        Guid customerId,
        Guid visitId,
        CancellationToken cancellationToken)
    {
        return await repository.GetOwnedByIdAsync(
                   customerId,
                   visitId,
                   cancellationToken)
               ?? throw new BusinessException(
                   "RESOURCE_NOT_FOUND",
                   "Visit was not found.",
                   404);
    }

    public async Task<Visit> CheckInAsync(
        Guid visitId,
        CancellationToken cancellationToken = default)
    {
        if (!currentUser.IsAuthenticated)
        {
            throw new BusinessException(
                "UNAUTHORIZED",
                "Authentication is required.",
                401);
        }

        var employee =
            await repository.GetEmployeeByUserAccountIdAsync(
                currentUser.UserAccountId,
                cancellationToken);

        if (employee is null ||
            employee.FacilityId is null)
        {
            throw new BusinessException(
                "FORBIDDEN",
                "Facility Staff profile is not available.",
                403);
        }

        var visit =
            await repository.GetByIdAsync(
                visitId,
                cancellationToken);

        if (visit is null)
        {
            throw new BusinessException(
                "RESOURCE_NOT_FOUND",
                "Visit was not found.",
                404);
        }

        if (visit.Status != "SCHEDULED")
        {
            throw new BusinessException(
                "VISIT_INVALID_STATUS",
                "Only a scheduled Visit may be checked in.",
                409);
        }

        if (visit.VisitType == "RESERVATION")
        {
            var reservation =
                await repository.GetReservationForVisitAsync(
                    visitId,
                    cancellationToken);

            if (reservation is null)
            {
                throw new BusinessException(
                    "VISIT_ENTITY_MISMATCH",
                    "Visit does not reference a valid Reservation.",
                    409);
            }

            if (reservation.Status != "CONFIRMED")
            {
                throw new BusinessException(
                    "VISIT_INVALID_STATUS",
                    "Reservation is not in a valid state for check-in.",
                    409);
            }

            await facilityAuthorizationService.EnsureSameFacilityAsync(
                reservation.FacilityId,
                cancellationToken);
        }
        else if (visit.VisitType == "ACCESS")
        {
            var contract =
                await repository.GetContractForVisitAsync(
                    visitId,
                    cancellationToken);

            if (contract is null)
            {
                throw new BusinessException(
                    "VISIT_ENTITY_MISMATCH",
                    "ACCESS Visit does not reference a valid Contract.",
                    409);
            }

            if (contract.Status != "ACTIVE")
            {
                throw new BusinessException(
                    "VISIT_INVALID_STATUS",
                    "ACCESS Visit requires an active Contract.",
                    409);
            }

            await facilityAuthorizationService.EnsureSameFacilityAsync(
                contract.FacilityId,
                cancellationToken);
        }
        else
        {
            // RETURN is owned by FWP-05.
            throw new BusinessException(
                "VISIT_ENTITY_MISMATCH",
                "Visit type is not supported by this operation.",
                409);
        }

        try
        {
            return await repository.CheckInAsync(
                visitId,
                employee.EmployeeId,
                cancellationToken);
        }
        catch (StoredProcedureBusinessException ex)
        {
            switch (ex.Code)
            {
                case "VISIT_INVALID_STATUS":
                    throw new BusinessException(
                        ex.Code,
                        "Visit cannot be checked in from its current state.",
                        409);

                case "VISIT_ENTITY_MISMATCH":
                    throw new BusinessException(
                        ex.Code,
                        "Visit does not reference the expected business entity.",
                        409);

                case "FORBIDDEN":
                    throw new BusinessException(
                        ex.Code,
                        "You are not authorized to handle this Visit.",
                        403);

                default:
                    throw;
            }
        }
    }

    public async Task<Visit> CreateAccessAsync(
        Guid contractId,
        DateOnly visitDate,
        CancellationToken cancellationToken = default)
    {
        var customerId =
            await GetCustomerIdAsync(cancellationToken);

        var contract =
            await repository.GetOwnedContractAsync(
                customerId,
                contractId,
                cancellationToken);

        if (contract is null)
        {
            throw new BusinessException(
                "RESOURCE_NOT_FOUND",
                "Contract was not found.",
                404);
        }

        if (contract.Status != "ACTIVE")
        {
            throw new BusinessException(
                "CONTRACT_NOT_ACTIVE",
                "Only an active Contract may create an ACCESS Visit.",
                409);
        }

        ValidateAccessVisitDate(
            contract,
            visitDate);

        if (await repository.HasPendingReturnVisitAsync(
                contract.ContractId,
                cancellationToken))
        {
            throw new BusinessException(
                "RETURN_VISIT_PENDING",
                "A pending RETURN Visit blocks new ACCESS Visits.",
                409);
        }

        try
        {
            return await repository.CreateAccessAsync(
                contract.ContractId,
                visitDate,
                cancellationToken);
        }
        catch (StoredProcedureBusinessException ex)
        {
            switch (ex.Code)
            {
                case "CONTRACT_NOT_ACTIVE":
                    throw new BusinessException(
                        ex.Code,
                        "Only an active Contract may create an ACCESS Visit.",
                        409);

                case "RETURN_VISIT_PENDING":
                    throw new BusinessException(
                        ex.Code,
                        "A pending RETURN Visit blocks new ACCESS Visits.",
                        409);

                case "RESOURCE_NOT_FOUND":
                    throw new BusinessException(
                        ex.Code,
                        "Contract was not found.",
                        404);

                default:
                    throw;
            }
        }
    }

    private void ValidateAccessVisitDate(
        Contract contract,
        DateOnly visitDate)
    {
        var businessNow =
            clock.ToBusinessTime(clock.UtcNow);

        var today =
            DateOnly.FromDateTime(
                businessNow.DateTime);

        if (visitDate < today)
        {
            throw new BusinessException(
                "VALIDATION_ERROR",
                "ACCESS Visit date cannot be in the past.",
                400);
        }

        var contractStart =
            new DateOnly(
                contract.StartMonth.Year,
                contract.StartMonth.Month,
                1);

        var contractEnd =
            new DateOnly(
                contract.EndMonth.Year,
                contract.EndMonth.Month,
                DateTime.DaysInMonth(
                    contract.EndMonth.Year,
                    contract.EndMonth.Month));

        if (visitDate < contractStart ||
            visitDate > contractEnd)
        {
            throw new BusinessException(
                "VALIDATION_ERROR",
                "ACCESS Visit date must be inside the Contract period.",
                400);
        }
    }

    public async Task<Visit> CheckOutAsync(
        Guid visitId,
        CancellationToken cancellationToken = default)
    {
        if (!currentUser.IsAuthenticated)
        {
            throw new BusinessException(
                "UNAUTHORIZED",
                "Authentication is required.",
                401);
        }

        var employee =
            await repository.GetEmployeeByUserAccountIdAsync(
                currentUser.UserAccountId,
                cancellationToken);

        if (employee is null ||
            employee.FacilityId is null)
        {
            throw new BusinessException(
                "FORBIDDEN",
                "Facility Staff profile is not available.",
                403);
        }

        var visit =
            await repository.GetByIdAsync(
                visitId,
                cancellationToken);

        if (visit is null)
        {
            throw new BusinessException(
                "RESOURCE_NOT_FOUND",
                "Visit was not found.",
                404);
        }

        if (visit.VisitType != "ACCESS")
        {
            throw new BusinessException(
                "VISIT_ENTITY_MISMATCH",
                "Only an ACCESS Visit may use this check-out operation.",
                409);
        }

        if (visit.Status != "CHECKED_IN")
        {
            throw new BusinessException(
                "VISIT_INVALID_STATUS",
                "Only a checked-in ACCESS Visit may be checked out.",
                409);
        }

        var contract =
            await repository.GetContractForVisitAsync(
                visitId,
                cancellationToken);

        if (contract is null)
        {
            throw new BusinessException(
                "VISIT_ENTITY_MISMATCH",
                "ACCESS Visit does not reference a valid Contract.",
                409);
        }

        if (contract.Status != "ACTIVE")
        {
            throw new BusinessException(
                "VISIT_INVALID_STATUS",
                "ACCESS Visit requires an active Contract.",
                409);
        }

        await facilityAuthorizationService.EnsureSameFacilityAsync(
            contract.FacilityId,
            cancellationToken);

        var succeeded =
            await repository.TryCheckOutAccessAsync(
                visitId,
                cancellationToken);

        if (!succeeded)
        {
            throw new BusinessException(
                "VISIT_INVALID_STATUS",
                "ACCESS Visit could not be checked out from its current state.",
                409);
        }

        return await repository.GetByIdAsync(
                visitId,
                cancellationToken)
            ?? throw new BusinessException(
                "RESOURCE_NOT_FOUND",
                "Visit was not found.",
                404);
    }
}