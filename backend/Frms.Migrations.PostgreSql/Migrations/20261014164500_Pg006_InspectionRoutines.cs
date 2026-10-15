using Frms.Migrations.PostgreSql.Persistence;
using Microsoft.EntityFrameworkCore.Infrastructure;
using Microsoft.EntityFrameworkCore.Migrations;

namespace Frms.Migrations.PostgreSql.Migrations;
[DbContext(typeof(FrmsDbContext))]
[Migration("20261014164500_Pg006_InspectionRoutines")]
public sealed class Pg006_InspectionRoutines : Migration
{
    protected override void Up(MigrationBuilder migrationBuilder)
    {
        using var stream = typeof(Pg006_InspectionRoutines).Assembly
            .GetManifestResourceStream("Frms.DataAccess.PostgreSql.Inspections.sql")
            ?? throw new InvalidOperationException("Missing PostgreSQL migration resource.");
        using var reader = new StreamReader(stream);
        migrationBuilder.Sql(reader.ReadToEnd());
    }
    protected override void Down(MigrationBuilder migrationBuilder) =>
        throw new NotSupportedException("Inspection/decision downgrade requires a reviewed compatible redeploy.");
}
