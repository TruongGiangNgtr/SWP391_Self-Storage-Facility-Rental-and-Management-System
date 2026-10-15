-- Existing access/return/cancellation/reschedule routines. Calendar dates use GMT+7.
CREATE OR REPLACE FUNCTION public."usp_CancelVisit"(p_visit_id uuid)
RETURNS TABLE("VisitId" uuid,"Status" varchar) LANGUAGE plpgsql AS $frms$
DECLARE v_visit public."Visit"%ROWTYPE;
BEGIN
 PERFORM pg_advisory_xact_lock(70439102);
 SELECT * INTO v_visit FROM public."Visit" v WHERE v."VisitId"=p_visit_id FOR UPDATE;
 IF NOT FOUND OR v_visit."Status"<>'SCHEDULED' THEN RAISE EXCEPTION USING ERRCODE='P1112',MESSAGE='VISIT_INVALID_STATUS'; END IF;
 IF v_visit."VisitType"='RESERVATION' THEN RAISE EXCEPTION USING ERRCODE='P1115',MESSAGE='Cancel the Reservation instead of cancelling its RESERVATION Visit directly.'; END IF;
 UPDATE public."Visit" AS target SET "Status"='CANCELLED' WHERE target."VisitId"=p_visit_id;
 RETURN QUERY SELECT p_visit_id,'CANCELLED'::varchar;
END $frms$;

CREATE OR REPLACE FUNCTION public."usp_CreateAccessVisit"(p_contract_id uuid,p_customer_id uuid,p_visit_date date)
RETURNS TABLE("VisitId" uuid,"Status" varchar) LANGUAGE plpgsql AS $frms$
DECLARE v_contract public."Contract"%ROWTYPE; v_visit uuid:=gen_random_uuid();
BEGIN
 PERFORM pg_advisory_xact_lock(70439102);
 SELECT * INTO v_contract FROM public."Contract" c WHERE c."ContractId"=p_contract_id FOR UPDATE;
 IF NOT FOUND OR v_contract."Status"<>'ACTIVE' OR v_contract."CustomerId" IS DISTINCT FROM p_customer_id THEN RAISE EXCEPTION USING ERRCODE='P1120',MESSAGE='CONTRACT_NOT_ACTIVE_OR_NOT_OWNED'; END IF;
 IF p_visit_date IS NULL OR p_visit_date<v_contract."StartMonth" OR p_visit_date>(v_contract."EndMonth"+interval '1 month - 1 day')::date THEN RAISE EXCEPTION USING ERRCODE='P1121',MESSAGE='ACCESS_VISIT_DATE_OUTSIDE_CONTRACT'; END IF;
 IF EXISTS(SELECT 1 FROM public."Visit" v WHERE v."EntityId"=p_contract_id AND v."VisitType"='RETURN' AND v."Status" IN ('SCHEDULED','CHECKED_IN')) THEN RAISE EXCEPTION USING ERRCODE='P1122',MESSAGE='RETURN_VISIT_PENDING'; END IF;
 IF p_visit_date<(clock_timestamp() AT TIME ZONE 'Asia/Ho_Chi_Minh')::date THEN RAISE EXCEPTION USING ERRCODE='P1123',MESSAGE='VISIT_DATE_IN_PAST'; END IF;
 INSERT INTO public."Visit"("VisitId","EntityId","VisitType","VisitDate","Status") VALUES(v_visit,p_contract_id,'ACCESS',p_visit_date,'SCHEDULED');
 RETURN QUERY SELECT v_visit,'SCHEDULED'::varchar;
END $frms$;

CREATE OR REPLACE FUNCTION public."usp_CreateReturnVisit"(p_contract_id uuid,p_customer_id uuid,p_visit_date date,p_user_account_id uuid DEFAULT NULL,p_now_utc timestamptz DEFAULT NULL)
RETURNS SETOF public."Visit" LANGUAGE plpgsql AS $frms$
DECLARE v_contract public."Contract"%ROWTYPE; v_status varchar; v_role varchar; v_visit uuid:=gen_random_uuid(); v_now timestamptz:=coalesce(p_now_utc,clock_timestamp());
BEGIN
 PERFORM pg_advisory_xact_lock(70439102);
 SELECT * INTO v_contract FROM public."Contract" c WHERE c."ContractId"=p_contract_id FOR UPDATE;
 IF NOT FOUND OR v_contract."CustomerId" IS DISTINCT FROM p_customer_id OR p_customer_id IS NULL THEN RAISE EXCEPTION USING ERRCODE='P1301',MESSAGE='RESOURCE_NOT_FOUND'; END IF;
 SELECT a."Status",r."RoleName" INTO v_status,v_role FROM public."Customer" c JOIN public."UserAccount" a ON a."UserAccountId"=c."UserAccountId" JOIN public."UserRole" r ON r."RoleId"=a."RoleId" WHERE c."CustomerId"=p_customer_id AND (p_user_account_id IS NULL OR a."UserAccountId"=p_user_account_id) FOR SHARE OF c,a,r;
 IF NOT FOUND THEN RAISE EXCEPTION USING ERRCODE='P1301',MESSAGE='RESOURCE_NOT_FOUND'; END IF;
 IF v_status<>'ACTIVE' THEN RAISE EXCEPTION USING ERRCODE='P1302',MESSAGE='ACCOUNT_INACTIVE'; END IF;
 IF v_role<>'CUSTOMER' THEN RAISE EXCEPTION USING ERRCODE='P1303',MESSAGE='FORBIDDEN'; END IF;
 IF v_contract."Status"<>'ACTIVE' THEN RAISE EXCEPTION USING ERRCODE='P1304',MESSAGE='CONTRACT_NOT_ACTIVE'; END IF;
 IF p_visit_date IS NULL THEN RAISE EXCEPTION USING ERRCODE='P1308',MESSAGE='VALIDATION_ERROR'; END IF;
 IF p_visit_date<(v_now AT TIME ZONE 'Asia/Ho_Chi_Minh')::date THEN RAISE EXCEPTION USING ERRCODE='P1306',MESSAGE='VISIT_DATE_IN_PAST'; END IF;
 IF EXISTS(SELECT 1 FROM public."Visit" v WHERE v."EntityId"=p_contract_id AND v."VisitType"='RETURN' AND v."Status" IN ('SCHEDULED','CHECKED_IN')) THEN RAISE EXCEPTION USING ERRCODE='P1305',MESSAGE='RETURN_VISIT_PENDING'; END IF;
 INSERT INTO public."Visit"("VisitId","EntityId","EmployeeId","VisitType","VisitDate","ActualReturnDate","Status") VALUES(v_visit,p_contract_id,NULL,'RETURN',p_visit_date,NULL,'SCHEDULED');
 RETURN QUERY SELECT v.* FROM public."Visit" v WHERE v."VisitId"=v_visit;
END $frms$;

CREATE OR REPLACE FUNCTION public."usp_RescheduleReturnVisit"(p_visit_id uuid,p_customer_id uuid,p_visit_date date,p_user_account_id uuid,p_now_utc timestamptz DEFAULT NULL)
RETURNS SETOF public."Visit" LANGUAGE plpgsql AS $frms$
DECLARE v_contract_id uuid; v_contract public."Contract"%ROWTYPE; v_visit public."Visit"%ROWTYPE; v_status varchar; v_role varchar; v_now timestamptz:=coalesce(p_now_utc,clock_timestamp());
BEGIN
 PERFORM pg_advisory_xact_lock(70439102);
 SELECT v."EntityId" INTO v_contract_id FROM public."Visit" v WHERE v."VisitId"=p_visit_id AND v."VisitType"='RETURN';
 SELECT * INTO v_contract FROM public."Contract" c WHERE c."ContractId"=v_contract_id FOR UPDATE;
 IF NOT FOUND OR v_contract."CustomerId" IS DISTINCT FROM p_customer_id OR p_customer_id IS NULL THEN RAISE EXCEPTION USING ERRCODE='P1301',MESSAGE='RESOURCE_NOT_FOUND'; END IF;
 SELECT * INTO v_visit FROM public."Visit" v WHERE v."VisitId"=p_visit_id AND v."EntityId"=v_contract_id AND v."VisitType"='RETURN' FOR UPDATE;
 IF NOT FOUND THEN RAISE EXCEPTION USING ERRCODE='P1301',MESSAGE='RESOURCE_NOT_FOUND'; END IF;
 SELECT a."Status",r."RoleName" INTO v_status,v_role FROM public."Customer" c JOIN public."UserAccount" a ON a."UserAccountId"=c."UserAccountId" JOIN public."UserRole" r ON r."RoleId"=a."RoleId" WHERE c."CustomerId"=p_customer_id AND a."UserAccountId"=p_user_account_id FOR SHARE OF c,a,r;
 IF NOT FOUND THEN RAISE EXCEPTION USING ERRCODE='P1301',MESSAGE='RESOURCE_NOT_FOUND'; END IF;
 IF v_status<>'ACTIVE' THEN RAISE EXCEPTION USING ERRCODE='P1302',MESSAGE='ACCOUNT_INACTIVE'; END IF;
 IF v_role<>'CUSTOMER' THEN RAISE EXCEPTION USING ERRCODE='P1303',MESSAGE='FORBIDDEN'; END IF;
 IF v_visit."Status"<>'SCHEDULED' OR v_visit."ActualReturnDate" IS NOT NULL THEN RAISE EXCEPTION USING ERRCODE='P1307',MESSAGE='VISIT_INVALID_STATUS'; END IF;
 IF v_contract."Status"<>'ACTIVE' THEN RAISE EXCEPTION USING ERRCODE='P1304',MESSAGE='CONTRACT_NOT_ACTIVE'; END IF;
 IF p_visit_date IS NULL THEN RAISE EXCEPTION USING ERRCODE='P1308',MESSAGE='VALIDATION_ERROR'; END IF;
 IF p_visit_date<(v_now AT TIME ZONE 'Asia/Ho_Chi_Minh')::date THEN RAISE EXCEPTION USING ERRCODE='P1306',MESSAGE='VISIT_DATE_IN_PAST'; END IF;
 UPDATE public."Visit" AS target SET "VisitDate"=p_visit_date WHERE target."VisitId"=p_visit_id;
 RETURN QUERY SELECT v.* FROM public."Visit" v WHERE v."VisitId"=p_visit_id;
END $frms$;

CREATE OR REPLACE FUNCTION public."usp_RescheduleVisit"(p_visit_id uuid,p_new_visit_date date)
RETURNS TABLE("VisitId" uuid,"VisitDate" date,"Status" varchar) LANGUAGE plpgsql AS $frms$
DECLARE v_visit public."Visit"%ROWTYPE; v_res public."Reservation"%ROWTYPE; v_contract public."Contract"%ROWTYPE; v_policy public."Policy"%ROWTYPE;
BEGIN
 PERFORM pg_advisory_xact_lock(70439102);
 SELECT * INTO v_visit FROM public."Visit" v WHERE v."VisitId"=p_visit_id FOR UPDATE;
 IF NOT FOUND OR v_visit."Status"<>'SCHEDULED' THEN RAISE EXCEPTION USING ERRCODE='P1112',MESSAGE='VISIT_INVALID_STATUS'; END IF;
 IF p_new_visit_date IS NULL OR p_new_visit_date<(clock_timestamp() AT TIME ZONE 'Asia/Ho_Chi_Minh')::date THEN RAISE EXCEPTION USING ERRCODE='P1123',MESSAGE='VISIT_DATE_IN_PAST'; END IF;
 IF v_visit."VisitType"='RESERVATION' THEN
  SELECT * INTO v_res FROM public."Reservation" r WHERE r."ReservationId"=v_visit."EntityId";
  SELECT * INTO v_policy FROM public."Policy" p WHERE p."PolicyId"=v_res."PolicyId";
  IF date_trunc('month',p_new_visit_date)::date IS DISTINCT FROM v_res."StartMonth" OR extract(day FROM p_new_visit_date) NOT BETWEEN v_policy."ReservationVisitStartDay" AND v_policy."ReservationVisitEndDay" THEN RAISE EXCEPTION USING ERRCODE='P1109',MESSAGE='VISIT_DATE_OUT_OF_POLICY'; END IF;
 ELSE
  SELECT * INTO v_contract FROM public."Contract" c WHERE c."ContractId"=v_visit."EntityId";
  IF v_contract."Status" IS DISTINCT FROM 'ACTIVE' THEN RAISE EXCEPTION USING ERRCODE='P1120',MESSAGE='CONTRACT_NOT_ACTIVE'; END IF;
  IF v_visit."VisitType"='ACCESS' AND (p_new_visit_date<v_contract."StartMonth" OR p_new_visit_date>(v_contract."EndMonth"+interval '1 month - 1 day')::date) THEN RAISE EXCEPTION USING ERRCODE='P1121',MESSAGE='ACCESS_VISIT_DATE_OUTSIDE_CONTRACT'; END IF;
 END IF;
 UPDATE public."Visit" AS target SET "VisitDate"=p_new_visit_date WHERE target."VisitId"=p_visit_id;
 RETURN QUERY SELECT p_visit_id,p_new_visit_date,'SCHEDULED'::varchar;
END $frms$;
