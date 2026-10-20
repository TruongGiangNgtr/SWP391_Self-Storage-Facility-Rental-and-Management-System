using Frms.Migrations.PostgreSql.Persistence;
using Microsoft.EntityFrameworkCore.Infrastructure;
using Microsoft.EntityFrameworkCore.Migrations;

namespace Frms.Migrations.PostgreSql.Migrations;

[DbContext(typeof(FrmsDbContext))]
[Migration("20261020153000_Pg013_HandoverUnitAssignments")]
public sealed class Pg013_HandoverUnitAssignments : Migration {
    protected override void Up(MigrationBuilder migrationBuilder) {
        using var stream = typeof(Pg013_HandoverUnitAssignments).Assembly
            .GetManifestResourceStream("Frms.DataAccess.PostgreSql.HandoverAssignments.sql")
            ?? throw new InvalidOperationException("Missing HandoverAssignments.sql embedded resource.");
        using var reader = new StreamReader(stream);
        migrationBuilder.Sql(reader.ReadToEnd());
    }

    protected override void Down(MigrationBuilder migrationBuilder) =>
        throw new NotSupportedException(
            "Handover assignment history is immutable. Restore a reviewed backup for rollback.");
}
