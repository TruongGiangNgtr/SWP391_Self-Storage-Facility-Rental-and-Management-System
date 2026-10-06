using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Design;

namespace Frms.DataAccess.Persistence;

public sealed class FrmsDbContextFactory : IDesignTimeDbContextFactory<FrmsDbContext>
{
    public FrmsDbContext CreateDbContext(string[] args)
    {
        var connectionString = Environment.GetEnvironmentVariable("FRMS_CONNECTION_STRING")
            ?? Environment.GetEnvironmentVariable("ConnectionStrings__Frms")
            ?? Environment.GetEnvironmentVariable("ConnectionStrings__FrmsDb");
        if (string.IsNullOrWhiteSpace(connectionString))
            throw new InvalidOperationException("SQL Server configuration is required for EF tools. Set ConnectionStrings__Frms or ConnectionStrings__FrmsDb. No default database is selected.");
        return new FrmsDbContext(new DbContextOptionsBuilder<FrmsDbContext>().UseSqlServer(connectionString).Options);
    }
}
