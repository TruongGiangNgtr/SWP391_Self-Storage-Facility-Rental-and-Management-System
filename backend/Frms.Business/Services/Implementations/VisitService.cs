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
    public async Task<(IReadOnlyList<Visit> Items, int TotalItems)>
    ListAccessibleAsync(
        int page,
        int pageSize,
        CancellationToken cancellationToken = default) {
        if (page < 1 || pageSize < 1 || pageSize > 100) {
            throw new BusinessException(
                "VALIDATION_ERROR",
                "Page must be at least 1 and pageSize must be between 1 and 100.",
                400);
        }

        if (!currentUser.IsAuthenticated) {
            throw new BusinessException(
                "UNAUTHORIZED",
                "Authentication is required.",
                401);
        }

        var skip = (page - 1) * pageSize;

        switch (currentUser.Role) {
            case "CUSTOMER":
                return await ListOwnAsync(
                    page,
                    pageSize,
                    cancellationToken);

            case "FACILITY_MANAGER": {
                var employee =
                    await repository.GetEmployeeByUserAccountIdAsync(
                        currentUser.UserAccountId,
                        cancellationToken);

                if (employee?.FacilityId is null) {
                    throw new BusinessException(
                        "FORBIDDEN",
                        "Facility Manager assignment is not available.",
                        403);
                }

                var totalItems =
                    await repository.CountByFacilityAsync(
                        employee.FacilityId.Value,
                        cancellationToken);

                var items =
                    await repository.ListByFacilityAsync(
                        employee.FacilityId.Value,
                        skip,
                        pageSize,
                        cancellationToken);

                return (items, totalItems);
            }

            case "BUSINESS_OPERATIONS_MANAGER": {
                var totalItems =
                    await repository.CountAllAsync(
                        cancellationToken);

                var items =
                    await repository.ListAllAsync(
                        skip,
                        pageSize,
                        cancellationToken);

                return (items, totalItems);
            }

            default:
                throw new BusinessException(
                    "FORBIDDEN",
                    "You are not authorized to list Visits.",
                    403);
        }
    }

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

    public async Task<Visit> GetByIdAsync(
        Guid visitId,
        CancellationToken cancellationToken = default)
    {
        if (!currentUser.IsAuthenticated) {
            throw new BusinessException(
                "UNAUTHORIZED",
                "Authentication is required.",
                401);
        }

        switch (currentUser.Role) {
            case "CUSTOMER":
                return await GetOwnAsync(
                    visitId,
                    cancellationToken);

            case "FACILITY_STAFF":
            case "FACILITY_MANAGER": {
                var visit = await GetRequiredByIdAsync(
                    visitId,
                    cancellationToken);

                var facilityId = await ResolveVisitFacilityIdAsync(
                    visit,
                    cancellationToken);

                await facilityAuthorizationService.EnsureSameFacilityAsync(
                    facilityId,
                    cancellationToken);

                return visit;
            }

            case "BUSINESS_OPERATIONS_MANAGER":
                return await GetRequiredByIdAsync(
                    visitId,
                    cancellationToken);

            default:
                throw new BusinessException(
                    "FORBIDDEN",
                    "You are not authorized to access this Visit.",
                    403);
        }
    }

    private async Task<Visit> GetRequiredByIdAsync(
        Guid visitId,
        CancellationToken cancellationToken)
    {
        return await repository.GetByIdAsync(
                   visitId,
                   cancellationToken)
               ?? throw new BusinessException(
                   "RESOURCE_NOT_FOUND",
                   "Visit was not found.",
                   404);
    }

    private async Task<Guid> ResolveVisitFacilityIdAsync(
        Visit visit,
        CancellationToken cancellationToken)
    {
        switch (visit.VisitType) {
            case "RESERVATION": {
                var reservation =
                    await repository.GetReservationForVisitAsync(
                        visit.VisitId,
                        cancellationToken);

                if (reservation is null) {
                    throw new BusinessException(
                        "RESOURCE_NOT_FOUND",
                        "Referenced Reservation was not found.",
                        404);
                }

                return reservation.FacilityId;
            }

            case "ACCESS":
            case "RETURN": {
                var contract =
                    await repository.GetContractForVisitAsync(
                        visit.VisitId,
                        cancellationToken);

                if (contract is null) {
                    throw new BusinessException(
                        "RESOURCE_NOT_FOUND",
                        "Referenced Contract was not found.",
                        404);
                }

                return contract.FacilityId;
            }

            default:
                throw new BusinessException(
                    "RESOURCE_NOT_FOUND",
                    "Visit referenced resource was not found.",
                    404);
        }
    }
}
