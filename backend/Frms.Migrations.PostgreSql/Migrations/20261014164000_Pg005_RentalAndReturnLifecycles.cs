using Frms.Migrations.PostgreSql.Persistence;
using Microsoft.EntityFrameworkCore.Infrastructure;
using Microsoft.EntityFrameworkCore.Migrations;

namespace Frms.Migrations.PostgreSql.Migrations;
[DbContext(typeof(FrmsDbContext))]
[Migration("20261014164000_Pg005_RentalAndReturnLifecycles")]
public sealed class Pg005_RentalAndReturnLifecycles : Migration
{
    protected override void Up(MigrationBuilder migrationBuilder)
    {
        foreach (var name in new[] { "RentalLifecycles.sql", "Returns.sql" })
        {
            using var stream = typeof(Pg005_RentalAndReturnLifecycles).Assembly
                .GetManifestResourceStream("Frms.DataAccess.PostgreSql." + name)
                ?? throw new InvalidOperationException("Missing PostgreSQL migration resource.");
            using var reader = new StreamReader(stream);
            migrationBuilder.Sql(reader.ReadToEnd());
        }
    }
    protected override void Down(MigrationBuilder migrationBuilder) =>
        throw new NotSupportedException("Financial lifecycle downgrade requires a reviewed compatible redeploy.");
}
