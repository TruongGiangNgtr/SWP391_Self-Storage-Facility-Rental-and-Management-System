using Frms.Migrations.PostgreSql.Persistence;
using Microsoft.EntityFrameworkCore.Infrastructure;
using Microsoft.EntityFrameworkCore.Migrations;

namespace Frms.Migrations.PostgreSql.Migrations;

[DbContext(typeof(FrmsDbContext))]
[Migration("20261116090000_Pg022_PayOsSessionQrCode")]
public sealed class Pg022_PayOsSessionQrCode : Migration
{
    protected override void Up(MigrationBuilder migrationBuilder) =>
        migrationBuilder.AddColumn<string>(
            name: "QrCode",
            schema: "public",
            table: "Payment",
            type: "character varying(2048)",
            maxLength: 2048,
            nullable: true,
            collation: "frms_ci_as");

    protected override void Down(MigrationBuilder migrationBuilder) =>
        migrationBuilder.DropColumn(name: "QrCode", schema: "public", table: "Payment");
}
