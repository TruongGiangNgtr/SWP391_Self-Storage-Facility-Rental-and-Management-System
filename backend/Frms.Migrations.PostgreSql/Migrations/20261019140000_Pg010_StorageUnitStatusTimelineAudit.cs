using Frms.Migrations.PostgreSql.Persistence;
using Microsoft.EntityFrameworkCore.Infrastructure;
using Microsoft.EntityFrameworkCore.Migrations;

namespace Frms.Migrations.PostgreSql.Migrations;

[DbContext(typeof(FrmsDbContext))]
[Migration("20261019140000_Pg010_StorageUnitStatusTimelineAudit")]
public sealed class Pg010_StorageUnitStatusTimelineAudit : Migration
{
    protected override void Up(MigrationBuilder migrationBuilder)
    {
        // New evidence starts when this migration is applied. No historical backfill.
        migrationBuilder.Sql("""
            CREATE OR REPLACE FUNCTION public.frms_audit_storage_unit_status()
            RETURNS trigger LANGUAGE plpgsql AS $frms$
            DECLARE v_actor uuid; v_actor_text text;
            BEGIN
                IF TG_OP = 'UPDATE' AND OLD."Status" IS NOT DISTINCT FROM NEW."Status" THEN
                    RETURN NEW;
                END IF;
                v_actor_text := nullif(current_setting('frms.user_account_id', true), '');
                BEGIN
                    v_actor := v_actor_text::uuid;
                EXCEPTION WHEN invalid_text_representation THEN
                    v_actor := NULL;
                END;
                INSERT INTO public."AuditLog"
                    ("UserAccountId", "Action", "EntityType", "EntityId", "OldValue", "NewValue", "CreatedAt")
                VALUES
                    (v_actor,
                     CASE WHEN TG_OP = 'INSERT' THEN 'CREATE_STORAGE_UNIT' ELSE 'CHANGE_STORAGE_UNIT_STATUS' END,
                     'STORAGE_UNIT', NEW."StorageUnitId",
                     CASE WHEN TG_OP = 'UPDATE' THEN jsonb_build_object('status', OLD."Status")::text ELSE NULL END,
                     jsonb_build_object('status', NEW."Status")::text,
                     clock_timestamp());
                RETURN NEW;
            END $frms$;

            CREATE TRIGGER "trg_Audit_StorageUnit_StatusHistory"
            AFTER INSERT OR UPDATE OF "Status" ON public."StorageUnit"
            FOR EACH ROW EXECUTE FUNCTION public.frms_audit_storage_unit_status();

            CREATE INDEX IF NOT EXISTS "IX_AuditLog_StorageUnitTimeline"
            ON public."AuditLog" ("EntityId", "CreatedAt" DESC, "AuditLogId" DESC)
            WHERE "EntityType" = 'STORAGE_UNIT';
            """);
    }

    protected override void Down(MigrationBuilder migrationBuilder) =>
        throw new NotSupportedException(
            "Historical audit capture cannot be removed without reviewed data-retention and rollback procedures.");
}
