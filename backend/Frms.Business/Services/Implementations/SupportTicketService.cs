using Frms.Business.Abstractions.Security;
using Frms.Business.Exceptions;
using Frms.Business.Services.Interfaces;
using Frms.DataAccess.Persistence.Entities;
using Frms.DataAccess.Repositories.Interfaces;
using Frms.DataAccess.StoredProcedures;

namespace Frms.Business.Services.Implementations;
internal sealed class SupportTicketService(
    ISupportTicketRepository repository,
    ICurrentUserContext currentUser,
    IFacilityAuthorizationService facilityAuthorization) : ISupportTicketService
{
    public async Task<(IReadOnlyList<SupportTicket> Items, int Total)> ListAccessibleAsync(
        int page, int pageSize, CancellationToken ct)
    {
        EnsureAuthenticated();
        if (page < 1 || pageSize < 1 || pageSize > 100)
            throw new BusinessException("VALIDATION_ERROR", "Invalid page or pageSize.", 400);
        var (customerId, facilityId, staffId) = await GetScopeAsync(ct);
        var total = await repository.CountAsync(customerId, facilityId, staffId, ct);
        var items = await repository.ListAsync(customerId, facilityId, staffId,
            (page - 1) * pageSize, pageSize, ct);
        return (items, total);
    }

    public async Task<SupportTicket> GetAccessibleAsync(Guid ticketId, CancellationToken ct)
    {
        EnsureAuthenticated();
        var record = await repository.GetAsync(ticketId, ct)
            ?? throw new BusinessException("RESOURCE_NOT_FOUND", "SupportTicket not found.", 404);
        switch (currentUser.Role)
        {
            case "CUSTOMER":
                var customerId = await repository.GetCustomerIdAsync(currentUser.UserAccountId, ct);
                if (!customerId.HasValue || record.Ticket.CustomerId != customerId.Value) throw Forbidden();
                break;
            case "FACILITY_MANAGER":
                await facilityAuthorization.EnsureSameFacilityAsync(record.FacilityId, ct);
                break;
            case "FACILITY_STAFF":
                await facilityAuthorization.EnsureSameFacilityAsync(record.FacilityId, ct);
                var employee = await repository.GetEmployeeAsync(currentUser.UserAccountId, ct);
                if (employee is null || record.Ticket.AssignedEmployeeId != employee.EmployeeId) throw Forbidden();
                break;
            default: throw Forbidden();
        }
        return record.Ticket;
    }

    public async Task<SupportTicket> AssignAsync(Guid ticketId, Guid staffEmployeeId, CancellationToken ct)
    {
        EnsureAuthenticated();
        if (currentUser.Role != "FACILITY_MANAGER") throw Forbidden();
        if (staffEmployeeId == Guid.Empty)
            throw new BusinessException("VALIDATION_ERROR", "employeeId is required.", 400);
        var record = await repository.GetAsync(ticketId, ct)
            ?? throw new BusinessException("RESOURCE_NOT_FOUND", "SupportTicket not found.", 404);
        await facilityAuthorization.EnsureSameFacilityAsync(record.FacilityId, ct);
        var manager = await repository.GetEmployeeAsync(currentUser.UserAccountId, ct);
        if (manager is null) throw Forbidden();
        if (record.Ticket.Status != "OPEN")
            throw new BusinessException("SUPPORT_TICKET_INVALID_STATUS", "SupportTicket must be OPEN.", 409);
        try
        {
            await repository.AssignAsync(ticketId, manager.EmployeeId, staffEmployeeId, ct);
        }
        catch (StoredProcedureBusinessException ex)
        {
            var status = ex.Code switch
            {
                "SUPPORT_TICKET_INVALID_STATUS" => 409,
                "FACILITY_MANAGER_SCOPE_INVALID" => 403,
                "STAFF_FACILITY_OR_ROLE_INVALID" => 403,
                _ => 409
            };
            throw new BusinessException(ex.Code, ex.Message, status);
        }
        return (await repository.GetAsync(ticketId, ct))?.Ticket
            ?? throw new BusinessException("RESOURCE_NOT_FOUND", "SupportTicket not found after assignment.", 404);
    }

    public async Task<SupportTicket> CancelAsync(Guid ticketId, string reason, CancellationToken ct)
    {
        EnsureAuthenticated();
        if (currentUser.Role != "CUSTOMER") throw Forbidden();
        if (ticketId == Guid.Empty || string.IsNullOrWhiteSpace(reason))
            throw new BusinessException("VALIDATION_ERROR", "ticketId and cancellation reason are required.", 400);

        var customerId = await repository.GetCustomerIdAsync(currentUser.UserAccountId, ct)
            ?? throw Forbidden();
        // Ownership and state are checked atomically inside the stored procedure.
        // The existing procedure does not persist the cancellation reason.
        try
        {
            await repository.CancelAsync(ticketId, customerId, ct);
        }
        catch (StoredProcedureBusinessException ex)
            when (ex.Code == "SUPPORT_TICKET_INVALID_STATUS_OR_OWNER")
        {
            throw new BusinessException(ex.Code,
                "Ticket is not customer-owned or cannot be cancelled from its current state.", 409);
        }
        return (await repository.GetAsync(ticketId, ct))?.Ticket
            ?? throw new BusinessException("RESOURCE_NOT_FOUND", "Ticket not found after cancellation.", 404);
    }

    private async Task<(Guid? CustomerId, Guid? FacilityId, Guid? StaffId)> GetScopeAsync(CancellationToken ct)
    {
        switch (currentUser.Role)
        {
            case "CUSTOMER":
                return (await repository.GetCustomerIdAsync(currentUser.UserAccountId, ct)
                    ?? throw Forbidden(), null, null);
            case "FACILITY_MANAGER":
                return (null, await facilityAuthorization.GetAssignedFacilityIdAsync(ct), null);
            case "FACILITY_STAFF":
                var facility = await facilityAuthorization.GetAssignedFacilityIdAsync(ct);
                var employee = await repository.GetEmployeeAsync(currentUser.UserAccountId, ct)
                    ?? throw Forbidden();
                return (null, facility, employee.EmployeeId);
            default: throw Forbidden();
        }
    }

    private void EnsureAuthenticated()
    {
        if (!currentUser.IsAuthenticated)
            throw new BusinessException("UNAUTHORIZED", "Authentication required.", 401);
    }
    private static BusinessException Forbidden() => new("FORBIDDEN", "Not authorized for this ticket.", 403);
}
