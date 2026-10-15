using Frms.Migrations.PostgreSql.Persistence;
using Microsoft.EntityFrameworkCore.Infrastructure;
using Microsoft.EntityFrameworkCore.Migrations;

namespace Frms.Migrations.PostgreSql.Migrations;

[DbContext(typeof(FrmsDbContext))]
[Migration("20261015210000_Pg009_PostgreSql17StringSearch")]
public sealed class Pg009_PostgreSql17StringSearch : Migration
{
    protected override void Up(MigrationBuilder migrationBuilder)
    {
        using var stream = typeof(Pg009_PostgreSql17StringSearch).Assembly
            .GetManifestResourceStream("Frms.DataAccess.PostgreSql.StringSearch.sql")
            ?? throw new InvalidOperationException("Missing PostgreSQL migration resource.");
        using var reader = new StreamReader(stream);
        migrationBuilder.Sql(reader.ReadToEnd());
    }

    protected override void Down(MigrationBuilder migrationBuilder) =>
        throw new NotSupportedException("String-search downgrade requires a reviewed compatible runtime redeploy.");
}
