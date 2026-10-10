using Frms.Business.Models.Results;
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
    private async Task<Visit> CheckOutCoreAsync(
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

    if (employee?.FacilityId is null)
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
            "Only an ACCESS Visit may be checked out through this operation.",
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

    await facilityAuthorizationService.EnsureSameFacilityAsync(
        contract.FacilityId,
        cancellationToken);

    try
    {
        return await repository.CheckOutAsync(
            visitId,
            cancellationToken);
    }
    catch (StoredProcedureBusinessException ex)
        when (ex.Code == "VISIT_INVALID_STATUS")
    {
        throw new BusinessException(
            "VISIT_INVALID_STATUS",
            "Visit cannot be checked out from its current state.",
            409);
    }
}
    private async Task<Visit> CreateAccessCoreAsync(
        Guid contractId,
        DateOnly visitDate,
        CancellationToken cancellationToken = default) {
        var customerId =
            await GetCustomerIdAsync(cancellationToken);

        var contract =
            await repository.GetOwnedContractByIdAsync(
                customerId,
                contractId,
                cancellationToken);

        if (contract is null) {
            throw new BusinessException(
                "RESOURCE_NOT_FOUND",
                "Contract was not found.",
                404);
        }

        if (contract.Status != "ACTIVE") {
            throw new BusinessException(
                "CONTRACT_NOT_ACTIVE",
                "ACCESS Visit requires an active Contract.",
                409);
        }

        ValidateAccessVisitDate(
            contract,
            visitDate);

        if (await repository.HasPendingReturnVisitAsync(
                contractId,
                cancellationToken)) {
            throw new BusinessException(
                "RETURN_VISIT_PENDING",
                "A pending RETURN Visit blocks ACCESS Visit creation.",
                409);
        }

        try {
            return await repository.CreateAccessAsync(
                contractId,
                customerId,
                visitDate,
                cancellationToken);
        }
        catch (StoredProcedureBusinessException ex) {
            switch (ex.Code) {
                case "CONTRACT_NOT_ACTIVE_OR_NOT_OWNED":
                    throw new BusinessException(
                        "CONTRACT_NOT_ACTIVE",
                        "ACCESS Visit requires an active Contract.",
                        409);

                case "RETURN_VISIT_PENDING":
                    throw new BusinessException(
                        ex.Code,
                        "A pending RETURN Visit blocks ACCESS Visit creation.",
                        409);

                case "ACCESS_VISIT_DATE_OUTSIDE_CONTRACT":
                    throw new BusinessException(
                        ex.Code,
                        "ACCESS Visit date must be within the Contract period.",
                        400);

                case "VISIT_DATE_IN_PAST":
                    throw new BusinessException(
                        ex.Code,
                        "ACCESS Visit date cannot be in the past.",
                        400);

                default:
                    throw;
            }
        }
    }
    private void ValidateAccessVisitDate(
        Contract contract,
        DateOnly visitDate) {
        var startDate = new DateOnly(
            contract.StartMonth.Year,
            contract.StartMonth.Month,
            1);

        var endDate = new DateOnly(
            contract.EndMonth.Year,
            contract.EndMonth.Month,
            DateTime.DaysInMonth(
                contract.EndMonth.Year,
                contract.EndMonth.Month));

        if (visitDate < startDate || visitDate > endDate) {
            throw new BusinessException(
                "ACCESS_VISIT_DATE_OUTSIDE_CONTRACT",
                "ACCESS Visit date must be within the Contract period.",
                400);
        }

        var businessToday = DateOnly.FromDateTime(
            clock.ToBusinessTime(clock.UtcNow).Date);

        if (visitDate < businessToday) {
            throw new BusinessException(
                "VISIT_DATE_IN_PAST",
                "ACCESS Visit date cannot be in the past.",
                400);
        }
    }

    private async Task<(IReadOnlyList<Visit> Items, int TotalItems)> ListAccessibleCoreAsync(
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
                return await ListOwnCoreAsync(
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

    private async Task<(IReadOnlyList<Visit> Items, int TotalItems)> ListOwnCoreAsync(
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

    private async Task<Visit> GetOwnCoreAsync(
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

    private async Task<Visit> RescheduleCoreAsync(
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

    private async Task<Visit> CancelCoreAsync(
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

    private async Task<Visit> CheckInCoreAsync(
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
        else if (visit.VisitType is "ACCESS" or "RETURN")
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

    private async Task<Visit> GetByIdCoreAsync(
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
                return await GetOwnCoreAsync(
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

    public async Task<(IReadOnlyList<VisitResult> Items, int TotalItems)> ListOwnAsync(int page, int pageSize, CancellationToken cancellationToken = default)
    {
        var row = await ListOwnCoreAsync(page, pageSize, cancellationToken);
        return (row.Items.Select(ServiceResultProjection.Map).ToArray(), row.TotalItems);
    }

    public async Task<(IReadOnlyList<VisitResult> Items, int TotalItems)> ListAccessibleAsync(int page, int pageSize, CancellationToken cancellationToken = default)
    {
        var row = await ListAccessibleCoreAsync(page, pageSize, cancellationToken);
        return (row.Items.Select(ServiceResultProjection.Map).ToArray(), row.TotalItems);
    }

    public async Task<VisitResult> GetOwnAsync(Guid visitId, CancellationToken cancellationToken = default)
    {
        return ServiceResultProjection.Map(await GetOwnCoreAsync(visitId, cancellationToken));
    }

    public async Task<VisitResult> GetByIdAsync(Guid visitId, CancellationToken cancellationToken = default)
    {
        return ServiceResultProjection.Map(await GetByIdCoreAsync(visitId, cancellationToken));
    }

    public async Task<VisitResult> CheckInAsync(Guid visitId, CancellationToken cancellationToken = default)
    {
        return ServiceResultProjection.Map(await CheckInCoreAsync(visitId, cancellationToken));
    }

    public async Task<VisitResult> CheckOutAsync(Guid visitId, CancellationToken cancellationToken = default)
    {
        return ServiceResultProjection.Map(await CheckOutCoreAsync(visitId, cancellationToken));
    }

    public async Task<VisitResult> RescheduleAsync(Guid visitId, DateOnly visitDate, CancellationToken cancellationToken = default)
    {
        return ServiceResultProjection.Map(await RescheduleCoreAsync(visitId, visitDate, cancellationToken));
    }

    public async Task<VisitResult> CancelAsync(Guid visitId, string reason, CancellationToken cancellationToken = default)
    {
        return ServiceResultProjection.Map(await CancelCoreAsync(visitId, reason, cancellationToken));
    }

    public async Task<VisitResult> CreateAccessAsync(Guid contractId, DateOnly visitDate, CancellationToken cancellationToken = default)
    {
        return ServiceResultProjection.Map(await CreateAccessCoreAsync(contractId, visitDate, cancellationToken));
    }
}
