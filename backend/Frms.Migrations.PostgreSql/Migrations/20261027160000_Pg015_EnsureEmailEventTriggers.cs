using Frms.Migrations.PostgreSql.Persistence;
using Microsoft.EntityFrameworkCore.Infrastructure;
using Microsoft.EntityFrameworkCore.Migrations;

namespace Frms.Migrations.PostgreSql.Migrations;

/// <summary>
/// Forward-only repair for Pg014 email event triggers. Runs after Pg014.
/// Pg014 owns all notification functions; this migration creates missing triggers only.
/// No notification delivery or background worker is enabled by this migration.
/// </summary>
[DbContext(typeof(FrmsDbContext))]
[Migration("20261027160000_Pg015_EnsureEmailEventTriggers")]
public sealed class Pg015_EnsureEmailEventTriggers : Migration
{
    protected override void Up(MigrationBuilder migrationBuilder)
    {
        migrationBuilder.Sql("""
DO $frms$
DECLARE
    r record;
    v_missing_functions text[] := ARRAY[]::text[];
BEGIN
    FOR r IN
        SELECT * FROM (VALUES
          ('Reservation',       'trg_Email_Reservation_Status',  'public.frms_email_notify_reservation()',    'AFTER UPDATE OF "Status"'),
          ('Invoice',           'trg_Email_Invoice_Insert',      'public.frms_email_notify_invoice()',        'AFTER INSERT'),
          ('Invoice',           'trg_Email_Invoice_Status',      'public.frms_email_notify_invoice()',        'AFTER UPDATE OF "Status"'),
          ('Visit',             'trg_Email_Visit_Insert',        'public.frms_email_notify_visit()',          'AFTER INSERT'),
          ('Visit',             'trg_Email_Visit_Updates',       'public.frms_email_notify_visit()',          'AFTER UPDATE OF "Status", "VisitDate"'),
          ('Contract',          'trg_Email_Contract_Insert',     'public.frms_email_notify_contract()',       'AFTER INSERT'),
          ('Contract',          'trg_Email_Contract_Updates',    'public.frms_email_notify_contract()',       'AFTER UPDATE OF "Status", "EndMonth"'),
          ('SupportTicket',     'trg_Email_SupportTicket_Insert','public.frms_email_notify_support_ticket()', 'AFTER INSERT'),
          ('SupportTicket',     'trg_Email_SupportTicket_Status','public.frms_email_notify_support_ticket()', 'AFTER UPDATE OF "Status"'),
          ('DepositSettlement', 'trg_Email_Settlement_Insert',   'public.frms_email_notify_settlement()',     'AFTER INSERT'),
          ('DepositSettlement', 'trg_Email_Settlement_Status',   'public.frms_email_notify_settlement()',     'AFTER UPDATE OF "Status"')
        ) AS t(table_name, trigger_name, function_signature, event_clause)
    LOOP
        IF to_regprocedure(r.function_signature) IS NULL THEN
            v_missing_functions := array_append(v_missing_functions, r.function_signature);
        END IF;
    END LOOP;
    IF cardinality(v_missing_functions) > 0 THEN
        RAISE EXCEPTION 'Pg014 functions missing: %. Apply Pg014 EF migration first; nothing changed.', array_to_string(v_missing_functions, ', ');
    END IF;
    FOR r IN
        SELECT * FROM (VALUES
          ('Reservation',       'trg_Email_Reservation_Status',  'public.frms_email_notify_reservation()',    'AFTER UPDATE OF "Status"'),
          ('Invoice',           'trg_Email_Invoice_Insert',      'public.frms_email_notify_invoice()',        'AFTER INSERT'),
          ('Invoice',           'trg_Email_Invoice_Status',      'public.frms_email_notify_invoice()',        'AFTER UPDATE OF "Status"'),
          ('Visit',             'trg_Email_Visit_Insert',        'public.frms_email_notify_visit()',          'AFTER INSERT'),
          ('Visit',             'trg_Email_Visit_Updates',       'public.frms_email_notify_visit()',          'AFTER UPDATE OF "Status", "VisitDate"'),
          ('Contract',          'trg_Email_Contract_Insert',     'public.frms_email_notify_contract()',       'AFTER INSERT'),
          ('Contract',          'trg_Email_Contract_Updates',    'public.frms_email_notify_contract()',       'AFTER UPDATE OF "Status", "EndMonth"'),
          ('SupportTicket',     'trg_Email_SupportTicket_Insert','public.frms_email_notify_support_ticket()', 'AFTER INSERT'),
          ('SupportTicket',     'trg_Email_SupportTicket_Status','public.frms_email_notify_support_ticket()', 'AFTER UPDATE OF "Status"'),
          ('DepositSettlement', 'trg_Email_Settlement_Insert',   'public.frms_email_notify_settlement()',     'AFTER INSERT'),
          ('DepositSettlement', 'trg_Email_Settlement_Status',   'public.frms_email_notify_settlement()',     'AFTER UPDATE OF "Status"')
        ) AS t(table_name, trigger_name, function_signature, event_clause)
    LOOP
        IF to_regclass(format('public.%I', r.table_name)) IS NULL THEN
            RAISE EXCEPTION 'Missing table public.%', r.table_name;
        END IF;
        IF NOT EXISTS (
            SELECT 1 FROM pg_catalog.pg_trigger t
            JOIN pg_catalog.pg_class c ON c.oid = t.tgrelid
            JOIN pg_catalog.pg_namespace n ON n.oid = c.relnamespace
            WHERE n.nspname = 'public' AND c.relname = r.table_name
              AND t.tgname = r.trigger_name AND NOT t.tgisinternal
        ) THEN
            EXECUTE format('CREATE TRIGGER %I %s ON public.%I FOR EACH ROW EXECUTE FUNCTION %s',
                           r.trigger_name, r.event_clause, r.table_name, r.function_signature);
            RAISE NOTICE 'Created %.%', r.table_name, r.trigger_name;
        ELSE
            RAISE NOTICE 'Already exists %.%', r.table_name, r.trigger_name;
        END IF;
    END LOOP;
END $frms$;

DO $verify$
DECLARE v_count int; v_enabled int;
BEGIN
    SELECT count(*), count(*) FILTER (WHERE t.tgenabled = 'O')
      INTO v_count, v_enabled
    FROM pg_catalog.pg_trigger t
    JOIN pg_catalog.pg_class c ON c.oid = t.tgrelid
    JOIN pg_catalog.pg_namespace n ON n.oid = c.relnamespace
    WHERE n.nspname='public' AND NOT t.tgisinternal AND t.tgname LIKE 'trg_Email_%';
    IF v_count != 11 OR v_enabled != 11 THEN
        RAISE EXCEPTION 'Pg014 trigger verification FAIL: total=%, enabled=% (expected 11/11)', v_count, v_enabled;
    END IF;
    RAISE NOTICE 'PASS: Pg014 has 11/11 enabled email triggers.';
END $verify$;
""");
    }

    protected override void Down(MigrationBuilder migrationBuilder) =>
        throw new NotSupportedException(
            "Email event triggers are forward-only. Use reviewed forward migrations for changes.");
}
