using System.Data;
using Frms.DataAccess.Persistence;
using Frms.DataAccess.Repositories.Interfaces;
using Frms.DataAccess.Repositories.Models;
using Npgsql;
using Frms.DataAccess.StoredProcedures;
using Microsoft.EntityFrameworkCore.Storage;
using Microsoft.EntityFrameworkCore;

namespace Frms.DataAccess.Repositories.Implementations;

internal sealed class AdminCustomerStatusRepository(FrmsDbContext db) : IAdminCustomerStatusRepository
{
    public async Task<CustomerAccountStatusRecord?> SetStatusAsync(
        Guid customerId, string newStatus, CancellationToken ct)
    {
        var accountId = await db.Customers.AsNoTracking()
            .Where(x => x.CustomerId == customerId)
            .Select(x => (Guid?)x.UserAccountId).SingleOrDefaultAsync(ct);
        if (!accountId.HasValue) return null;

        var connection = db.Database.GetDbConnection();
        var shouldClose = connection.State != ConnectionState.Open;
        try
        {
            if (shouldClose) await db.Database.OpenConnectionAsync(ct);
            await using var command = connection.CreateCommand();
            command.Transaction = db.Database.CurrentTransaction?.GetDbTransaction();
            command.CommandText = "usp_SetUserAccountStatus";
            command.CommandType = CommandType.StoredProcedure;
            var id = command.CreateParameter();
            id.ParameterName = "@UserAccountId";
            id.DbType = DbType.Guid;
            id.Value = accountId.Value;
            command.Parameters.Add(id);
            var status = command.CreateParameter();
            status.ParameterName = "@NewStatus";
            status.DbType = DbType.AnsiString;
            status.Size = 50;
            status.Value = newStatus;
            command.Parameters.Add(status);
            await PostgreSqlRoutine.ExecuteNonQueryAsync(command, ct);
        }
        catch (PostgresException ex) when (PostgreSqlErrors.Number(ex) == 51149)
        {
            throw new CustomerStatusProcedureException("CUSTOMER_HAS_ACTIVE_LIFECYCLE");
        }
        catch (PostgresException ex) when (PostgreSqlErrors.Number(ex) == 51150)
        {
            throw new CustomerStatusProcedureException("USER_ACCOUNT_NOT_FOUND");
        }
        finally
        {
            if (shouldClose && connection.State == ConnectionState.Open)
                await db.Database.CloseConnectionAsync();
        }
        return new CustomerAccountStatusRecord(customerId, accountId.Value, newStatus);
    }
}
