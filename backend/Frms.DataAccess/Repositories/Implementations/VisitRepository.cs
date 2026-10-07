using System.Data;
using Frms.DataAccess.Persistence;
using Frms.DataAccess.Persistence.Entities;
using Frms.DataAccess.Repositories.Interfaces;
using Frms.DataAccess.StoredProcedures;
using Microsoft.Data.SqlClient;
using Microsoft.EntityFrameworkCore;

namespace Frms.DataAccess.Repositories.Implementations;

internal sealed class VisitRepository(
    FrmsDbContext dbContext) : IVisitRepository
{
    public Task<Guid?> GetCustomerIdByUserAccountIdAsync(
        Guid userAccountId,
        CancellationToken cancellationToken)
    {
        return dbContext.Customers
            .AsNoTracking()
            .Where(x => x.UserAccountId == userAccountId)
            .Select(x => (Guid?)x.CustomerId)
            .SingleOrDefaultAsync(cancellationToken);
    }

    public async Task<IReadOnlyList<Visit>> ListOwnedAsync(
        Guid customerId,
        int skip,
        int take,
        CancellationToken cancellationToken)
    {
        return await OwnedVisits(customerId)
            .AsNoTracking()
            .OrderByDescending(x => x.VisitDate)
            .ThenBy(x => x.VisitId)
            .Skip(skip)
            .Take(take)
            .ToListAsync(cancellationToken);
    }

    public Task<int> CountOwnedAsync(
        Guid customerId,
        CancellationToken cancellationToken)
    {
        return OwnedVisits(customerId)
            .CountAsync(cancellationToken);
    }

    public Task<Visit?> GetOwnedByIdAsync(
        Guid customerId,
        Guid visitId,
        CancellationToken cancellationToken)
    {
        return OwnedVisits(customerId)
            .AsNoTracking()
            .SingleOrDefaultAsync(
                x => x.VisitId == visitId,
                cancellationToken);
    }

    public Task<Reservation?> GetOwnedReservationForVisitAsync(
        Guid customerId,
        Guid visitId,
        CancellationToken cancellationToken)
    {
        return (
            from visit in dbContext.Visits.AsNoTracking()
            join reservation in dbContext.Reservations.AsNoTracking()
                on visit.EntityId equals reservation.ReservationId
            where visit.VisitId == visitId
                  && visit.VisitType == "RESERVATION"
                  && reservation.CustomerId == customerId
            select reservation)
            .SingleOrDefaultAsync(cancellationToken);
    }

    public Task<Policy?> GetPolicyAsync(
        Guid policyId,
        CancellationToken cancellationToken)
    {
        return dbContext.Policies
            .AsNoTracking()
            .SingleOrDefaultAsync(
                x => x.PolicyId == policyId,
                cancellationToken);
    }

    public async Task UpdateVisitDateAsync(
        Guid visitId,
        DateOnly visitDate,
        CancellationToken cancellationToken)
    {
        var visit = await dbContext.Visits
            .SingleAsync(
                x => x.VisitId == visitId,
                cancellationToken);

        visit.VisitDate = visitDate;

        await dbContext.SaveChangesAsync(cancellationToken);
    }

    public async Task<Visit> CancelAsync(
        Guid customerId,
        Guid visitId,
        string reason,
        CancellationToken cancellationToken)
    {
        var ownedVisit = await GetOwnedByIdAsync(
            customerId,
            visitId,
            cancellationToken);

        if (ownedVisit is null)
        {
            throw new StoredProcedureBusinessException(
                "RESOURCE_NOT_FOUND",
                "RESOURCE_NOT_FOUND");
        }

        var connection = dbContext.Database.GetDbConnection();
        var shouldCloseConnection =
            connection.State != ConnectionState.Open;

        try
        {
            if (shouldCloseConnection)
            {
                await connection.OpenAsync(cancellationToken);
            }

            await using var command = connection.CreateCommand();

            command.CommandText = "dbo.usp_CancelVisit";
            command.CommandType = CommandType.StoredProcedure;

            var visitParameter = command.CreateParameter();
            visitParameter.ParameterName = "@VisitId";
            visitParameter.DbType = DbType.Guid;
            visitParameter.Value = visitId;
            command.Parameters.Add(visitParameter);

            await command.ExecuteNonQueryAsync(cancellationToken);
        }
        catch (SqlException ex) when (ex.Number == 51112) {
            throw new StoredProcedureBusinessException(
                "VISIT_INVALID_STATUS",
                "VISIT_INVALID_STATUS");
        }

        catch (SqlException ex) when (ex.Number == 51115) {
            throw new StoredProcedureBusinessException(
                "RESERVATION_VISIT_CANCEL_NOT_ALLOWED",
                "RESERVATION_VISIT_CANCEL_NOT_ALLOWED");
        }
        finally
        {
            if (shouldCloseConnection &&
                connection.State == ConnectionState.Open)
            {
                await connection.CloseAsync();
            }
        }

        return await dbContext.Visits
            .AsNoTracking()
            .SingleAsync(
                x => x.VisitId == visitId,
                cancellationToken);
    }

    private IQueryable<Visit> OwnedVisits(Guid customerId)
    {
        return dbContext.Visits.Where(visit =>
            (
                visit.VisitType == "RESERVATION" &&
                dbContext.Reservations.Any(reservation =>
                    reservation.ReservationId == visit.EntityId &&
                    reservation.CustomerId == customerId)
            )
            ||
            (
                (visit.VisitType == "ACCESS" ||
                 visit.VisitType == "RETURN") &&
                dbContext.Contracts.Any(contract =>
                    contract.ContractId == visit.EntityId &&
                    contract.CustomerId == customerId)
            ));
    }

    public Task<Employee?> GetEmployeeByUserAccountIdAsync(
        Guid userAccountId,
        CancellationToken cancellationToken)
    {
        return dbContext.Employees
            .AsNoTracking()
            .SingleOrDefaultAsync(
                x => x.UserAccountId == userAccountId,
                cancellationToken);
    }

    public Task<Visit?> GetByIdAsync(
        Guid visitId,
        CancellationToken cancellationToken)
    {
        return dbContext.Visits
            .AsNoTracking()
            .SingleOrDefaultAsync(
                x => x.VisitId == visitId,
                cancellationToken);
    }

    public Task<Reservation?> GetReservationForVisitAsync(
        Guid visitId,
        CancellationToken cancellationToken)
    {
        return (
            from visit in dbContext.Visits.AsNoTracking()
            join reservation in dbContext.Reservations.AsNoTracking()
                on visit.EntityId equals reservation.ReservationId
            where visit.VisitId == visitId
                && visit.VisitType == "RESERVATION"
            select reservation)
            .SingleOrDefaultAsync(cancellationToken);
    }

    public async Task<Visit> CheckInAsync(
        Guid visitId,
        Guid employeeId,
        CancellationToken cancellationToken)
    {
        var connection = dbContext.Database.GetDbConnection();
        var shouldCloseConnection =
            connection.State != ConnectionState.Open;

        try
        {
            if (shouldCloseConnection)
            {
                await connection.OpenAsync(cancellationToken);
            }

            await using var command = connection.CreateCommand();

            command.CommandText = "dbo.usp_CheckInVisit";
            command.CommandType = CommandType.StoredProcedure;

            var visitParameter = command.CreateParameter();
            visitParameter.ParameterName = "@VisitId";
            visitParameter.DbType = DbType.Guid;
            visitParameter.Value = visitId;
            command.Parameters.Add(visitParameter);

            var employeeParameter = command.CreateParameter();
            employeeParameter.ParameterName = "@EmployeeId";
            employeeParameter.DbType = DbType.Guid;
            employeeParameter.Value = employeeId;
            command.Parameters.Add(employeeParameter);

            await command.ExecuteNonQueryAsync(
                cancellationToken);
        }
        catch (SqlException ex)
        {
            if (ex.Message.Contains(
                    "VISIT_INVALID_STATUS",
                    StringComparison.OrdinalIgnoreCase))
            {
                throw new StoredProcedureBusinessException(
                    "VISIT_INVALID_STATUS",
                    "VISIT_INVALID_STATUS");
            }

            if (ex.Message.Contains(
                    "VISIT_ENTITY_MISMATCH",
                    StringComparison.OrdinalIgnoreCase))
            {
                throw new StoredProcedureBusinessException(
                    "VISIT_ENTITY_MISMATCH",
                    "VISIT_ENTITY_MISMATCH");
            }

            if (ex.Message.Contains(
                    "FORBIDDEN",
                    StringComparison.OrdinalIgnoreCase))
            {
                throw new StoredProcedureBusinessException(
                    "FORBIDDEN",
                    "FORBIDDEN");
            }

            throw;
        }
        finally
        {
            if (shouldCloseConnection &&
                connection.State == ConnectionState.Open)
            {
                await connection.CloseAsync();
            }
        }

        return await dbContext.Visits
            .AsNoTracking()
            .SingleAsync(
                x => x.VisitId == visitId,
                cancellationToken);
    }

    public Task<Contract?> GetContractForVisitAsync(
        Guid visitId,
        CancellationToken cancellationToken)
    {
        return (
            from visit in dbContext.Visits.AsNoTracking()
            join contract in dbContext.Contracts.AsNoTracking()
                on visit.EntityId equals contract.ContractId
            where visit.VisitId == visitId
                  && (visit.VisitType == "ACCESS"
                      || visit.VisitType == "RETURN")
            select contract)
            .SingleOrDefaultAsync(cancellationToken);
    }

    public async Task<IReadOnlyList<Visit>> ListByFacilityAsync(
        Guid facilityId,
        int skip,
        int take,
        CancellationToken cancellationToken) {
        return await FacilityVisits(facilityId)
            .AsNoTracking()
            .OrderByDescending(x => x.VisitDate)
            .ThenBy(x => x.VisitId)
            .Skip(skip)
            .Take(take)
            .ToListAsync(cancellationToken);
    }

    public Task<int> CountByFacilityAsync(
        Guid facilityId,
        CancellationToken cancellationToken) {
        return FacilityVisits(facilityId)
            .CountAsync(cancellationToken);
    }

    public async Task<IReadOnlyList<Visit>> ListAllAsync(
        int skip,
        int take,
        CancellationToken cancellationToken) {
        return await dbContext.Visits
            .AsNoTracking()
            .OrderByDescending(x => x.VisitDate)
            .ThenBy(x => x.VisitId)
            .Skip(skip)
            .Take(take)
            .ToListAsync(cancellationToken);
    }

    public Task<int> CountAllAsync(
        CancellationToken cancellationToken) {
        return dbContext.Visits
            .CountAsync(cancellationToken);
    }

    private IQueryable<Visit> FacilityVisits(Guid facilityId) {
        return dbContext.Visits.Where(visit =>
            (
                visit.VisitType == "RESERVATION" &&
                dbContext.Reservations.Any(reservation =>
                    reservation.ReservationId == visit.EntityId &&
                    reservation.FacilityId == facilityId)
            )
            ||
            (
                (visit.VisitType == "ACCESS" ||
                 visit.VisitType == "RETURN") &&
                dbContext.Contracts.Any(contract =>
                    contract.ContractId == visit.EntityId &&
                    contract.FacilityId == facilityId)
            ));
    }
}
