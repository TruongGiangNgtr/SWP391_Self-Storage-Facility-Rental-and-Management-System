using Frms.Migrations.PostgreSql.Persistence;
using Microsoft.EntityFrameworkCore.Infrastructure;
using Microsoft.EntityFrameworkCore.Migrations;

namespace Frms.Migrations.PostgreSql.Migrations;

/// <summary>
/// OD-09/10: Enforce an exact 12 ASCII digit saved CCCD inside atomic RES-001.
/// Does not change Customer schema or existing Reservation/Invoice history.
/// Deploy after Pg016; Pg014 email event and trigger objects are unrelated.
/// </summary>
[DbContext(typeof(FrmsDbContext))]
[Migration("20261027180000_Pg017_CccdBeforeReservation")]
public sealed class Pg017_CccdBeforeReservation : Migration
{
    protected override void Up(MigrationBuilder migrationBuilder)
    {
        migrationBuilder.Sql("""
            CREATE OR REPLACE FUNCTION public."usp_CreateReservation"(p_customer_id uuid,p_facility_id uuid,p_unit_type_id uuid,p_start_month date,p_end_month date,p_now_utc timestamptz DEFAULT NULL)
            RETURNS TABLE("ReservationId" uuid,"DepositInvoiceId" uuid,"DepositAmount" numeric,"DepositDueAt" timestamptz)
            LANGUAGE plpgsql AS $frms$
            DECLARE v_now timestamptz(3):=coalesce(p_now_utc,clock_timestamp()); v_month date; v_policy public."Policy"%ROWTYPE; v_price numeric; v_reservation uuid:=gen_random_uuid(); v_invoice uuid:=gen_random_uuid(); v_capacity bigint; v_holds bigint; v_occupied bigint; v_cccd varchar(20);
            BEGIN
             PERFORM pg_advisory_xact_lock(70439102);
             IF p_start_month IS NULL OR p_end_month IS NULL OR extract(day FROM p_start_month)<>1 OR extract(day FROM p_end_month)<>1 OR p_end_month<p_start_month
              OR p_start_month<=date_trunc('month',v_now AT TIME ZONE 'Asia/Ho_Chi_Minh')::date THEN RAISE EXCEPTION USING ERRCODE='P1101',MESSAGE='INVALID_MONTH_RANGE'; END IF;
             -- Lock account and Customer profile at the authoritative creation boundary.
             -- FOR SHARE conflicts with concurrent CCCD UPDATE; recheck after waiting for it.
             SELECT c."CCCD" INTO v_cccd FROM public."UserAccount" a JOIN public."Customer" c ON c."UserAccountId"=a."UserAccountId"
             WHERE c."CustomerId"=p_customer_id AND a."Status"='ACTIVE' FOR SHARE OF a,c;
             IF NOT FOUND THEN RAISE EXCEPTION USING ERRCODE='P1102',MESSAGE='ACCOUNT_INACTIVE_OR_CUSTOMER_NOT_FOUND'; END IF;
             IF v_cccd IS NULL OR v_cccd !~ '^[0-9]{12}$' THEN
              RAISE EXCEPTION USING ERRCODE='P1111',MESSAGE='CUSTOMER_CCCD_INVALID';
             END IF;
             PERFORM 1 FROM public."Facility" f WHERE f."FacilityId"=p_facility_id AND f."Status"='ACTIVE' FOR UPDATE;
             IF NOT FOUND THEN RAISE EXCEPTION USING ERRCODE='P1103',MESSAGE='FACILITY_INACTIVE'; END IF;
             SELECT u."RentalPrice" INTO v_price FROM public."UnitType" u WHERE u."UnitTypeId"=p_unit_type_id FOR UPDATE;
             IF NOT FOUND THEN RAISE EXCEPTION USING ERRCODE='P1104',MESSAGE='UNIT_TYPE_NOT_FOUND'; END IF;
             SELECT * INTO v_policy FROM public."Policy" p WHERE p."Status"='ACTIVE' ORDER BY p."Version" DESC LIMIT 1 FOR SHARE;
             IF NOT FOUND THEN RAISE EXCEPTION USING ERRCODE='P1105',MESSAGE='ACTIVE_POLICY_NOT_FOUND'; END IF;
             v_month:=p_start_month;
             WHILE v_month<=p_end_month LOOP
              SELECT count(*) INTO v_capacity FROM public."StorageUnit" s WHERE s."FacilityId"=p_facility_id AND s."UnitTypeId"=p_unit_type_id AND s."Status" IN ('AVAILABLE','IN_USE','INSPECTION');
              SELECT count(*) INTO v_holds FROM public."Reservation" r WHERE r."FacilityId"=p_facility_id AND r."UnitTypeId"=p_unit_type_id AND r."Status" IN ('PENDING_DEPOSIT','CONFIRMED') AND r."StartMonth"<=v_month AND r."EndMonth">=v_month;
              SELECT count(*) INTO v_occupied FROM public."Contract" c JOIN public."StorageUnit" s ON s."StorageUnitId"=c."StorageUnitId" WHERE c."FacilityId"=p_facility_id AND s."UnitTypeId"=p_unit_type_id AND c."Status"='ACTIVE' AND c."StartMonth"<=v_month AND c."EndMonth">=v_month;
              IF v_capacity<=v_holds+v_occupied THEN RAISE EXCEPTION USING ERRCODE='P1106',MESSAGE='CAPACITY_NOT_AVAILABLE'; END IF;
              v_month:=(v_month+interval '1 month')::date;
             END LOOP;
             INSERT INTO public."Reservation"("ReservationId","CustomerId","FacilityId","UnitTypeId","PolicyId","StartMonth","EndMonth","LockedRentalPrice","DepositAmount","Status","CreatedAt") VALUES(v_reservation,p_customer_id,p_facility_id,p_unit_type_id,v_policy."PolicyId",p_start_month,p_end_month,v_price,v_price,'PENDING_DEPOSIT',v_now);
             INSERT INTO public."Invoice"("InvoiceId","EntityId","InvoiceType","BillingMonth","BaseAmount","DiscountId","DiscountAmount","AmountDue","DueDate","Status","CreatedAt") VALUES(v_invoice,v_reservation,'DEPOSIT',NULL,v_price,NULL,0,v_price,v_now+v_policy."DepositTimeoutHours"*interval '1 hour','UNPAID',v_now);
             RETURN QUERY SELECT v_reservation,v_invoice,v_price,v_now+v_policy."DepositTimeoutHours"*interval '1 hour';
            END $frms$;
        """);
    }

    protected override void Down(MigrationBuilder migrationBuilder)
    {
        // Revert only this guard; no schema/data is modified.
        migrationBuilder.Sql("""
            CREATE OR REPLACE FUNCTION public."usp_CreateReservation"(p_customer_id uuid,p_facility_id uuid,p_unit_type_id uuid,p_start_month date,p_end_month date,p_now_utc timestamptz DEFAULT NULL)
            RETURNS TABLE("ReservationId" uuid,"DepositInvoiceId" uuid,"DepositAmount" numeric,"DepositDueAt" timestamptz)
            LANGUAGE plpgsql AS $frms$
            DECLARE v_now timestamptz(3):=coalesce(p_now_utc,clock_timestamp()); v_month date; v_policy public."Policy"%ROWTYPE; v_price numeric; v_reservation uuid:=gen_random_uuid(); v_invoice uuid:=gen_random_uuid(); v_capacity bigint; v_holds bigint; v_occupied bigint;
            BEGIN
             PERFORM pg_advisory_xact_lock(70439102);
             IF p_start_month IS NULL OR p_end_month IS NULL OR extract(day FROM p_start_month)<>1 OR extract(day FROM p_end_month)<>1 OR p_end_month<p_start_month
              OR p_start_month<=date_trunc('month',v_now AT TIME ZONE 'Asia/Ho_Chi_Minh')::date THEN RAISE EXCEPTION USING ERRCODE='P1101',MESSAGE='INVALID_MONTH_RANGE'; END IF;
             PERFORM 1 FROM public."UserAccount" a JOIN public."Customer" c ON c."UserAccountId"=a."UserAccountId" WHERE c."CustomerId"=p_customer_id AND a."Status"='ACTIVE' FOR SHARE OF a,c;
             IF NOT FOUND THEN RAISE EXCEPTION USING ERRCODE='P1102',MESSAGE='ACCOUNT_INACTIVE_OR_CUSTOMER_NOT_FOUND'; END IF;
             PERFORM 1 FROM public."Facility" f WHERE f."FacilityId"=p_facility_id AND f."Status"='ACTIVE' FOR UPDATE;
             IF NOT FOUND THEN RAISE EXCEPTION USING ERRCODE='P1103',MESSAGE='FACILITY_INACTIVE'; END IF;
             SELECT u."RentalPrice" INTO v_price FROM public."UnitType" u WHERE u."UnitTypeId"=p_unit_type_id FOR UPDATE;
             IF NOT FOUND THEN RAISE EXCEPTION USING ERRCODE='P1104',MESSAGE='UNIT_TYPE_NOT_FOUND'; END IF;
             SELECT * INTO v_policy FROM public."Policy" p WHERE p."Status"='ACTIVE' ORDER BY p."Version" DESC LIMIT 1 FOR SHARE;
             IF NOT FOUND THEN RAISE EXCEPTION USING ERRCODE='P1105',MESSAGE='ACTIVE_POLICY_NOT_FOUND'; END IF;
             v_month:=p_start_month;
             WHILE v_month<=p_end_month LOOP
              SELECT count(*) INTO v_capacity FROM public."StorageUnit" s WHERE s."FacilityId"=p_facility_id AND s."UnitTypeId"=p_unit_type_id AND s."Status" IN ('AVAILABLE','IN_USE','INSPECTION');
              SELECT count(*) INTO v_holds FROM public."Reservation" r WHERE r."FacilityId"=p_facility_id AND r."UnitTypeId"=p_unit_type_id AND r."Status" IN ('PENDING_DEPOSIT','CONFIRMED') AND r."StartMonth"<=v_month AND r."EndMonth">=v_month;
              SELECT count(*) INTO v_occupied FROM public."Contract" c JOIN public."StorageUnit" s ON s."StorageUnitId"=c."StorageUnitId" WHERE c."FacilityId"=p_facility_id AND s."UnitTypeId"=p_unit_type_id AND c."Status"='ACTIVE' AND c."StartMonth"<=v_month AND c."EndMonth">=v_month;
              IF v_capacity<=v_holds+v_occupied THEN RAISE EXCEPTION USING ERRCODE='P1106',MESSAGE='CAPACITY_NOT_AVAILABLE'; END IF;
              v_month:=(v_month+interval '1 month')::date;
             END LOOP;
             INSERT INTO public."Reservation"("ReservationId","CustomerId","FacilityId","UnitTypeId","PolicyId","StartMonth","EndMonth","LockedRentalPrice","DepositAmount","Status","CreatedAt") VALUES(v_reservation,p_customer_id,p_facility_id,p_unit_type_id,v_policy."PolicyId",p_start_month,p_end_month,v_price,v_price,'PENDING_DEPOSIT',v_now);
             INSERT INTO public."Invoice"("InvoiceId","EntityId","InvoiceType","BillingMonth","BaseAmount","DiscountId","DiscountAmount","AmountDue","DueDate","Status","CreatedAt") VALUES(v_invoice,v_reservation,'DEPOSIT',NULL,v_price,NULL,0,v_price,v_now+v_policy."DepositTimeoutHours"*interval '1 hour','UNPAID',v_now);
             RETURN QUERY SELECT v_reservation,v_invoice,v_price,v_now+v_policy."DepositTimeoutHours"*interval '1 hour';
            END $frms$;
        """);
    }
}
