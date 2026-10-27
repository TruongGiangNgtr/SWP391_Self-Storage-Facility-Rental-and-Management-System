using Frms.Migrations.PostgreSql.Persistence;
using Microsoft.EntityFrameworkCore.Infrastructure;
using Microsoft.EntityFrameworkCore.Migrations;

namespace Frms.Migrations.PostgreSql.Migrations;

/// <summary>
/// Keep Pg014 email-event triggers, but queue eligible events without a PostgreSQL
/// role/database/session GUC. Delivery remains controlled separately by
/// BackgroundJobs:Notifications:Enabled and the configured INotificationSender.
/// </summary>
[DbContext(typeof(FrmsDbContext))]
[Migration("20261027170000_Pg016_RemoveEmailEventQueueGate")]
public sealed class Pg016_RemoveEmailEventQueueGate : Migration
{
    protected override void Up(MigrationBuilder migrationBuilder)
    {
        migrationBuilder.Sql("""
            CREATE OR REPLACE FUNCTION public.frms_email_events_enabled()
            RETURNS boolean LANGUAGE sql STABLE AS $frms$
                SELECT true;
            $frms$;
            """);
    }

    protected override void Down(MigrationBuilder migrationBuilder)
    {
        migrationBuilder.Sql("""
            CREATE OR REPLACE FUNCTION public.frms_email_events_enabled()
            RETURNS boolean LANGUAGE sql STABLE AS $frms$
                SELECT coalesce(current_setting('frms.email_events_enabled', true) = 'on', false);
            $frms$;
            """);
    }
}
