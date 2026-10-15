using Microsoft.EntityFrameworkCore.Storage;
using System.Data;
using Frms.DataAccess.Persistence;
using Frms.DataAccess.StoredProcedures;
using Npgsql;
using Microsoft.EntityFrameworkCore;

namespace Frms.DataAccess.Repositories.Implementations;

// Narrow SQL adapter; all business transactions remain in authoritative SPs.
internal sealed class ReturnSqlExecutor(FrmsDbContext db)
{
    private static readonly string[] KnownCodes =
    [
        "RETURN_ALREADY_FINALIZED", "DAMAGE_DECISION_PENDING",
        "INSPECTION_ALREADY_CLAIMED", "INSPECTION_INVALID_STATUS",
        "VISIT_INVALID_STATUS", "VISIT_ENTITY_MISMATCH",
        "CONTRACT_NOT_ACTIVE", "RETURN_ALREADY_CONFIRMED",
        "DAMAGE_TYPE_INACTIVE", "EXTRA_FEE_TYPE_INACTIVE",
        "UNIT_INVALID_STATUS", "UNIT_NOT_AVAILABLE", "FORBIDDEN",
        "RESOURCE_NOT_FOUND", "VALIDATION_ERROR"
    ];

    public Task ExecuteAsync(string procedure, CancellationToken ct,
        params (string Name, DbType Type, object? Value)[] arguments)
        => ExecuteCoreAsync(procedure, false, ct, arguments);

    public async Task<Guid> ExecuteIdentifierAsync(string procedure, CancellationToken ct,
        params (string Name, DbType Type, object? Value)[] arguments)
    {
        var result = await ExecuteCoreAsync(procedure, true, ct, arguments);
        return result is Guid id ? id
            : throw new InvalidOperationException("Database function returned no identifier.");
    }

    private async Task<object?> ExecuteCoreAsync(string procedure, bool returnsIdentifier,
        CancellationToken ct, (string Name, DbType Type, object? Value)[] arguments)
    {
        var connection = db.Database.GetDbConnection();
        var mustClose = connection.State != ConnectionState.Open;
        try
        {
            if (mustClose) await connection.OpenAsync(ct);
            await using var command = connection.CreateCommand();
            command.Transaction = db.Database.CurrentTransaction?.GetDbTransaction();
            command.CommandText = procedure;
            command.CommandType = CommandType.StoredProcedure;
            foreach (var (name, type, value) in arguments)
            {
                var p = command.CreateParameter();
                p.ParameterName = name;
                p.DbType = type;
                p.Value = value ?? DBNull.Value;
                if (type == DbType.Binary && p is NpgsqlParameter sqlParam)
                    sqlParam.NpgsqlDbType = NpgsqlTypes.NpgsqlDbType.Bytea;
                command.Parameters.Add(p);
            }
            if (returnsIdentifier) return await PostgreSqlRoutine.ExecuteScalarAsync(command, ct);
            await PostgreSqlRoutine.ExecuteNonQueryAsync(command, ct);
            return null;
        }
        catch (PostgresException ex)
        {
            var code = KnownCodes.FirstOrDefault(c =>
                string.Equals(ex.MessageText, c, StringComparison.Ordinal));
            if (code is not null) throw new StoredProcedureBusinessException(code, code);
            throw;
        }
        finally
        {
            if (mustClose && connection.State == ConnectionState.Open)
                await connection.CloseAsync();
        }
    }

    public static (string, DbType, object?) GuidParam(string name, Guid value)
        => (name, DbType.Guid, value);
    public static (string, DbType, object?) TextParam(string name, string? value)
        => (name, DbType.String, value);
    public static (string, DbType, object?) DateParam(string name, DateOnly value)
        => (name, DbType.Date, value.ToDateTime(TimeOnly.MinValue));
    public static (string, DbType, object?) DecimalParam(string name, decimal value)
        => (name, DbType.Decimal, value);
}
