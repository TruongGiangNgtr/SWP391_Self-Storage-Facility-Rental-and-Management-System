using Frms.Migrations.PostgreSql.Persistence;
using Microsoft.EntityFrameworkCore.Infrastructure;
using Microsoft.EntityFrameworkCore.Migrations;

namespace Frms.Migrations.PostgreSql.Migrations;

[DbContext(typeof(FrmsDbContext))]
[Migration("20261014163000_Pg004_VisitsAdministrationAndSupport")]
public sealed class Pg004_VisitsAdministrationAndSupport : Migration
{
    protected override void Up(MigrationBuilder migrationBuilder)
    {
        foreach (var name in new[] { "Administration.sql", "Visits.sql", "Support.sql" })
        {
            using var stream = typeof(Pg004_VisitsAdministrationAndSupport).Assembly
                .GetManifestResourceStream("Frms.DataAccess.PostgreSql." + name)
                ?? throw new InvalidOperationException("Missing PostgreSQL migration resource.");
            using var reader = new StreamReader(stream);
            migrationBuilder.Sql(reader.ReadToEnd());
        }
    }
    protected override void Down(MigrationBuilder migrationBuilder) =>
        throw new NotSupportedException("Routine downgrade requires a reviewed compatible redeploy.");
}
