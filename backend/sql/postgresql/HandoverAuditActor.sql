-- Preserve SRS authenticated Staff audit identity using the already validated
-- Employee account. Context is transaction-local and restored before returning.
-- Atomic frozen CompleteHandover and RenewContract ports. First month remains offline.
CREATE OR REPLACE FUNCTION public."usp_CompleteHandover"(p_reservation_id uuid,p_visit_id uuid,p_storage_unit_id uuid,p_employee_id uuid,p_discount_id uuid DEFAULT NULL,p_now_utc timestamptz DEFAULT NULL)
RETURNS TABLE("ContractId" uuid,"StorageUnitId" uuid,"ContractStatus" varchar,"Result" varchar) LANGUAGE plpgsql AS $frms$
DECLARE v_res public."Reservation"%ROWTYPE; v_visit public."Visit"%ROWTYPE; v_unit public."StorageUnit"%ROWTYPE; v_discount public."Discount"%ROWTYPE; v_contract public."Contract"%ROWTYPE; v_id uuid:=gen_random_uuid(); v_now timestamptz(3):=coalesce(p_now_utc,clock_timestamp()); v_actor uuid; v_prior_actor text;
BEGIN
 PERFORM pg_advisory_xact_lock(70439102);
 SELECT * INTO v_res FROM public."Reservation" r WHERE r."ReservationId"=p_reservation_id FOR UPDATE;
 IF NOT FOUND THEN RAISE EXCEPTION USING ERRCODE='P1101',MESSAGE='RESOURCE_NOT_FOUND'; END IF;
 SELECT * INTO v_contract FROM public."Contract" c WHERE c."ReservationId"=p_reservation_id;
 IF FOUND THEN RETURN QUERY SELECT v_contract."ContractId",v_contract."StorageUnitId",v_contract."Status",'ALREADY_COMPLETED'::varchar; RETURN; END IF;
 IF v_res."Status"<>'CONFIRMED' THEN RAISE EXCEPTION USING ERRCODE='P1107',MESSAGE='RESERVATION_INVALID_STATUS'; END IF;
 SELECT * INTO v_visit FROM public."Visit" v WHERE v."VisitId"=p_visit_id FOR UPDATE;
 IF NOT FOUND THEN RAISE EXCEPTION USING ERRCODE='P1101',MESSAGE='RESOURCE_NOT_FOUND'; END IF;
 IF v_visit."EntityId"<>p_reservation_id OR v_visit."VisitType"<>'RESERVATION' OR v_visit."Status"<>'CHECKED_IN' THEN RAISE EXCEPTION USING ERRCODE='P1112',MESSAGE='VISIT_INVALID_STATUS'; END IF;
 SELECT e."UserAccountId" INTO v_actor FROM public."Employee" e JOIN public."UserAccount" a ON a."UserAccountId"=e."UserAccountId" JOIN public."UserRole" rr ON rr."RoleId"=a."RoleId" WHERE e."EmployeeId"=p_employee_id AND e."FacilityId"=v_res."FacilityId" AND a."Status"='ACTIVE' AND rr."RoleName"='FACILITY_STAFF' FOR SHARE OF e,a,rr;
 IF NOT FOUND THEN RAISE EXCEPTION USING ERRCODE='P1114',MESSAGE='FORBIDDEN'; END IF;
 SELECT * INTO v_unit FROM public."StorageUnit" s WHERE s."StorageUnitId"=p_storage_unit_id FOR UPDATE;
 IF NOT FOUND THEN RAISE EXCEPTION USING ERRCODE='P1101',MESSAGE='RESOURCE_NOT_FOUND'; END IF;
 IF v_unit."FacilityId"<>v_res."FacilityId" OR v_unit."UnitTypeId"<>v_res."UnitTypeId" THEN RAISE EXCEPTION USING ERRCODE='P1117',MESSAGE='UNIT_FACILITY_TYPE_MISMATCH'; END IF;
 IF v_unit."Status"<>'AVAILABLE' THEN RAISE EXCEPTION USING ERRCODE='P1117',MESSAGE='UNIT_NOT_AVAILABLE'; END IF;
 IF p_discount_id IS NOT NULL THEN
  SELECT * INTO v_discount FROM public."Discount" d WHERE d."DiscountId"=p_discount_id FOR SHARE;
  IF NOT FOUND THEN RAISE EXCEPTION USING ERRCODE='P1118',MESSAGE='DISCOUNT_NOT_VALID'; END IF;
  IF v_discount."CustomerId"<>v_res."CustomerId" THEN RAISE EXCEPTION USING ERRCODE='P1120',MESSAGE='DISCOUNT_NOT_OWNED_BY_CUSTOMER'; END IF;
  IF v_discount."Status"<>'ACTIVE' OR v_discount."EffectiveFrom">v_now OR (v_discount."EffectiveTo" IS NOT NULL AND v_discount."EffectiveTo"<v_now) THEN RAISE EXCEPTION USING ERRCODE='P1118',MESSAGE='DISCOUNT_NOT_VALID'; END IF;
 END IF;
 v_prior_actor:=coalesce(current_setting('frms.user_account_id',true),'');
 PERFORM set_config('frms.user_account_id',v_actor::text,true);
 INSERT INTO public."Contract"("ContractId","ReservationId","CustomerId","FacilityId","StorageUnitId","PolicyId","DiscountId","StartMonth","EndMonth","Status") VALUES(v_id,p_reservation_id,v_res."CustomerId",v_res."FacilityId",p_storage_unit_id,v_res."PolicyId",p_discount_id,v_res."StartMonth",v_res."EndMonth",'ACTIVE');
 -- No first-month Invoice/Payment; captured Policy and Discount are preserved.
 UPDATE public."StorageUnit" AS target SET "Status"='IN_USE' WHERE target."StorageUnitId"=p_storage_unit_id AND target."Status"='AVAILABLE';
 IF NOT FOUND THEN RAISE EXCEPTION USING ERRCODE='P1117',MESSAGE='UNIT_NOT_AVAILABLE'; END IF;
 UPDATE public."Visit" AS target SET "Status"='CHECKED_OUT',"EmployeeId"=p_employee_id WHERE target."VisitId"=p_visit_id AND target."Status"='CHECKED_IN';
 IF NOT FOUND THEN RAISE EXCEPTION USING ERRCODE='P1112',MESSAGE='VISIT_INVALID_STATUS'; END IF;
 UPDATE public."Reservation" AS target SET "Status"='COMPLETED' WHERE target."ReservationId"=p_reservation_id AND target."Status"='CONFIRMED';
 IF NOT FOUND THEN RAISE EXCEPTION USING ERRCODE='P1107',MESSAGE='RESERVATION_INVALID_STATUS'; END IF;
 PERFORM set_config('frms.user_account_id',v_prior_actor,true);
 RETURN QUERY SELECT v_id,p_storage_unit_id,'ACTIVE'::varchar,'COMPLETED'::varchar;
END $frms$;
