using System.Data;
using System.Data.Common;
using Microsoft.EntityFrameworkCore.Diagnostics;

namespace Frms.DataAccess.Persistence;

internal sealed class DatabaseAuditActor(Func<Guid?> readActor)
{
    public Guid? UserAccountId => readActor();
}

// Session-affine direct/session-pool connections only. Every open sets an
// explicit actor, including anonymous/jobs, so pooled connections cannot inherit it.
internal sealed class PostgreSqlAuditInterceptor(DatabaseAuditActor? actor = null) : DbConnectionInterceptor
{
    private DbCommand CreateCommand(DbConnection connection)
    {
        var command = connection.CreateCommand();
        command.CommandText = "SELECT set_config('frms.user_account_id', @actor, false)";
        var parameter = command.CreateParameter();
        parameter.ParameterName = "actor";
        parameter.DbType = DbType.String;
        parameter.Value = actor?.UserAccountId?.ToString("D") ?? string.Empty;
        command.Parameters.Add(parameter);
        return command;
    }

    public override void ConnectionOpened(DbConnection connection, ConnectionEndEventData eventData)
    {
        using var command = CreateCommand(connection);
        command.ExecuteNonQuery();
    }

    public override async Task ConnectionOpenedAsync(DbConnection connection,
        ConnectionEndEventData eventData, CancellationToken cancellationToken = default)
    {
        await using var command = CreateCommand(connection);
        await command.ExecuteNonQueryAsync(cancellationToken);
    }
}
