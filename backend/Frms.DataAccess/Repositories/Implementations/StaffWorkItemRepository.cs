using Frms.DataAccess.Persistence;
using Frms.DataAccess.Repositories.Interfaces;
using Frms.DataAccess.Repositories.Models;
using Microsoft.EntityFrameworkCore;

namespace Frms.DataAccess.Repositories.Implementations;

internal sealed class StaffWorkItemRepository(
    FrmsDbContext dbContext) : IStaffWorkItemRepository {
    public Task<Guid?> GetEmployeeIdAsync(
        Guid userAccountId,
        Guid facilityId,
        CancellationToken cancellationToken) {
        return dbContext.Employees
            .AsNoTracking()
            .Where(x =>
                x.UserAccountId == userAccountId &&
                x.FacilityId == facilityId)
            .Select(x => (Guid?)x.EmployeeId)
            .SingleOrDefaultAsync(cancellationToken);
    }

    public async Task<(
        IReadOnlyList<StaffWorkItemRecord> Items,
        int TotalItems)> GetWorkItemsAsync(
            Guid facilityId,
            Guid employeeId,
            DateOnly date,
            int page,
            int pageSize,
            CancellationToken cancellationToken) {
        // RESERVATION visits
        var reservationVisits = await (
            from visit in dbContext.Visits.AsNoTracking()
            join reservation in dbContext.Reservations.AsNoTracking()
                on visit.EntityId equals reservation.ReservationId
            join customer in dbContext.Customers.AsNoTracking()
                on reservation.CustomerId equals customer.CustomerId
            join account in dbContext.UserAccounts.AsNoTracking()
                on customer.UserAccountId equals account.UserAccountId
            where reservation.FacilityId == facilityId
                && visit.VisitType == "RESERVATION"
                && visit.VisitDate == date
                && (visit.Status == "SCHEDULED"
                    || visit.Status == "CHECKED_IN")
            select new StaffWorkItemRecord(
                "RESERVATION_VISIT",
                visit.VisitId,
                visit.EntityId,
                visit.VisitDate,
                visit.Status,
                customer.CustomerId,
                customer.FullName,
                account.PhoneNumber)
        ).ToListAsync(cancellationToken);

        // ACCESS and RETURN visits
        var contractVisits = await (
            from visit in dbContext.Visits.AsNoTracking()
            join contract in dbContext.Contracts.AsNoTracking()
                on visit.EntityId equals contract.ContractId
            join customer in dbContext.Customers.AsNoTracking()
                on contract.CustomerId equals customer.CustomerId
            join account in dbContext.UserAccounts.AsNoTracking()
                on customer.UserAccountId equals account.UserAccountId
            where contract.FacilityId == facilityId
                && (visit.VisitType == "ACCESS"
                    || visit.VisitType == "RETURN")
                && visit.VisitDate == date
                && (visit.Status == "SCHEDULED"
                    || visit.Status == "CHECKED_IN")
            select new StaffWorkItemRecord(
                visit.VisitType == "ACCESS"
                    ? "ACCESS_VISIT"
                    : "RETURN_VISIT",
                visit.VisitId,
                visit.EntityId,
                visit.VisitDate,
                visit.Status,
                customer.CustomerId,
                customer.FullName,
                account.PhoneNumber)
        ).ToListAsync(cancellationToken);

        // Pending Inspections and Inspections claimed by this Staff
        var inspections = await (
            from inspection in dbContext.Inspections.AsNoTracking()
            join contract in dbContext.Contracts.AsNoTracking()
                on inspection.ContractId equals contract.ContractId
            where contract.FacilityId == facilityId
                && (
                    inspection.Status == "PENDING"
                    || (
                        inspection.Status == "IN_PROGRESS"
                        && inspection.EmployeeId == employeeId
                    )
                )
            select new StaffWorkItemRecord(
                "INSPECTION",
                inspection.InspectionId,
                null,
                null,
                inspection.Status,
                null,
                null,
                null)
        ).ToListAsync(cancellationToken);

        // Support tickets assigned to current Staff
        var tickets = await (
            from ticket in dbContext.SupportTickets.AsNoTracking()
            join contract in dbContext.Contracts.AsNoTracking()
                on ticket.ContractId equals contract.ContractId
            where contract.FacilityId == facilityId
                && ticket.AssignedEmployeeId == employeeId
                && (
                    ticket.Status == "OPEN"
                    || ticket.Status == "IN_PROGRESS"
                )
            select new StaffWorkItemRecord(
                "SUPPORT_TICKET",
                ticket.SupportTicketId,
                null,
                null,
                ticket.Status,
                null,
                null,
                null)
        ).ToListAsync(cancellationToken);

        // Merge before pagination so metadata covers every work type
        var allItems = reservationVisits
            .Concat(contractVisits)
            .Concat(inspections)
            .Concat(tickets)
            .OrderBy(x => x.ScheduledDate.HasValue ? 0 : 1)
            .ThenBy(x => x.ScheduledDate)
            .ThenBy(x => x.WorkType)
            .ThenBy(x => x.ReferenceId)
            .ToList();

        var totalItems = allItems.Count;

        var skip = (int)Math.Min(
            (long)(page - 1) * pageSize,
            totalItems);

        var items = allItems
            .Skip(skip)
            .Take(pageSize)
            .ToArray();

        return (items, totalItems);
    }
}
