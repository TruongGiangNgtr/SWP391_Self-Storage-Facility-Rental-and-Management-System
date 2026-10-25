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
    // Match both SQLSTATE and MESSAGE emitted by the deployed PostgreSQL
    // Inspections.sql / Returns.sql routines. Do not classify an unrelated
    // database error as a business conflict based on MessageText alone.
    private static readonly IReadOnlyDictionary<string, string> BusinessCodesBySqlState =
        new Dictionary<string, string>(StringComparer.Ordinal)
        {
            ["P1112"] = "VISIT_INVALID_STATUS",
            ["P1114"] = "STAFF_FACILITY_OR_ROLE_INVALID",
            ["P1120"] = "CONTRACT_NOT_ACTIVE",
            ["P1124"] = "STORAGE_UNIT_NOT_IN_USE",
            ["P1125"] = "INSPECTION_ALREADY_CLAIMED",
            ["P1126"] = "INSPECTION_INVALID_STATUS",
            ["P1127"] = "DAMAGE_AMOUNT_REQUIRED",
            ["P1128"] = "EXTRA_FEE_TYPE_NOT_ACTIVE",
            ["P1129"] = "EXTRA_FEE_AMOUNT_INVALID",
            ["P1130"] = "EVIDENCE_FILE_REQUIRED",
            ["P1212"] = "DAMAGE_TYPE_NOT_ACTIVE",
            ["P1280"] = "VALIDATION_ERROR",
            ["P1281"] = "RESOURCE_NOT_FOUND",
            ["P1282"] = "RETURN_ALREADY_FINALIZED",
            ["P1283"] = "FORBIDDEN",
            ["P1284"] = "UNIT_INVALID_STATUS",
            ["P1285"] = "INSPECTION_INVALID_STATUS",
            ["P1286"] = "VISIT_ENTITY_MISMATCH",
            ["P1287"] = "DAMAGE_DECISION_PENDING",
            ["P1288"] = "VALIDATION_ERROR"
        };

    internal static string? ResolveBusinessCode(string sqlState, string messageText) =>
        BusinessCodesBySqlState.TryGetValue(sqlState, out var expectedCode) &&
        string.Equals(messageText, expectedCode, StringComparison.Ordinal)
            ? expectedCode
            : null;

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
            var code = ResolveBusinessCode(ex.SqlState, ex.MessageText);
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
