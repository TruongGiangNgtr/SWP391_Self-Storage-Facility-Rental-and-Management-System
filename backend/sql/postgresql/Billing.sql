-- SRS BR-BIL-02..06/CALC-LATE-01..02 and due-day clarification §2949:
-- calendar thresholds are GMT+7, persisted instants are UTC. No first month invoice.
CREATE OR REPLACE FUNCTION public.frms_business_day_start(p_month date,p_day int)
RETURNS timestamptz LANGUAGE sql IMMUTABLE STRICT AS $frms$
 SELECT (p_month+LEAST(p_day,extract(day FROM p_month+interval '1 month - 1 day')::int)-1)::timestamp AT TIME ZONE 'Asia/Ho_Chi_Minh';
$frms$;

CREATE OR REPLACE FUNCTION public."usp_CreateMonthlyInvoice"(p_contract_id uuid,p_billing_month date,p_now_utc timestamptz DEFAULT NULL)
RETURNS TABLE("InvoiceId" uuid,"Result" varchar,"BaseAmount" numeric,"DiscountAmount" numeric,"AmountDue" numeric,"DueDate" timestamptz) LANGUAGE plpgsql AS $frms$
DECLARE v_existing public."Invoice"%ROWTYPE; v_contract public."Contract"%ROWTYPE; v_res public."Reservation"%ROWTYPE; v_base numeric(18,2); v_pct numeric(5,2):=0; v_discount_id uuid; v_discount numeric(18,2); v_due_amount numeric(18,2); v_due_day int; v_due timestamptz(3); v_id uuid:=gen_random_uuid();
BEGIN
 PERFORM pg_advisory_xact_lock(70439102);
 IF p_billing_month IS NULL OR extract(day FROM p_billing_month)<>1 THEN RAISE EXCEPTION USING ERRCODE='P1137',MESSAGE='BILLING_MONTH_INVALID'; END IF;
 SELECT * INTO v_existing FROM public."Invoice" i WHERE i."EntityId"=p_contract_id AND i."InvoiceType"='RENTAL_FEE' AND i."BillingMonth"=p_billing_month;
 IF FOUND THEN RETURN QUERY SELECT v_existing."InvoiceId",'EXISTING'::varchar,v_existing."BaseAmount",v_existing."DiscountAmount",v_existing."AmountDue",v_existing."DueDate"; RETURN; END IF;
 SELECT * INTO v_contract FROM public."Contract" c WHERE c."ContractId"=p_contract_id FOR UPDATE;
 IF NOT FOUND OR v_contract."Status"<>'ACTIVE' THEN RAISE EXCEPTION USING ERRCODE='P1120',MESSAGE='CONTRACT_NOT_ACTIVE'; END IF;
 IF p_billing_month<v_contract."StartMonth" OR p_billing_month>v_contract."EndMonth" THEN RAISE EXCEPTION USING ERRCODE='P1137',MESSAGE='BILLING_MONTH_INVALID'; END IF;
 IF p_billing_month=v_contract."StartMonth" THEN RAISE EXCEPTION USING ERRCODE='P1208',MESSAGE='FIRST_MONTH_INVOICE_HANDOVER_ONLY'; END IF;
 SELECT * INTO v_res FROM public."Reservation" r WHERE r."ReservationId"=v_contract."ReservationId";
 v_base:=v_res."LockedRentalPrice";
 IF p_billing_month>v_res."EndMonth" THEN
  SELECT e."AppliedMonthlyPrice" INTO v_base FROM public."ContractExtension" e WHERE e."ContractId"=p_contract_id AND (e."OldEndMonth"+interval '1 month')::date<=p_billing_month AND e."NewEndMonth">=p_billing_month ORDER BY e."NewEndMonth" LIMIT 1;
  IF v_base IS NULL THEN RAISE EXCEPTION USING ERRCODE='P1138',MESSAGE='RENEWAL_PRICE_SNAPSHOT_NOT_FOUND'; END IF;
 END IF;
 IF v_contract."DiscountId" IS NOT NULL THEN
  -- Captured Discount remains applicable after inactivation.
  SELECT d."Percentage",d."DiscountId" INTO v_pct,v_discount_id FROM public."Discount" d WHERE d."DiscountId"=v_contract."DiscountId";
  IF NOT FOUND THEN RAISE EXCEPTION USING ERRCODE='P1118',MESSAGE='DISCOUNT_NOT_VALID'; END IF;
 END IF;
 v_discount:=round(v_base*coalesce(v_pct,0)/100,2); v_due_amount:=greatest(v_base-v_discount,0);
 SELECT p."MonthlyPaymentDueDay" INTO v_due_day FROM public."Policy" p WHERE p."PolicyId"=v_contract."PolicyId";
 v_due:=public.frms_business_day_start(p_billing_month,v_due_day)+interval '1 day'-interval '1 millisecond';
 INSERT INTO public."Invoice"("InvoiceId","EntityId","InvoiceType","BillingMonth","BaseAmount","DiscountId","DiscountAmount","AmountDue","DueDate","Status","CreatedAt") VALUES(v_id,p_contract_id,'RENTAL_FEE',p_billing_month,v_base,v_discount_id,v_discount,v_due_amount,v_due,'UNPAID',coalesce(p_now_utc,clock_timestamp()));
 RETURN QUERY SELECT v_id,NULL::varchar,v_base,v_discount,v_due_amount,v_due;
END $frms$;

CREATE OR REPLACE FUNCTION public."usp_CalculateLateFee"(p_invoice_id uuid,p_as_of_utc timestamptz DEFAULT NULL)
RETURNS TABLE("InvoiceId" uuid,"OverdueDays" int,"LateFeeAmount" numeric) LANGUAGE plpgsql AS $frms$
DECLARE v_invoice public."Invoice"%ROWTYPE; v_contract public."Contract"%ROWTYPE; v_policy public."Policy"%ROWTYPE; v_now timestamptz(3):=coalesce(p_as_of_utc,clock_timestamp()); v_cutoff timestamptz; v_paid timestamptz; v_days int; v_amount numeric(18,2);
BEGIN
 PERFORM pg_advisory_xact_lock(70439102);
 SELECT * INTO v_invoice FROM public."Invoice" i WHERE i."InvoiceId"=p_invoice_id FOR UPDATE;
 IF NOT FOUND OR v_invoice."InvoiceType"<>'RENTAL_FEE' OR v_invoice."Status" NOT IN ('OVERDUE','PAID') THEN RAISE EXCEPTION USING ERRCODE='P1145',MESSAGE='LATE_FEE_NOT_APPLICABLE'; END IF;
 SELECT * INTO v_contract FROM public."Contract" c WHERE c."ContractId"=v_invoice."EntityId";
 IF v_invoice."BillingMonth"<=v_contract."StartMonth" THEN RAISE EXCEPTION USING ERRCODE='P1145',MESSAGE='LATE_FEE_NOT_APPLICABLE'; END IF;
 SELECT * INTO v_policy FROM public."Policy" p WHERE p."PolicyId"=v_contract."PolicyId";
 v_cutoff:=v_now;
 IF v_invoice."Status"='PAID' THEN
  SELECT p."PaidAt" INTO v_paid FROM public."Payment" p WHERE p."InvoiceId"=p_invoice_id AND p."Status"='SUCCESS' ORDER BY p."PaidAt" DESC LIMIT 1;
  IF FOUND THEN v_cutoff:=v_paid; END IF;
 END IF;
 v_days:=(v_cutoff AT TIME ZONE 'Asia/Ho_Chi_Minh')::date-(public.frms_business_day_start(v_invoice."BillingMonth",v_policy."OverdueStartDay") AT TIME ZONE 'Asia/Ho_Chi_Minh')::date+1;
 IF v_days<1 THEN RAISE EXCEPTION USING ERRCODE='P1145',MESSAGE='LATE_FEE_NOT_APPLICABLE'; END IF;
 v_amount:=round(v_invoice."AmountDue"/v_policy."LateFeeDivisorDays"*v_days,2);
 IF EXISTS(SELECT 1 FROM public."LateFee" l WHERE l."InvoiceId"=p_invoice_id) THEN
  UPDATE public."LateFee" AS l SET "OverdueDays"=v_days,"Amount"=v_amount,"CalculatedAt"=v_now WHERE l."InvoiceId"=p_invoice_id;
 ELSE
  INSERT INTO public."LateFee"("LateFeeId","InvoiceId","OverdueDays","Amount","CalculatedAt") VALUES(gen_random_uuid(),p_invoice_id,v_days,v_amount,v_now);
 END IF;
 RETURN QUERY SELECT p_invoice_id,v_days,v_amount;
END $frms$;

CREATE OR REPLACE FUNCTION public."usp_MarkOverdueInvoices"(p_now_utc timestamptz DEFAULT NULL)
RETURNS TABLE("MarkedOverdueCount" int) LANGUAGE plpgsql AS $frms$
DECLARE v_count int;
BEGIN
 PERFORM pg_advisory_xact_lock(70439102);
 UPDATE public."Invoice" AS i SET "Status"='OVERDUE' FROM public."Contract" c JOIN public."Policy" p ON p."PolicyId"=c."PolicyId" WHERE c."ContractId"=i."EntityId" AND i."InvoiceType"='RENTAL_FEE' AND i."Status"='UNPAID' AND i."BillingMonth">c."StartMonth" AND coalesce(p_now_utc,clock_timestamp())>=public.frms_business_day_start(i."BillingMonth",p."OverdueStartDay");
 GET DIAGNOSTICS v_count=ROW_COUNT;
 RETURN QUERY SELECT v_count;
END $frms$;
