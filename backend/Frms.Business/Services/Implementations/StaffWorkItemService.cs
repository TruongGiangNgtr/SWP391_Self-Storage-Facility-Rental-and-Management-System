using Frms.Business.Abstractions.Security;
using Frms.Business.Abstractions.Time;
using Frms.Business.Exceptions;
using Frms.Business.Models.Results;
using Frms.Business.Services.Interfaces;
using Frms.DataAccess.Repositories.Interfaces;

namespace Frms.Business.Services.Implementations;

internal sealed class StaffWorkItemService(
    IStaffWorkItemRepository repository,
    IFacilityAuthorizationService facilityAuthorization,
    ICurrentUserContext currentUser,
    IClock clock) : IStaffWorkItemService {
    public async Task<StaffWorkItemPageResult> ListAsync(
        DateOnly? date,
        int page,
        int pageSize,
        CancellationToken cancellationToken = default) {
        if (page < 1 || pageSize is < 1 or > 100) {
            throw new BusinessException(
                "INVALID_PAGINATION",
                "Page must be at least 1 and pageSize must be between 1 and 100.",
                400);
        }

        if (!currentUser.IsAuthenticated) {
            throw new BusinessException(
                "UNAUTHORIZED",
                "Authentication is required.",
                401);
        }

        if (currentUser.Role != "FACILITY_STAFF") {
            throw new BusinessException(
                "FORBIDDEN",
                "Only Facility Staff can access work items.",
                403);
        }

        // Facility is resolved from authenticated account,
        // never from client input.
        var facilityId =
            await facilityAuthorization.GetAssignedFacilityIdAsync(
                cancellationToken);

        var employeeId = await repository.GetEmployeeIdAsync(
            currentUser.UserAccountId,
            facilityId,
            cancellationToken);

        if (employeeId is null) {
            throw new BusinessException(
                "FORBIDDEN",
                "Facility Staff assignment was not found.",
                403);
        }

        var businessToday = DateOnly.FromDateTime(
            clock.ToBusinessTime(clock.UtcNow).Date);

        var selectedDate = date ?? businessToday;

        var (items, totalItems) =
            await repository.GetWorkItemsAsync(
                facilityId,
                employeeId.Value,
                selectedDate,
                page,
                pageSize,
                cancellationToken);

        return new StaffWorkItemPageResult(
            items.Select(x => new StaffWorkItemResult(
                x.WorkType,
                x.ReferenceId,
                x.EntityId,
                x.ScheduledDate,
                x.Status,
                x.CustomerId,
                x.CustomerName,
                x.CustomerPhone
            )).ToArray(),
            page,
            pageSize,
            totalItems);
    }
}
