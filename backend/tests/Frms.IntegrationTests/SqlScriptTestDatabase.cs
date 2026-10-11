using System.Collections.Concurrent;
using System.Text.RegularExpressions;
using Frms.DataAccess.Persistence;
using Microsoft.Data.SqlClient;
using Microsoft.EntityFrameworkCore;

namespace Frms.IntegrationTests;

// Test-only bootstrap: use the same standalone SQL file shipped to the team.
// Never infer a connection or adopt/drop a pre-existing database.
internal static class SqlScriptTestDatabase
{
    private static readonly ConcurrentDictionary<string, byte> Created = new();

    internal static async Task EnsureAsync(FrmsDbContext context)
    {
        var builder = new SqlConnectionStringBuilder(context.Database.GetConnectionString());
        Guard(builder.InitialCatalog);
        if (await context.Database.CanConnectAsync())
        {
            var count = await context.Database.SqlQueryRaw<int>(
                "SELECT COUNT(*) AS [Value] FROM sys.tables WHERE is_ms_shipped=0 AND name<>'__EFMigrationsHistory'").SingleAsync();
            Assert.That(count, Is.EqualTo(27), "Provision the dedicated test database using the standalone SQL baseline.");
            return;
        }

        var target = builder.InitialCatalog;
        builder.InitialCatalog = "master";
        await using var connection = new SqlConnection(builder.ConnectionString);
        await connection.OpenAsync();
        await using var command = connection.CreateCommand();
        command.CommandText = "SELECT DB_ID(@Name)";
        command.Parameters.AddWithValue("@Name", target);
        Assert.That(await command.ExecuteScalarAsync(), Is.EqualTo(DBNull.Value),
            "Do not modify an existing database that could not be connected to.");
        command.Parameters.Clear();
        command.CommandTimeout = 60;
        var script = await File.ReadAllTextAsync(ScriptPath());
        // The owner renamed the export target from FRMS_Team to FRMS.
        // Replace database identifiers/guard literals only, never demo row names.
        var targetedScript = script.Replace("[FRMS_Team]", "[" + target + "]", StringComparison.Ordinal)
            .Replace("N'FRMS_Team'", "N'" + target + "'", StringComparison.Ordinal)
            .Replace("[FRMS]", "[" + target + "]", StringComparison.Ordinal)
            .Replace("N'FRMS'", "N'" + target + "'", StringComparison.Ordinal);
        var batches = Regex.Split(targetedScript, @"(?im)^\s*GO\s*$");
        for (var index = 0; index < batches.Length; index++)
        {
            if (string.IsNullOrWhiteSpace(batches[index])) continue;
            command.CommandText = batches[index];
            await command.ExecuteNonQueryAsync();
            if (index == 1) Created.TryAdd(Key(builder.DataSource, target), 0);
        }
    }

    internal static async Task DropCreatedAsync(FrmsDbContext context)
    {
        var builder = new SqlConnectionStringBuilder(context.Database.GetConnectionString());
        var target = builder.InitialCatalog;
        Guard(target);
        Assert.That(Created.TryRemove(Key(builder.DataSource, target), out _), Is.True,
            "Only a database created by this test bootstrap may be deleted.");
        await context.Database.CloseConnectionAsync();
        using (var pool = new SqlConnection(builder.ConnectionString)) SqlConnection.ClearPool(pool);
        builder.InitialCatalog = "master";
        await using var connection = new SqlConnection(builder.ConnectionString);
        await connection.OpenAsync();
        await using var command = connection.CreateCommand();
        command.CommandText = "DROP DATABASE [" + target + "]";
        await command.ExecuteNonQueryAsync();
    }

    private static void Guard(string name) => Assert.That(
        Regex.IsMatch(name, @"^Frms_Test_[A-Za-z0-9_]{1,118}$"), Is.True,
        "Only an explicitly configured disposable Frms_Test_* database is allowed.");

    private static string Key(string server, string name) => server + "/" + name;

    private static string ScriptPath()
    {
        for (var directory = new DirectoryInfo(AppContext.BaseDirectory); directory is not null; directory = directory.Parent)
        {
            var path = Path.Combine(directory.FullName, "sql", "FRMS_EPS01_Team_Schema_Seed_20261011.sql");
            if (File.Exists(path)) return path;
        }
        throw new FileNotFoundException("Standalone SQL baseline not found under backend/sql.");
    }
}
