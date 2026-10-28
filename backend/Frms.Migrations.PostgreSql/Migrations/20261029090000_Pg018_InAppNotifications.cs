using Frms.Migrations.PostgreSql.Persistence;
using Microsoft.EntityFrameworkCore.Infrastructure;
using Microsoft.EntityFrameworkCore.Migrations;

namespace Frms.Migrations.PostgreSql.Migrations;

/// <summary>
/// OD-06 / REV-2026-10-29-INAPP-NOTIFICATIONS. Approved NOT-D01/02:
/// new in-app provenance only, no legacy publication/backfill or email delivery.
/// </summary>
[DbContext(typeof(FrmsDbContext))]
[Migration("20261029090000_Pg018_InAppNotifications")]
public sealed class Pg018_InAppNotifications : Migration
{
    protected override void Up(MigrationBuilder migrationBuilder)
    {
        // Acquire the same lock as legacy sender leases before DDL or rerouting.
        migrationBuilder.Sql("SELECT pg_advisory_xact_lock(70439120);");
        migrationBuilder.AddColumn<bool>(
            name: "IsInApp", schema: "public", table: "NotificationLog",
            type: "boolean", nullable: false, defaultValue: false);
        using var stream = typeof(Pg018_InAppNotifications).Assembly
            .GetManifestResourceStream("Frms.DataAccess.PostgreSql.InAppNotifications.sql")
            ?? throw new InvalidOperationException("Missing InAppNotifications.sql embedded resource.");
        using var reader = new StreamReader(stream);
        migrationBuilder.Sql(reader.ReadToEnd());
        migrationBuilder.CreateIndex(
            name: "IX_NotificationLog_InApp", schema: "public", table: "NotificationLog",
            columns: ["UserAccountId", "CreatedAt", "NotificationLogId"], filter: "\"IsInApp\"");
    }

    protected override void Down(MigrationBuilder migrationBuilder) =>
        throw new NotSupportedException(
            "In-app provenance and historical delivery must be preserved. Deploy a reviewed forward repair; do not downgrade Pg018.");
}