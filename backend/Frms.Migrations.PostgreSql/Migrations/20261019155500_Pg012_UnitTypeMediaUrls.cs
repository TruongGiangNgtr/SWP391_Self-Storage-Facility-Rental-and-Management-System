using Frms.Migrations.PostgreSql.Persistence;
using Microsoft.EntityFrameworkCore.Infrastructure;
using Microsoft.EntityFrameworkCore.Migrations;

namespace Frms.Migrations.PostgreSql.Migrations;

/// <summary>
/// Adds optional UnitType media URLs. Does not backfill unverified CDN URLs or
/// modify existing UnitType names, modes, sizes, prices or descriptions.
/// </summary>
[DbContext(typeof(FrmsDbContext))]
[Migration("20261019155500_Pg012_UnitTypeMediaUrls")]
public sealed class Pg012_UnitTypeMediaUrls : Migration
{
    protected override void Up(MigrationBuilder migrationBuilder)
    {
        migrationBuilder.AddColumn<string>(
            name: "ImageUrl",
            schema: "public",
            table: "UnitType",
            type: "character varying(2048)",
            maxLength: 2048,
            nullable: true,
            collation: "frms_ci_as");

        migrationBuilder.AddColumn<string>(
            name: "VideoUrl",
            schema: "public",
            table: "UnitType",
            type: "character varying(2048)",
            maxLength: 2048,
            nullable: true,
            collation: "frms_ci_as");
    }

    protected override void Down(MigrationBuilder migrationBuilder)
    {
        // Protect manually maintained media URLs from accidental data loss.
        migrationBuilder.Sql("""
            DO $frms$
            BEGIN
                IF EXISTS (
                    SELECT 1 FROM public."UnitType"
                    WHERE "ImageUrl" IS NOT NULL OR "VideoUrl" IS NOT NULL
                ) THEN
                    RAISE EXCEPTION 'Pg012 rollback would remove UnitType media URL data; clear only after reviewed backup.';
                END IF;
            END $frms$;
            """);

        migrationBuilder.DropColumn(name: "ImageUrl", schema: "public", table: "UnitType");
        migrationBuilder.DropColumn(name: "VideoUrl", schema: "public", table: "UnitType");
    }
}
