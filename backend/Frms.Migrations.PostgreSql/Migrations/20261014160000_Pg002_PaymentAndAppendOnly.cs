using Frms.Migrations.PostgreSql.Persistence;
using Microsoft.EntityFrameworkCore.Infrastructure;
using Microsoft.EntityFrameworkCore.Migrations;

namespace Frms.Migrations.PostgreSql.Migrations;

[DbContext(typeof(FrmsDbContext))]
[Migration("20261014160000_Pg002_PaymentAndAppendOnly")]
public sealed class Pg002_PaymentAndAppendOnly : Migration
{
    protected override void Up(MigrationBuilder migrationBuilder)
    {
        migrationBuilder.Sql(Read("AppendOnly.sql"));
        migrationBuilder.Sql(Read("PaymentResult.sql"));
        migrationBuilder.Sql(Read("Expiration.sql"));
    }

    protected override void Down(MigrationBuilder migrationBuilder)
    {
        migrationBuilder.Sql("""
            DO $guard$ BEGIN
              IF EXISTS(SELECT 1 FROM public."Payment") OR EXISTS(SELECT 1 FROM public."AuditLog")
                 OR EXISTS(SELECT 1 FROM public."LoginHistory") OR EXISTS(SELECT 1 FROM public."ContractExtension") THEN
                RAISE EXCEPTION 'Cannot remove financial/history protections while evidence exists';
              END IF;
            END $guard$;
            DROP FUNCTION public."usp_ExpirePendingReservations"(timestamptz);
            DROP FUNCTION public."usp_ApplyPaymentResult"(uuid,varchar,varchar,numeric,timestamptz,varchar);
            DROP TRIGGER "trg_ContractExtension_AppendOnly" ON public."ContractExtension";
            DROP TRIGGER "trg_LoginHistory_AppendOnly" ON public."LoginHistory";
            DROP TRIGGER "trg_AuditLog_AppendOnly" ON public."AuditLog";
            DROP FUNCTION public.frms_reject_history_mutation();
            """);
    }

    private static string Read(string name)
    {
        using var stream = typeof(Pg002_PaymentAndAppendOnly).Assembly
            .GetManifestResourceStream("Frms.DataAccess.PostgreSql." + name)
            ?? throw new InvalidOperationException("Missing PostgreSQL migration resource.");
        using var reader = new StreamReader(stream);
        return reader.ReadToEnd();
    }
}
