using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Design;

namespace Frms.DataAccess.Persistence;

public sealed class FrmsDbContextFactory : IDesignTimeDbContextFactory<FrmsDbContext>
{
    public FrmsDbContext CreateDbContext(string[] args)
    {
        var connectionString = Environment.GetEnvironmentVariable("FRMS_CONNECTION_STRING")
            ?? Environment.GetEnvironmentVariable("ConnectionStrings__Frms")
            ?? "Server=localhost;Database=Frms_DesignTime;Trusted_Connection=True;TrustServerCertificate=True";
        return new FrmsDbContext(new DbContextOptionsBuilder<FrmsDbContext>().UseSqlServer(connectionString).Options);
    }
}
