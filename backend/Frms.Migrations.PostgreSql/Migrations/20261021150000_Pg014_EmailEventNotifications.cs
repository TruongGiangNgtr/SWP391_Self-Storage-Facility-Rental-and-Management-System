using Frms.Migrations.PostgreSql.Persistence;
using Microsoft.EntityFrameworkCore.Infrastructure;
using Microsoft.EntityFrameworkCore.Migrations;

namespace Frms.Migrations.PostgreSql.Migrations;

/// <summary>
/// Adds opt-in, transactional event-to-NotificationLog hooks. No tables/columns change.
/// Event enqueue stays disabled until frms.email_events_enabled is explicitly set to on.
/// </summary>
[DbContext(typeof(FrmsDbContext))]
[Migration("20261021150000_Pg014_EmailEventNotifications")]
public sealed class Pg014_EmailEventNotifications : Migration
{
    protected override void Up(MigrationBuilder migrationBuilder)
    {
        using var stream = typeof(Pg014_EmailEventNotifications).Assembly
            .GetManifestResourceStream("Frms.DataAccess.PostgreSql.EmailEventNotifications.sql")
            ?? throw new InvalidOperationException("Missing EmailEventNotifications.sql embedded resource.");
        using var reader = new StreamReader(stream);
        migrationBuilder.Sql(reader.ReadToEnd());
    }

    protected override void Down(MigrationBuilder migrationBuilder)
    {
        // Disable delivery hooks without destroying existing NotificationLog history.
        migrationBuilder.Sql("""
            DROP TRIGGER IF EXISTS "trg_Email_Settlement_Status" ON public."DepositSettlement";
            DROP TRIGGER IF EXISTS "trg_Email_Settlement_Insert" ON public."DepositSettlement";
            DROP TRIGGER IF EXISTS "trg_Email_SupportTicket_Status" ON public."SupportTicket";
            DROP TRIGGER IF EXISTS "trg_Email_SupportTicket_Insert" ON public."SupportTicket";
            DROP TRIGGER IF EXISTS "trg_Email_Contract_Updates" ON public."Contract";
            DROP TRIGGER IF EXISTS "trg_Email_Contract_Insert" ON public."Contract";
            DROP TRIGGER IF EXISTS "trg_Email_Visit_Updates" ON public."Visit";
            DROP TRIGGER IF EXISTS "trg_Email_Visit_Insert" ON public."Visit";
            DROP TRIGGER IF EXISTS "trg_Email_Invoice_Status" ON public."Invoice";
            DROP TRIGGER IF EXISTS "trg_Email_Invoice_Insert" ON public."Invoice";
            DROP TRIGGER IF EXISTS "trg_Email_Reservation_Status" ON public."Reservation";
            DROP FUNCTION IF EXISTS public.frms_email_notify_settlement();
            DROP FUNCTION IF EXISTS public.frms_email_notify_support_ticket();
            DROP FUNCTION IF EXISTS public.frms_email_notify_contract();
            DROP FUNCTION IF EXISTS public.frms_email_notify_visit();
            DROP FUNCTION IF EXISTS public.frms_email_notify_invoice();
            DROP FUNCTION IF EXISTS public.frms_email_notify_reservation();
            DROP FUNCTION IF EXISTS public.frms_email_queue_customer_event(uuid, varchar, varchar);
            DROP FUNCTION IF EXISTS public.frms_email_events_enabled();
            """);
    }
}
