-- RES-001/004/005 and BR-VIS-05: atomic ports of frozen current routines.
-- Capacity writers serialize on Facility before reading holds; an absent
-- Reservation/Visit row is protected by its parent lock and unique index.
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

CREATE OR REPLACE FUNCTION public."usp_ConfirmReservation"(p_reservation_id uuid,p_reservation_visit_date date,p_now_utc timestamptz DEFAULT NULL)
RETURNS TABLE("VisitId" uuid,"Status" varchar) LANGUAGE plpgsql AS $frms$
DECLARE v_res public."Reservation"%ROWTYPE; v_visit uuid; v_policy public."Policy"%ROWTYPE; v_days int;
BEGIN
 PERFORM pg_advisory_xact_lock(70439102);
 SELECT * INTO v_res FROM public."Reservation" r WHERE r."ReservationId"=p_reservation_id FOR UPDATE;
 IF v_res."Status"='CONFIRMED' THEN
  SELECT v."VisitId" INTO v_visit FROM public."Visit" v WHERE v."EntityId"=p_reservation_id AND v."VisitType"='RESERVATION' FOR UPDATE;
  IF NOT FOUND THEN RAISE EXCEPTION USING ERRCODE='P1110',MESSAGE='RESERVATION_INVALID_STATUS'; END IF;
  RETURN QUERY SELECT v_visit,v_res."Status"; RETURN;
 END IF;
 IF v_res."Status" IS DISTINCT FROM 'PENDING_DEPOSIT' THEN RAISE EXCEPTION USING ERRCODE='P1107',MESSAGE='RESERVATION_INVALID_STATUS'; END IF;
 PERFORM 1 FROM public."Invoice" i WHERE i."EntityId"=p_reservation_id AND i."InvoiceType"='DEPOSIT' AND i."Status"='PAID' FOR SHARE;
 IF NOT FOUND THEN RAISE EXCEPTION USING ERRCODE='P1108',MESSAGE='DEPOSIT_NOT_PAID'; END IF;
 SELECT * INTO v_policy FROM public."Policy" p WHERE p."PolicyId"=v_res."PolicyId" FOR SHARE;
 v_days:=extract(day FROM v_res."StartMonth"+interval '1 month - 1 day')::int;
 IF p_reservation_visit_date IS NULL OR v_policy."PolicyId" IS NULL OR p_reservation_visit_date<v_res."StartMonth"+LEAST(v_policy."ReservationVisitStartDay",v_days)-1 OR p_reservation_visit_date>v_res."StartMonth"+LEAST(v_policy."ReservationVisitEndDay",v_days)-1 THEN
  RAISE EXCEPTION USING ERRCODE='P1109',MESSAGE='VISIT_DATE_OUT_OF_POLICY'; END IF;
 IF EXISTS(SELECT 1 FROM public."Visit" v WHERE v."EntityId"=p_reservation_id AND v."VisitType"='RESERVATION') THEN RAISE EXCEPTION USING ERRCODE='P1110',MESSAGE='RESERVATION_INVALID_STATUS'; END IF;
 v_visit:=gen_random_uuid();
 INSERT INTO public."Visit"("VisitId","EntityId","EmployeeId","VisitType","VisitDate","ActualReturnDate","Status") VALUES(v_visit,p_reservation_id,NULL,'RESERVATION',p_reservation_visit_date,NULL,'SCHEDULED');
 UPDATE public."Reservation" AS target SET "Status"='CONFIRMED' WHERE target."ReservationId"=p_reservation_id;
 RETURN QUERY SELECT v_visit,'CONFIRMED'::varchar;
END $frms$;

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
END $frms$;

CREATE OR REPLACE FUNCTION public."usp_CheckInVisit"(p_visit_id uuid,p_employee_id uuid,p_now_utc timestamptz DEFAULT NULL)
RETURNS void LANGUAGE plpgsql AS $frms$
DECLARE v_route public."Visit"%ROWTYPE; v_visit public."Visit"%ROWTYPE; v_facility uuid; v_status varchar;
BEGIN
 PERFORM pg_advisory_xact_lock(70439102);
 SELECT * INTO v_route FROM public."Visit" v WHERE v."VisitId"=p_visit_id;
 IF NOT FOUND OR v_route."VisitType" NOT IN ('RESERVATION','ACCESS','RETURN') THEN RAISE EXCEPTION USING ERRCODE='P1301',MESSAGE='VISIT_ENTITY_MISMATCH'; END IF;
 IF v_route."VisitType"='RESERVATION' THEN
  SELECT r."FacilityId",r."Status" INTO v_facility,v_status FROM public."Reservation" r WHERE r."ReservationId"=v_route."EntityId" FOR UPDATE;
 ELSE
  SELECT c."FacilityId",c."Status" INTO v_facility,v_status FROM public."Contract" c WHERE c."ContractId"=v_route."EntityId" FOR UPDATE;
 END IF;
 IF v_facility IS NULL THEN RAISE EXCEPTION USING ERRCODE='P1301',MESSAGE='VISIT_ENTITY_MISMATCH'; END IF;
 SELECT * INTO v_visit FROM public."Visit" v WHERE v."VisitId"=p_visit_id FOR UPDATE;
 IF NOT FOUND OR ROW(v_visit."EntityId",v_visit."VisitType") IS DISTINCT FROM ROW(v_route."EntityId",v_route."VisitType") THEN RAISE EXCEPTION USING ERRCODE='P1301',MESSAGE='VISIT_ENTITY_MISMATCH'; END IF;
 PERFORM 1 FROM public."Employee" e JOIN public."UserAccount" a ON a."UserAccountId"=e."UserAccountId" JOIN public."UserRole" r ON r."RoleId"=a."RoleId" WHERE e."EmployeeId"=p_employee_id AND e."FacilityId"=v_facility AND a."Status"='ACTIVE' AND r."RoleName"='FACILITY_STAFF' FOR SHARE OF e,a,r;
 IF NOT FOUND THEN RAISE EXCEPTION USING ERRCODE='P1302',MESSAGE='FORBIDDEN'; END IF;
 IF v_visit."Status"<>'SCHEDULED' OR (v_visit."VisitType"='RESERVATION' AND v_status<>'CONFIRMED') OR (v_visit."VisitType" IN ('ACCESS','RETURN') AND v_status<>'ACTIVE')
  OR v_visit."VisitDate"<>(clock_timestamp() AT TIME ZONE 'Asia/Ho_Chi_Minh')::date THEN RAISE EXCEPTION USING ERRCODE='P1303',MESSAGE='VISIT_INVALID_STATUS'; END IF;
 UPDATE public."Visit" AS target SET "Status"='CHECKED_IN',"EmployeeId"=p_employee_id WHERE target."VisitId"=p_visit_id;
END $frms$;
