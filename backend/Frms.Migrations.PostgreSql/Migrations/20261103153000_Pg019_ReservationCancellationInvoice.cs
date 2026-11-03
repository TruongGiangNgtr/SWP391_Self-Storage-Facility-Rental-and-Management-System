using Frms.Migrations.PostgreSql.Persistence;
using Microsoft.EntityFrameworkCore.Infrastructure;
using Microsoft.EntityFrameworkCore.Migrations;

namespace Frms.Migrations.PostgreSql.Migrations;

/// <summary>
/// RES-005: Cancel an UNPAID deposit Invoice atomically with Reservation cancellation.
/// Preserve PAID history. No historical backfill and no schema changes.
/// Must be applied after Pg018; never re-run the full Reservations.sql resource,
/// because Pg017 overrides usp_CreateReservation with the CCCD prerequisite.
/// </summary>
[DbContext(typeof(FrmsDbContext))]
[Migration("20261103153000_Pg019_ReservationCancellationInvoice")]
public sealed class Pg019_ReservationCancellationInvoice : Migration
{
    protected override void Up(MigrationBuilder migrationBuilder)
    {
        migrationBuilder.Sql("""
            CREATE OR REPLACE FUNCTION public."usp_CancelReservation"(p_user_account_id uuid,p_customer_id uuid,p_reservation_id uuid)
            RETURNS void LANGUAGE plpgsql AS $frms$
            DECLARE v_res public."Reservation"%ROWTYPE; v_status varchar; v_role varchar;
            BEGIN
             PERFORM pg_advisory_xact_lock(70439102);
             SELECT * INTO v_res FROM public."Reservation" r WHERE r."ReservationId"=p_reservation_id FOR UPDATE;
             IF NOT FOUND OR v_res."CustomerId" IS DISTINCT FROM p_customer_id THEN RAISE EXCEPTION USING ERRCODE='P1201',MESSAGE='RESOURCE_NOT_FOUND'; END IF;
             SELECT a."Status",r."RoleName" INTO v_status,v_role FROM public."Customer" c JOIN public."UserAccount" a ON a."UserAccountId"=c."UserAccountId" JOIN public."UserRole" r ON r."RoleId"=a."RoleId" WHERE c."CustomerId"=p_customer_id AND a."UserAccountId"=p_user_account_id FOR SHARE OF c,a,r;
             IF NOT FOUND THEN RAISE EXCEPTION USING ERRCODE='P1201',MESSAGE='RESOURCE_NOT_FOUND'; END IF;
             IF v_status<>'ACTIVE' THEN RAISE EXCEPTION USING ERRCODE='P1202',MESSAGE='ACCOUNT_INACTIVE'; END IF;
             IF v_role<>'CUSTOMER' THEN RAISE EXCEPTION USING ERRCODE='P1203',MESSAGE='FORBIDDEN'; END IF;
             IF v_res."Status" NOT IN ('PENDING_DEPOSIT','CONFIRMED','CANCELLED') THEN RAISE EXCEPTION USING ERRCODE='P1204',MESSAGE='RESERVATION_INVALID_STATUS'; END IF;
             IF EXISTS(SELECT 1 FROM public."Visit" v WHERE v."EntityId"=p_reservation_id AND v."VisitType"='RESERVATION' AND v."Status" IN ('CHECKED_IN','CHECKED_OUT')) THEN RAISE EXCEPTION USING ERRCODE='P1205',MESSAGE='VISIT_INVALID_STATUS'; END IF;
             IF EXISTS(SELECT 1 FROM public."Contract" c WHERE c."ReservationId"=p_reservation_id) THEN RAISE EXCEPTION USING ERRCODE='P1204',MESSAGE='RESERVATION_INVALID_STATUS'; END IF;
             UPDATE public."Visit" AS target SET "Status"='CANCELLED' WHERE target."EntityId"=p_reservation_id AND target."VisitType"='RESERVATION' AND target."Status"='SCHEDULED';
             UPDATE public."Reservation" AS target SET "Status"='CANCELLED' WHERE target."ReservationId"=p_reservation_id AND target."Status" IN ('PENDING_DEPOSIT','CONFIRMED');
             -- Close only an unpaid DEPOSIT Invoice in the SAME transaction as cancellation.
             -- PAID/previously CANCELLED invoices and their financial snapshots remain untouched.
             -- Concurrent payOS result application serializes on the Invoice row lock.
             UPDATE public."Invoice" AS target SET "Status"='CANCELLED'
             WHERE target."EntityId"=p_reservation_id AND target."InvoiceType"='DEPOSIT'
               AND target."Status"='UNPAID';
            END $frms$;
            """);
    }

    protected override void Down(MigrationBuilder migrationBuilder) =>
        throw new NotSupportedException(
            "Financial status and cancellation history must not be silently downgraded. " +
            "Use a reviewed forward migration if behavior must change.");
}
