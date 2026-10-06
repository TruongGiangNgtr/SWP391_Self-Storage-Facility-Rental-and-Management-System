using Frms.DataAccess.Persistence;
using Frms.DataAccess.Repositories.Interfaces;
using Frms.DataAccess.Repositories.Models;
using Microsoft.EntityFrameworkCore;
using System.Data;
using Frms.DataAccess.StoredProcedures;
using Microsoft.Data.SqlClient;

namespace Frms.DataAccess.Repositories.Implementations;

internal sealed class ReservationRepository(
    FrmsDbContext dbContext) : IReservationRepository
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

    public async Task<CreatedReservationRecord> CreateAsync(
        Guid customerId,
        Guid facilityId,
        Guid unitTypeId,
        DateOnly startMonth,
        DateOnly endMonth,
        CancellationToken cancellationToken)
    {
        Guid reservationId;
        Guid depositInvoiceId;

        var connection = dbContext.Database.GetDbConnection();
        var shouldCloseConnection = connection.State != ConnectionState.Open;

        try
        {
            if (shouldCloseConnection)
            {
                await connection.OpenAsync(cancellationToken);
            }

            await using var command = connection.CreateCommand();

            command.CommandText = "dbo.usp_CreateReservation";
            command.CommandType = CommandType.StoredProcedure;

            var customerParameter = command.CreateParameter();
            customerParameter.ParameterName = "@CustomerId";
            customerParameter.DbType = DbType.Guid;
            customerParameter.Value = customerId;
            command.Parameters.Add(customerParameter);

            var facilityParameter = command.CreateParameter();
            facilityParameter.ParameterName = "@FacilityId";
            facilityParameter.DbType = DbType.Guid;
            facilityParameter.Value = facilityId;
            command.Parameters.Add(facilityParameter);

            var unitTypeParameter = command.CreateParameter();
            unitTypeParameter.ParameterName = "@UnitTypeId";
            unitTypeParameter.DbType = DbType.Guid;
            unitTypeParameter.Value = unitTypeId;
            command.Parameters.Add(unitTypeParameter);

            var startMonthParameter = command.CreateParameter();
            startMonthParameter.ParameterName = "@StartMonth";
            startMonthParameter.DbType = DbType.Date;
            startMonthParameter.Value = startMonth.ToDateTime(TimeOnly.MinValue);
            command.Parameters.Add(startMonthParameter);

            var endMonthParameter = command.CreateParameter();
            endMonthParameter.ParameterName = "@EndMonth";
            endMonthParameter.DbType = DbType.Date;
            endMonthParameter.Value = endMonth.ToDateTime(TimeOnly.MinValue);
            command.Parameters.Add(endMonthParameter);

            await using var reader =
                await command.ExecuteReaderAsync(cancellationToken);

            if (!await reader.ReadAsync(cancellationToken))
            {
                throw new InvalidOperationException(
                    "usp_CreateReservation returned no result.");
            }

            reservationId =
                reader.GetGuid(reader.GetOrdinal("ReservationId"));

            depositInvoiceId =
                reader.GetGuid(reader.GetOrdinal("DepositInvoiceId"));
        }
        catch (SqlException ex) when (ex.Number is 51101 or 51102 or 51103 or 51104 or 51106)
        {
            var code = ex.Number switch
            {
                51101 => "INVALID_MONTH_RANGE",
                51102 => "ACCOUNT_INACTIVE",
                51103 => "FACILITY_INACTIVE",
                51104 => "RESOURCE_NOT_FOUND",
                51106 => "CAPACITY_NOT_AVAILABLE",
                _ => throw new InvalidOperationException()
            };

            throw new StoredProcedureBusinessException(code, code);
        }
        finally
        {
            if (shouldCloseConnection &&
                connection.State == ConnectionState.Open)
            {
                await connection.CloseAsync();
            }
        }

        var reservation = await dbContext.Reservations
            .AsNoTracking()
            .SingleAsync(
                x => x.ReservationId == reservationId,
                cancellationToken);

        var depositInvoice = await dbContext.Invoices
            .AsNoTracking()
            .SingleAsync(
                x => x.InvoiceId == depositInvoiceId,
                cancellationToken);

        return new CreatedReservationRecord(
            reservation.ReservationId,
            reservation.FacilityId,
            reservation.UnitTypeId,
            reservation.PolicyId,
            reservation.StartMonth,
            reservation.EndMonth,
            reservation.LockedRentalPrice,
            reservation.DepositAmount,
            reservation.Status,
            new DepositInvoiceRecord(
                depositInvoice.InvoiceId,
                depositInvoice.Status,
                depositInvoice.AmountDue,
                depositInvoice.DueDate),
            reservation.CreatedAt);
    }

    public async Task<ConfirmedReservationRecord> ConfirmAsync(
        Guid customerId,
        Guid reservationId,
        DateOnly reservationVisitDate,
        CancellationToken cancellationToken)
    {
        var ownedReservation = await dbContext.Reservations
            .AsNoTracking()
            .SingleOrDefaultAsync(
                x => x.ReservationId == reservationId &&
                    x.CustomerId == customerId,
                cancellationToken);

        if (ownedReservation is null)
        {
            throw new StoredProcedureBusinessException(
                "RESOURCE_NOT_FOUND",
                "RESOURCE_NOT_FOUND");
        }

        // Idempotent retry: nếu Reservation đã confirm và Visit đã tồn tại,
        // trả lại authoritative state thay vì tạo Visit thứ hai.
        if (ownedReservation.Status == "CONFIRMED")
        {
            var existing = await TryGetConfirmedAsync(
                customerId,
                reservationId,
                cancellationToken);

            if (existing is not null)
            {
                return existing;
            }
        }

        Guid visitId;
        string reservationStatus;

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

            command.CommandText = "dbo.usp_ConfirmReservation";
            command.CommandType = CommandType.StoredProcedure;

            var reservationParameter = command.CreateParameter();
            reservationParameter.ParameterName = "@ReservationId";
            reservationParameter.DbType = DbType.Guid;
            reservationParameter.Value = reservationId;
            command.Parameters.Add(reservationParameter);

            var visitDateParameter = command.CreateParameter();
            visitDateParameter.ParameterName = "@ReservationVisitDate";
            visitDateParameter.DbType = DbType.Date;
            visitDateParameter.Value =
                reservationVisitDate.ToDateTime(TimeOnly.MinValue);
            command.Parameters.Add(visitDateParameter);

            await using var reader =
                await command.ExecuteReaderAsync(cancellationToken);

            if (!await reader.ReadAsync(cancellationToken))
            {
                throw new InvalidOperationException(
                    "usp_ConfirmReservation returned no result.");
            }

            visitId = reader.GetGuid(
                reader.GetOrdinal("VisitId"));

            reservationStatus = reader.GetString(
                reader.GetOrdinal("Status"));
        }
        catch (SqlException ex)
            when (ex.Number is 51107 or 51108 or 51109 or 51110)
        {
            // 51107/51110 có thể xảy ra khi request confirm được retry
            // sau/concurrently với request đã thành công.
            if (ex.Number is 51107 or 51110)
            {
                var existing = await TryGetConfirmedAsync(
                    customerId,
                    reservationId,
                    cancellationToken);

                if (existing is not null)
                {
                    return existing;
                }
            }

            var code = ex.Number switch
            {
                51107 => "RESERVATION_INVALID_STATUS",
                51108 => "DEPOSIT_NOT_PAID",
                51109 => "VISIT_DATE_OUT_OF_POLICY",
                51110 => "RESERVATION_INVALID_STATUS",
                _ => throw new InvalidOperationException()
            };

            throw new StoredProcedureBusinessException(
                code,
                code);
        }
        finally
        {
            if (shouldCloseConnection &&
                connection.State == ConnectionState.Open)
            {
                await connection.CloseAsync();
            }
        }

        var visit = await dbContext.Visits
            .AsNoTracking()
            .SingleAsync(
                x => x.VisitId == visitId,
                cancellationToken);

        return new ConfirmedReservationRecord(
            reservationId,
            reservationStatus,
            new ReservationVisitRecord(
                visit.VisitId,
                visit.VisitType,
                visit.VisitDate,
                visit.Status));
    }

    private async Task<ConfirmedReservationRecord?> TryGetConfirmedAsync(
        Guid customerId,
        Guid reservationId,
        CancellationToken cancellationToken)
    {
        var reservation = await dbContext.Reservations
            .AsNoTracking()
            .SingleOrDefaultAsync(
                x => x.ReservationId == reservationId &&
                    x.CustomerId == customerId &&
                    x.Status == "CONFIRMED",
                cancellationToken);

        if (reservation is null)
        {
            return null;
        }

        var visit = await dbContext.Visits
            .AsNoTracking()
            .SingleOrDefaultAsync(
                x => x.EntityId == reservationId &&
                    x.VisitType == "RESERVATION",
                cancellationToken);

        if (visit is null)
        {
            return null;
        }

        return new ConfirmedReservationRecord(
            reservation.ReservationId,
            reservation.Status,
            new ReservationVisitRecord(
                visit.VisitId,
                visit.VisitType,
                visit.VisitDate,
                visit.Status));
    }
}