using System.Data;
using Frms.DataAccess.Persistence;
using Frms.DataAccess.Persistence.Entities;
using Frms.DataAccess.Repositories.Interfaces;
using Frms.DataAccess.Repositories.Models;
using Frms.DataAccess.StoredProcedures;
using Microsoft.Data.SqlClient;
using Microsoft.EntityFrameworkCore;

namespace Frms.DataAccess.Repositories.Implementations;

internal sealed class HandoverRepository(
    FrmsDbContext dbContext)
    : IHandoverRepository
{
    public Task<Reservation?> GetReservationAsync(
        Guid reservationId,
        CancellationToken cancellationToken)
    {
        return dbContext.Reservations
            .AsNoTracking()
            .SingleOrDefaultAsync(
                x => x.ReservationId == reservationId,
                cancellationToken);
    }

    public async Task<CompletedHandoverRecord> CompleteAsync(
        Guid reservationId,
        Guid visitId,
        Guid storageUnitId,
        Guid firstMonthPaymentId,
        Guid? discountId,
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

            command.CommandText =
                "dbo.usp_CompleteHandover";

            command.CommandType =
                CommandType.StoredProcedure;

            var reservationParameter =
                command.CreateParameter();

            reservationParameter.ParameterName =
                "@ReservationId";

            reservationParameter.DbType =
                DbType.Guid;

            reservationParameter.Value =
                reservationId;

            command.Parameters.Add(
                reservationParameter);

            var visitParameter =
                command.CreateParameter();

            visitParameter.ParameterName =
                "@VisitId";

            visitParameter.DbType =
                DbType.Guid;

            visitParameter.Value =
                visitId;

            command.Parameters.Add(
                visitParameter);

            var storageUnitParameter =
                command.CreateParameter();

            storageUnitParameter.ParameterName =
                "@StorageUnitId";

            storageUnitParameter.DbType =
                DbType.Guid;

            storageUnitParameter.Value =
                storageUnitId;

            command.Parameters.Add(
                storageUnitParameter);

            var paymentParameter =
                command.CreateParameter();

            paymentParameter.ParameterName =
                "@PaymentId";

            paymentParameter.DbType =
                DbType.Guid;

            paymentParameter.Value =
                firstMonthPaymentId;

            command.Parameters.Add(
                paymentParameter);

            var discountParameter =
                command.CreateParameter();

            discountParameter.ParameterName =
                "@DiscountId";

            discountParameter.DbType =
                DbType.Guid;

            discountParameter.Value =
                discountId.HasValue
                    ? discountId.Value
                    : DBNull.Value;

            command.Parameters.Add(
                discountParameter);

            await command.ExecuteNonQueryAsync(
                cancellationToken);
        }
        catch (SqlException ex)
        {
            var code = GetBusinessCode(ex.Message);

            if (code is not null)
            {
                throw new StoredProcedureBusinessException(
                    code,
                    code);
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

        var contract =
            await dbContext.Contracts
                .AsNoTracking()
                .SingleAsync(
                    x => x.ReservationId == reservationId,
                    cancellationToken);

        var reservation =
            await dbContext.Reservations
                .AsNoTracking()
                .SingleAsync(
                    x => x.ReservationId == reservationId,
                    cancellationToken);

        var visit =
            await dbContext.Visits
                .AsNoTracking()
                .SingleAsync(
                    x => x.VisitId == visitId,
                    cancellationToken);

        var storageUnit =
            await dbContext.StorageUnits
                .AsNoTracking()
                .SingleAsync(
                    x => x.StorageUnitId == storageUnitId,
                    cancellationToken);

        return new CompletedHandoverRecord(
            new HandoverContractRecord(
                contract.ContractId,
                contract.Status,
                contract.StorageUnitId,
                contract.StartMonth,
                contract.EndMonth),
            reservation.Status,
            visit.Status,
            storageUnit.Status);
    }

    private static string? GetBusinessCode(
        string message)
    {
        var knownCodes = new[]
        {
            "RESERVATION_INVALID_STATUS",
            "VISIT_INVALID_STATUS",
            "UNIT_NOT_AVAILABLE",
            "UNIT_FACILITY_TYPE_MISMATCH",
            "FIRST_MONTH_PAYMENT_NOT_SUCCESS",
            "DISCOUNT_NOT_OWNED_BY_CUSTOMER",
            "DISCOUNT_NOT_VALID",
            "FORBIDDEN",
            "RESOURCE_NOT_FOUND"
        };

        return knownCodes.FirstOrDefault(
            code => message.Contains(
                code,
                StringComparison.OrdinalIgnoreCase));
    }
}