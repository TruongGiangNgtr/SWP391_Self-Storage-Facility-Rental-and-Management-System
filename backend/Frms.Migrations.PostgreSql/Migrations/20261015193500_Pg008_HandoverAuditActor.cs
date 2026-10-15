using Frms.Migrations.PostgreSql.Persistence;
using Microsoft.EntityFrameworkCore.Infrastructure;
using Microsoft.EntityFrameworkCore.Migrations;

namespace Frms.Migrations.PostgreSql.Migrations;
[DbContext(typeof(FrmsDbContext))]
[Migration("20261015193500_Pg008_HandoverAuditActor")]
public sealed class Pg008_HandoverAuditActor : Migration
{
    protected override void Up(MigrationBuilder migrationBuilder)
    {
        using var stream = typeof(Pg008_HandoverAuditActor).Assembly
            .GetManifestResourceStream("Frms.DataAccess.PostgreSql.HandoverAuditActor.sql")
            ?? throw new InvalidOperationException("Missing PostgreSQL migration resource.");
        using var reader = new StreamReader(stream);
        migrationBuilder.Sql(reader.ReadToEnd());
    }
    protected override void Down(MigrationBuilder migrationBuilder) =>
        throw new NotSupportedException("Audit identity protection requires a reviewed compatible redeploy.");
}
