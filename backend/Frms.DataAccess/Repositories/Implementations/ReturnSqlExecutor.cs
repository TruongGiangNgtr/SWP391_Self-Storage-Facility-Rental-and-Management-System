using System.Data;
using Frms.DataAccess.Persistence;
using Frms.DataAccess.StoredProcedures;
using Microsoft.Data.SqlClient;
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

    public async Task ExecuteAsync(
        string procedure,
        CancellationToken ct,
        params (string Name, DbType Type, object? Value)[] arguments)
    {
        var connection = db.Database.GetDbConnection();
        var mustClose = connection.State != ConnectionState.Open;
        try
        {
            if (mustClose) await connection.OpenAsync(ct);
            await using var command = connection.CreateCommand();
            command.CommandText = "dbo." + procedure;
            command.CommandType = CommandType.StoredProcedure;
            foreach (var (name, type, value) in arguments)
            {
                var p = command.CreateParameter();
                p.ParameterName = name;
                p.DbType = type;
                p.Value = value ?? DBNull.Value;
                if (type == DbType.Binary && p is SqlParameter sqlParam)
                    sqlParam.Size = -1;
                command.Parameters.Add(p);
            }
            await command.ExecuteNonQueryAsync(ct);
        }
        catch (SqlException ex)
        {
            var code = KnownCodes.FirstOrDefault(c =>
                ex.Message.Contains(c, StringComparison.OrdinalIgnoreCase));
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
