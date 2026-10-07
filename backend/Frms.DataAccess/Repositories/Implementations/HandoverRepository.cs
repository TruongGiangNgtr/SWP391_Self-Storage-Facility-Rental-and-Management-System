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
    : IHandoverRepository {
    public Task<Reservation?> GetReservationAsync(
        Guid reservationId,
        CancellationToken cancellationToken) {
        return dbContext.Reservations
            .AsNoTracking()
            .SingleOrDefaultAsync(
                x => x.ReservationId == reservationId,
                cancellationToken);
    }

    public Task<Employee?> GetEmployeeByUserAccountIdAsync(
        Guid userAccountId,
        CancellationToken cancellationToken) {
        return dbContext.Employees
            .AsNoTracking()
            .SingleOrDefaultAsync(
                x => x.UserAccountId == userAccountId,
                cancellationToken);
    }

    public async Task<CompletedHandoverRecord> CompleteAsync(
        Guid reservationId,
        Guid visitId,
        Guid storageUnitId,
        Guid employeeId,
        Guid? discountId,
        CancellationToken cancellationToken) {
        var connection =
            dbContext.Database.GetDbConnection();

        var shouldCloseConnection =
            connection.State != ConnectionState.Open;

        try {
            if (shouldCloseConnection) {
                await connection.OpenAsync(
                    cancellationToken);
            }

            await using var command =
                connection.CreateCommand();

            // MUST UPDATE SQL dbo.usp_CompleteHandover before use
            // File located in '/update'
            // usp_CompleteHandover mordified date 07/10/2026
            command.CommandText =
                "dbo.usp_CompleteHandover";

            command.CommandType =
                CommandType.StoredProcedure;

            AddGuidParameter(
                command,
                "@ReservationId",
                reservationId);

            AddGuidParameter(
                command,
                "@VisitId",
                visitId);

            AddGuidParameter(
                command,
                "@StorageUnitId",
                storageUnitId);

            AddGuidParameter(
                command,
                "@EmployeeId",
                employeeId);

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
        catch (SqlException ex) {
            var code =
                GetBusinessCode(ex.Message);

            if (code is not null) {
                throw new StoredProcedureBusinessException(
                    code,
                    code);
            }

            throw;
        }
        finally {
            if (shouldCloseConnection &&
                connection.State == ConnectionState.Open) {
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
                    x => x.StorageUnitId ==
                         contract.StorageUnitId,
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

    private static void AddGuidParameter(
        System.Data.Common.DbCommand command,
        string name,
        Guid value) {
        var parameter =
            command.CreateParameter();

        parameter.ParameterName = name;
        parameter.DbType = DbType.Guid;
        parameter.Value = value;

        command.Parameters.Add(parameter);
    }

    private static string? GetBusinessCode(
        string message) {
        var knownCodes = new[]
        {
            "RESERVATION_INVALID_STATUS",
            "VISIT_INVALID_STATUS",
            "UNIT_NOT_AVAILABLE",
            "UNIT_FACILITY_TYPE_MISMATCH",
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
