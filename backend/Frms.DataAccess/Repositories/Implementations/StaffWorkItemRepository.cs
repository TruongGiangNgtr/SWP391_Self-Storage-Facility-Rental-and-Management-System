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
        // Build a common scalar projection before UNION ALL, so PostgreSQL
        // filters/scopes, counts, sorts and pages the complete combined dataset.
        var reservationVisits =
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
                && (visit.Status == "SCHEDULED" || visit.Status == "CHECKED_IN")
            select new {
                WorkType = "RESERVATION_VISIT",
                ReferenceId = visit.VisitId,
                EntityId = (Guid?)visit.EntityId,
                ScheduledDate = (DateOnly?)visit.VisitDate,
                Status = visit.Status,
                CustomerId = (Guid?)customer.CustomerId,
                CustomerName = (string?)customer.FullName,
                CustomerPhone = (string?)account.PhoneNumber
            };

        var contractVisits =
            from visit in dbContext.Visits.AsNoTracking()
            join contract in dbContext.Contracts.AsNoTracking()
                on visit.EntityId equals contract.ContractId
            join customer in dbContext.Customers.AsNoTracking()
                on contract.CustomerId equals customer.CustomerId
            join account in dbContext.UserAccounts.AsNoTracking()
                on customer.UserAccountId equals account.UserAccountId
            where contract.FacilityId == facilityId
                && (visit.VisitType == "ACCESS" || visit.VisitType == "RETURN")
                && visit.VisitDate == date
                && (visit.Status == "SCHEDULED" || visit.Status == "CHECKED_IN")
            select new {
                WorkType = visit.VisitType == "ACCESS" ? "ACCESS_VISIT" : "RETURN_VISIT",
                ReferenceId = visit.VisitId,
                EntityId = (Guid?)visit.EntityId,
                ScheduledDate = (DateOnly?)visit.VisitDate,
                Status = visit.Status,
                CustomerId = (Guid?)customer.CustomerId,
                CustomerName = (string?)customer.FullName,
                CustomerPhone = (string?)account.PhoneNumber
            };

        var inspections =
            from inspection in dbContext.Inspections.AsNoTracking()
            join contract in dbContext.Contracts.AsNoTracking()
                on inspection.ContractId equals contract.ContractId
            where contract.FacilityId == facilityId
                && (inspection.Status == "PENDING"
                    || (inspection.Status == "IN_PROGRESS" && inspection.EmployeeId == employeeId))
            select new {
                WorkType = "INSPECTION",
                ReferenceId = inspection.InspectionId,
                EntityId = (Guid?)null,
                ScheduledDate = (DateOnly?)null,
                Status = inspection.Status,
                CustomerId = (Guid?)null,
                CustomerName = (string?)null,
                CustomerPhone = (string?)null
            };

        var tickets =
            from ticket in dbContext.SupportTickets.AsNoTracking()
            join contract in dbContext.Contracts.AsNoTracking()
                on ticket.ContractId equals contract.ContractId
            where contract.FacilityId == facilityId
                && ticket.AssignedEmployeeId == employeeId
                && (ticket.Status == "OPEN" || ticket.Status == "IN_PROGRESS")
            select new {
                WorkType = "SUPPORT_TICKET",
                ReferenceId = ticket.SupportTicketId,
                EntityId = (Guid?)null,
                ScheduledDate = (DateOnly?)null,
                Status = ticket.Status,
                CustomerId = (Guid?)null,
                CustomerName = (string?)null,
                CustomerPhone = (string?)null
            };

        var allItems = reservationVisits.Concat(contractVisits)
            .Concat(inspections).Concat(tickets);
        var totalItems = await allItems.CountAsync(cancellationToken);
        var skip = ((long)page - 1) * pageSize;
        if (skip > int.MaxValue || skip >= totalItems)
            return (Array.Empty<StaffWorkItemRecord>(), totalItems);

        var items = await allItems
            .OrderBy(x => x.ScheduledDate.HasValue ? 0 : 1)
            .ThenBy(x => x.ScheduledDate)
            .ThenBy(x => x.WorkType)
            .ThenBy(x => x.ReferenceId)
            .Skip((int)skip)
            .Take(pageSize)
            .Select(x => new StaffWorkItemRecord(
                x.WorkType, x.ReferenceId, x.EntityId, x.ScheduledDate, x.Status,
                x.CustomerId, x.CustomerName, x.CustomerPhone, null, null))
            .ToArrayAsync(cancellationToken);

        // Batch-enrich only the physical RETURN visits in the paged result.
        // No per-row requests and no set-operation DateTime store-type mismatch.
        var returnVisitIds = items.Where(x => x.WorkType == "RETURN_VISIT")
            .Select(x => x.ReferenceId).ToArray();
        if (returnVisitIds.Length > 0)
        {
            var confirmations = await dbContext.Visits.AsNoTracking()
                .Where(visit => returnVisitIds.Contains(visit.VisitId))
                .Select(visit => new
                {
                    visit.VisitId,
                    visit.ActualReturnDate,
                    InspectionId = dbContext.Inspections.AsNoTracking()
                        .Where(i => i.VisitId == visit.VisitId)
                        .Select(i => (Guid?)i.InspectionId).FirstOrDefault()
                })
                .ToArrayAsync(cancellationToken);
            var byVisit = confirmations.ToDictionary(x => x.VisitId);
            items = items.Select(item =>
                byVisit.TryGetValue(item.ReferenceId, out var confirmation)
                    ? item with
                    {
                        ActualReturnDate = confirmation.ActualReturnDate,
                        InspectionId = confirmation.InspectionId
                    }
                    : item).ToArray();
        }
        return (items, totalItems);
    }
}
