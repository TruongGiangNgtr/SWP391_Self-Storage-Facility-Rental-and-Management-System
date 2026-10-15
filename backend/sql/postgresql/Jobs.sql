-- Only the existing expiration Api job remains registered. These wrappers
-- preserve existing SQL jobs; their external deployment is a separate gate.
CREATE OR REPLACE FUNCTION public."usp_ProcessReservationNoShow"(p_now_utc timestamptz DEFAULT NULL)
RETURNS TABLE("ProcessedNoShowCount" int) LANGUAGE plpgsql AS $frms$
DECLARE v_row record; v_count int:=0; v_now timestamptz:=coalesce(p_now_utc,clock_timestamp());
BEGIN
 PERFORM pg_advisory_xact_lock(70439102);
 FOR v_row IN SELECT r."ReservationId",v."VisitId",v."Status" FROM public."Reservation" r JOIN public."Policy" p ON p."PolicyId"=r."PolicyId" JOIN public."Visit" v ON v."EntityId"=r."ReservationId" AND v."VisitType"='RESERVATION' WHERE r."Status"='CONFIRMED' AND v."Status" IN ('SCHEDULED','CHECKED_IN') AND (v_now AT TIME ZONE 'Asia/Ho_Chi_Minh')::date>(public.frms_business_day_start(r."StartMonth",p."ReservationVisitEndDay") AT TIME ZONE 'Asia/Ho_Chi_Minh')::date ORDER BY r."ReservationId" FOR UPDATE OF r,v LOOP
  UPDATE public."Visit" AS v SET "Status"=CASE WHEN v_row."Status"='SCHEDULED' THEN 'CANCELLED' ELSE 'CHECKED_OUT' END WHERE v."VisitId"=v_row."VisitId";
  UPDATE public."Reservation" AS r SET "Status"='CANCELLED' WHERE r."ReservationId"=v_row."ReservationId";
  v_count:=v_count+1;
 END LOOP;
 RETURN QUERY SELECT v_count;
END $frms$;

CREATE OR REPLACE FUNCTION public."usp_GenerateMonthlyInvoices"(p_billing_month date DEFAULT NULL,p_now_utc timestamptz DEFAULT NULL)
RETURNS void LANGUAGE plpgsql AS $frms$
DECLARE v_month date:=coalesce(p_billing_month,date_trunc('month',coalesce(p_now_utc,clock_timestamp()) AT TIME ZONE 'Asia/Ho_Chi_Minh')::date); v_id uuid;
BEGIN
 PERFORM pg_advisory_xact_lock(70439102);
 FOR v_id IN SELECT c."ContractId" FROM public."Contract" c WHERE c."Status"='ACTIVE' AND c."StartMonth"<v_month AND c."EndMonth">=v_month ORDER BY c."ContractId" LOOP
  IF NOT EXISTS(SELECT 1 FROM public."Invoice" i WHERE i."EntityId"=v_id AND i."InvoiceType"='RENTAL_FEE' AND i."BillingMonth"=v_month) THEN PERFORM public."usp_CreateMonthlyInvoice"(v_id,v_month,p_now_utc); END IF;
 END LOOP;
END $frms$;

CREATE OR REPLACE FUNCTION public."usp_RecalculateOpenLateFees"(p_now_utc timestamptz DEFAULT NULL)
RETURNS void LANGUAGE plpgsql AS $frms$
DECLARE v_id uuid;
BEGIN
 PERFORM pg_advisory_xact_lock(70439102);
 FOR v_id IN SELECT i."InvoiceId" FROM public."Invoice" i JOIN public."Contract" c ON c."ContractId"=i."EntityId" WHERE i."InvoiceType"='RENTAL_FEE' AND i."Status"='OVERDUE' AND i."BillingMonth">c."StartMonth" ORDER BY i."InvoiceId" LOOP
  PERFORM public."usp_CalculateLateFee"(v_id,p_now_utc);
 END LOOP;
END $frms$;
