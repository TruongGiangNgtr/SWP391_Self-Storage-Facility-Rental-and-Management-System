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
}