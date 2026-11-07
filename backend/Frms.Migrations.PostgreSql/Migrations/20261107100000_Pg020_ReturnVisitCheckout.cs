using Frms.Migrations.PostgreSql.Persistence;
using Microsoft.EntityFrameworkCore.Infrastructure;
using Microsoft.EntityFrameworkCore.Migrations;

namespace Frms.Migrations.PostgreSql.Migrations;

/// <summary>
/// Close a confirmed physical RETURN Visit inside atomic Finalize Return.
/// Pg005 historical Returns.sql and prior migrations remain immutable.
/// </summary>
[DbContext(typeof(FrmsDbContext))]
[Migration("20261107100000_Pg020_ReturnVisitCheckout")]
public sealed class Pg020_ReturnVisitCheckout : Migration
{
    protected override void Up(MigrationBuilder migrationBuilder)
    {
        using var stream = typeof(Pg020_ReturnVisitCheckout).Assembly
            .GetManifestResourceStream("Frms.DataAccess.PostgreSql.ReturnVisitCheckout.sql")
            ?? throw new InvalidOperationException("Missing ReturnVisitCheckout.sql embedded resource.");
        using var reader = new StreamReader(stream);
        migrationBuilder.Sql(reader.ReadToEnd());
    }

    protected override void Down(MigrationBuilder migrationBuilder) =>
        throw new NotSupportedException(
            "Return lifecycle and audit history must not be silently downgraded. " +
            "Use a reviewed forward repair.");
}
