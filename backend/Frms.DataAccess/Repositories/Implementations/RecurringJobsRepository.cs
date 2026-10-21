using System.Data;
using System.Data.Common;
using Frms.DataAccess.Persistence;
using Frms.DataAccess.Repositories.Interfaces;
using Microsoft.EntityFrameworkCore;

namespace Frms.DataAccess.Repositories.Implementations;

internal sealed class RecurringJobsRepository(FrmsDbContext db) : IRecurringJobsRepository
{
    // Distinct from the transaction-scoped lifecycle guard in SQL (70439102).
    // Session lock remains held across external delivery, without a DB transaction.
    private const long NotificationLock = 70439120L;

    public Task ProcessReservationNoShowAsync(CancellationToken token) =>
        ExecuteRoutineAsync("SELECT * FROM public.\"usp_ProcessReservationNoShow\"()", token);

    public Task GenerateMonthlyInvoicesAsync(CancellationToken token) =>
        ExecuteRoutineAsync("SELECT public.\"usp_GenerateMonthlyInvoices\"()", token);

    public Task MarkOverdueInvoicesAsync(CancellationToken token) =>
        ExecuteRoutineAsync("SELECT * FROM public.\"usp_MarkOverdueInvoices\"()", token);

    public Task RecalculateOpenLateFeesAsync(CancellationToken token) =>
        ExecuteRoutineAsync("SELECT public.\"usp_RecalculateOpenLateFees\"()", token);

    private async Task ExecuteRoutineAsync(string sql, CancellationToken token)
    {
        var connection = db.Database.GetDbConnection();
        var shouldClose = connection.State != ConnectionState.Open;
        try
        {
            if (shouldClose) await connection.OpenAsync(token);
            await using var command = connection.CreateCommand();
            command.CommandType = CommandType.Text;
            command.CommandText = sql; // Hardcoded routine names; never user input.
            await command.ExecuteScalarAsync(token);
        }
        finally
        {
            if (shouldClose && connection.State == ConnectionState.Open)
                await connection.CloseAsync();
        }
    }

    public async Task<INotificationDeliveryLease?> TryAcquireNotificationAsync(CancellationToken token)
    {
        var connection = db.Database.GetDbConnection();
        await connection.OpenAsync(token);
        var locked = false;
        try
        {
            await using (var guard = connection.CreateCommand())
            {
                guard.CommandText = $"SELECT pg_try_advisory_lock({NotificationLock})";
                locked = (bool)(await guard.ExecuteScalarAsync(token) ?? false);
            }
            if (!locked)
            {
                await connection.CloseAsync();
                return null;
            }

            // Reuse the existing PostgreSQL queue routine. Its row-level lock
            // ends with this SELECT; the session advisory lock serializes senders.
            await using var command = connection.CreateCommand();
            command.CommandText = """
                SELECT q."NotificationLogId", q."UserAccountId", u."Email",
                       q."NotificationType", q."Content"
                FROM public."usp_RetryPendingNotification"() q
                JOIN public."UserAccount" u ON u."UserAccountId" = q."UserAccountId"
                """;
            await using var reader = await command.ExecuteReaderAsync(token);
            if (!await reader.ReadAsync(token))
            {
                await reader.DisposeAsync();
                await command.DisposeAsync();
                await ReleaseAsync(connection);
                return null;
            }
            var item = new PendingNotification(
                reader.GetGuid(0), reader.GetGuid(1), reader.GetString(2),
                reader.GetString(3), reader.GetString(4));
            return new NotificationLease(connection, item);
        }
        catch
        {
            if (locked) await ReleaseAsync(connection);
            else if (connection.State == ConnectionState.Open) await connection.CloseAsync();
            throw;
        }
    }

    private static async Task ReleaseAsync(DbConnection connection)
    {
        try
        {
            if (connection.State == ConnectionState.Open)
            {
                await using var release = connection.CreateCommand();
                release.CommandText = $"SELECT pg_advisory_unlock({NotificationLock})";
                await release.ExecuteScalarAsync();
            }
        }
        finally
        {
            if (connection.State == ConnectionState.Open)
                await connection.CloseAsync();
        }
    }

    private sealed class NotificationLease(DbConnection connection, PendingNotification notification)
        : INotificationDeliveryLease
    {
        private bool _disposed;
        public PendingNotification Notification => notification;

        public async Task MarkSentAsync(CancellationToken token)
        {
            if (_disposed) throw new ObjectDisposedException(nameof(NotificationLease));
            await using var command = connection.CreateCommand();
            command.CommandText = "SELECT public.\"usp_MarkNotificationSent\"(@id)";
            var parameter = command.CreateParameter();
            parameter.ParameterName = "id";
            parameter.DbType = DbType.Guid;
            parameter.Value = notification.NotificationLogId;
            command.Parameters.Add(parameter);
            await command.ExecuteScalarAsync(token);
        }

        public async ValueTask DisposeAsync()
        {
            if (_disposed) return;
            _disposed = true;
            await ReleaseAsync(connection);
        }
    }
}
