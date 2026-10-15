using System.Data;
using Frms.DataAccess.Persistence;
using Frms.DataAccess.Persistence.Entities;
using Frms.DataAccess.Repositories.Interfaces;
using Frms.DataAccess.Repositories.Models;
using Frms.DataAccess.StoredProcedures;
using Npgsql;
using Microsoft.EntityFrameworkCore;

namespace Frms.DataAccess.Repositories.Implementations;

internal sealed class RenewalRepository(
    FrmsDbContext dbContext)
    : IRenewalRepository
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

    public Task<Contract?> GetOwnedContractAsync(
        Guid customerId,
        Guid contractId,
        CancellationToken cancellationToken)
    {
        return dbContext.Contracts
            .AsNoTracking()
            .SingleOrDefaultAsync(
                x => x.ContractId == contractId &&
                     x.CustomerId == customerId,
                cancellationToken);
    }

    public async Task<RenewedContractRecord> RenewAsync(
        Guid customerId,
        Guid contractId,
        DateOnly newEndMonth,
        CancellationToken cancellationToken)
    {
        var contractBefore =
            await GetOwnedContractAsync(
                customerId,
                contractId,
                cancellationToken);

        if (contractBefore is null)
        {
            throw new StoredProcedureBusinessException(
                "RESOURCE_NOT_FOUND",
                "RESOURCE_NOT_FOUND");
        }

        Guid extensionId;

        var connection =
            dbContext.Database.GetDbConnection();

        var shouldCloseConnection =
            connection.State != ConnectionState.Open;

        try
        {
            if (shouldCloseConnection)
            {
                await connection.OpenAsync(
                    cancellationToken);
            }

            await using var command =
                connection.CreateCommand();

            command.CommandText =
                "usp_RenewContract";

            command.CommandType =
                CommandType.StoredProcedure;

            var contractParameter =
                command.CreateParameter();

            contractParameter.ParameterName =
                "@ContractId";

            contractParameter.DbType =
                DbType.Guid;

            contractParameter.Value =
                contractId;

            command.Parameters.Add(
                contractParameter);

            var newEndMonthParameter =
                command.CreateParameter();

            newEndMonthParameter.ParameterName =
                "@NewEndMonth";

            newEndMonthParameter.DbType =
                DbType.Date;

            newEndMonthParameter.Value =
                newEndMonth.ToDateTime(
                    TimeOnly.MinValue);

            command.Parameters.Add(
                newEndMonthParameter);

            var result = await PostgreSqlRoutine.ExecuteScalarAsync(command, cancellationToken);
            extensionId = result is Guid id ? id
                : throw new InvalidOperationException("Renewal function returned no extension identifier.");
        }
        catch (PostgresException ex)
        {
            var code =
                GetBusinessCode(ex.MessageText);

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
                    x => x.ContractId == contractId,
                    cancellationToken);

        var extension =
            await dbContext.ContractExtensions
                .AsNoTracking()
                .SingleAsync(
                    x => x.ContractExtensionId == extensionId,
                    cancellationToken);

        return new RenewedContractRecord(
            contract.ContractId,
            extension.OldEndMonth,
            extension.NewEndMonth,
            extension.AppliedMonthlyPrice,
            contract.Status);
    }

    private static string? GetBusinessCode(
        string message)
    {
        var knownCodes = new[]
        {
            "CONTRACT_NOT_ACTIVE",
            "RETURN_VISIT_PENDING",
            "RENEWAL_NOT_CONTIGUOUS",
            "RENEWAL_CAPACITY_NOT_AVAILABLE",
            "RESOURCE_NOT_FOUND"
        };

        return knownCodes.FirstOrDefault(
            code => string.Equals(message, code, StringComparison.Ordinal));
    }
}