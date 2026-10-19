using Frms.Migrations.PostgreSql.Persistence;
using Microsoft.EntityFrameworkCore.Infrastructure;
using Microsoft.EntityFrameworkCore.Migrations;

namespace Frms.Migrations.PostgreSql.Migrations;

/// <summary>
/// Ports the existing SQL Server demo-only Visit check-in routine without
/// changing the production usp_CheckInVisit function or application policy.
/// </summary>
[DbContext(typeof(FrmsDbContext))]
[Migration("20261019150000_Pg011_DemoVisitCheckIn")]
public sealed class Pg011_DemoVisitCheckIn : Migration
{
    protected override void Up(MigrationBuilder migrationBuilder)
    {
        using var stream = typeof(Pg011_DemoVisitCheckIn).Assembly
            .GetManifestResourceStream("Frms.DataAccess.PostgreSql.CheckInVisitDemo.sql")
            ?? throw new InvalidOperationException("Missing PostgreSQL demo check-in migration resource.");
        using var reader = new StreamReader(stream);
        migrationBuilder.Sql(reader.ReadToEnd());
    }

    protected override void Down(MigrationBuilder migrationBuilder)
    {
        // Removing a demo function does not undo Visit or Contract evidence.
        // Existing production routines, tables and triggers are untouched.
        migrationBuilder.Sql("""
            DROP FUNCTION IF EXISTS public."usp_CheckInVisitDemo"(uuid, uuid, timestamptz);
            """);
    }
}
