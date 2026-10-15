using DotNetEnv;
using DotNetEnv.Configuration;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Design;
using Microsoft.Extensions.Configuration;

namespace Frms.Migrations.PostgreSql.Persistence;

public sealed class FrmsDbContextFactory : IDesignTimeDbContextFactory<FrmsDbContext>
{
    public FrmsDbContext CreateDbContext(string[] args)
    {
        // Explicit process configuration always wins; never mutate environment
        // variables or implicitly choose a database for tooling.
        var connectionString = ReadProcessConnection();
        if (connectionString is null && IsDevelopment(args))
            connectionString = ReadLocalConnection();

        if (string.IsNullOrWhiteSpace(connectionString))
            throw new InvalidOperationException("Database configuration is required for EF tools. Set ConnectionStrings__Frms or ConnectionStrings__FrmsDb, or configure the ignored API .env for Development.");

        return new FrmsDbContext(new DbContextOptionsBuilder<FrmsDbContext>().UseNpgsql(connectionString).Options);
    }

    private static string? ReadProcessConnection() =>
        Environment.GetEnvironmentVariable("FRMS_CONNECTION_STRING")
        ?? Environment.GetEnvironmentVariable("ConnectionStrings__Frms")
        ?? Environment.GetEnvironmentVariable("ConnectionStrings__FrmsDb");

    private static bool IsDevelopment(string[] args)
    {
        var environment = Environment.GetEnvironmentVariable("DOTNET_ENVIRONMENT")
            ?? Environment.GetEnvironmentVariable("ASPNETCORE_ENVIRONMENT")
            ?? "Development";
        for (var index = 0; index < args.Length; index++)
        {
            if (args[index] == "--environment" && index + 1 < args.Length)
                environment = args[++index];
            else if (args[index].StartsWith("--environment=", StringComparison.Ordinal))
                environment = args[index]["--environment=".Length..];
        }
        return string.Equals(environment, "Development", StringComparison.OrdinalIgnoreCase);
    }

    private static string? ReadLocalConnection()
    {
        var path = FindLocalEnv(Directory.GetCurrentDirectory())
            ?? FindLocalEnv(AppContext.BaseDirectory);
        if (path is null || !File.Exists(path)) return null;

        try
        {
            var configuration = new ConfigurationBuilder().AddDotNetEnv(path, LoadOptions.NoEnvVars()).Build();
            using var configurationLifetime = configuration as IDisposable;
            return configuration["FRMS_CONNECTION_STRING"]
                ?? configuration.GetConnectionString("Frms")
                ?? configuration.GetConnectionString("FrmsDb");
        }
        catch (Exception)
        {
            // A parser exception can contain local secrets. Do not retain the
            // exception as an InnerException or echo file content/path values.
            throw new InvalidOperationException("Unable to load the API .env for EF tools. Check file access and dotenv syntax.");
        }
    }

    private static string? FindLocalEnv(string start)
    {
        for (var directory = new DirectoryInfo(start); directory is not null; directory = directory.Parent)
        {
            var backend = File.Exists(Path.Combine(directory.FullName, "Frms.slnx"))
                ? directory.FullName
                : Path.Combine(directory.FullName, "backend");
            if (File.Exists(Path.Combine(backend, "Frms.slnx")))
                return Path.Combine(backend, "Frms.Api", ".env");
        }
        return null;
    }
}
