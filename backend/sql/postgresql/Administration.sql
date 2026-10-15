-- Atomic ports of existing administration/policy routines. No new permissions.
CREATE OR REPLACE FUNCTION public."usp_CreatePolicyVersion"(p_deposit_timeout_hours int,p_reservation_visit_start_day int,p_reservation_visit_end_day int,p_monthly_payment_due_day int,p_overdue_start_day int,p_late_fee_divisor_days int,p_early_return_waive_fee_until_day int,p_now_utc timestamptz DEFAULT NULL)
RETURNS TABLE("PolicyId" uuid,"Version" int,"Status" varchar) LANGUAGE plpgsql AS $frms$
DECLARE v_now timestamptz(3):=coalesce(p_now_utc,clock_timestamp()); v_current uuid; v_next int; v_new uuid:=gen_random_uuid();
BEGIN
 PERFORM pg_advisory_xact_lock(70439103);
 SELECT p."PolicyId",p."Version"+1 INTO v_current,v_next FROM public."Policy" p WHERE p."Status"='ACTIVE' ORDER BY p."Version" DESC LIMIT 1 FOR UPDATE;
 IF v_next IS NULL THEN SELECT coalesce(max(p."Version"),0)+1 INTO v_next FROM public."Policy" p; END IF;
 IF v_current IS NOT NULL THEN UPDATE public."Policy" AS target SET "Status"='INACTIVE',"EffectiveTo"=v_now WHERE target."PolicyId"=v_current; END IF;
 INSERT INTO public."Policy"("PolicyId","Version","Status","EffectiveFrom","EffectiveTo","DepositTimeoutHours","ReservationVisitStartDay","ReservationVisitEndDay","MonthlyPaymentDueDay","OverdueStartDay","LateFeeDivisorDays","EarlyReturnWaiveFeeUntilDay","CreatedAt") VALUES(v_new,v_next,'ACTIVE',v_now,NULL,p_deposit_timeout_hours,p_reservation_visit_start_day,p_reservation_visit_end_day,p_monthly_payment_due_day,p_overdue_start_day,p_late_fee_divisor_days,p_early_return_waive_fee_until_day,v_now);
 RETURN QUERY SELECT v_new,v_next,'ACTIVE'::varchar;
END $frms$;

CREATE OR REPLACE FUNCTION public."usp_SetUserAccountStatus"(p_user_account_id uuid,p_new_status varchar)
RETURNS TABLE("UserAccountId" uuid,"Status" varchar) LANGUAGE plpgsql AS $frms$
BEGIN
 PERFORM pg_advisory_xact_lock(70439102);
 IF p_new_status IS NULL OR p_new_status NOT IN ('ACTIVE','INACTIVE') THEN RAISE EXCEPTION USING ERRCODE='P1148',MESSAGE='ACCOUNT_STATUS_INVALID'; END IF;
 PERFORM 1 FROM public."UserAccount" a WHERE a."UserAccountId"=p_user_account_id FOR UPDATE;
 IF NOT FOUND THEN RAISE EXCEPTION USING ERRCODE='P1150',MESSAGE='USER_ACCOUNT_NOT_FOUND'; END IF;
 IF p_new_status='INACTIVE' AND EXISTS(SELECT 1 FROM public."Customer" c WHERE c."UserAccountId"=p_user_account_id AND (EXISTS(SELECT 1 FROM public."Reservation" r WHERE r."CustomerId"=c."CustomerId" AND r."Status"='CONFIRMED') OR EXISTS(SELECT 1 FROM public."Contract" ct WHERE ct."CustomerId"=c."CustomerId" AND ct."Status"='ACTIVE'))) THEN
  RAISE EXCEPTION USING ERRCODE='P1149',MESSAGE='CUSTOMER_HAS_ACTIVE_LIFECYCLE'; END IF;
 UPDATE public."UserAccount" AS target SET "Status"=p_new_status WHERE target."UserAccountId"=p_user_account_id;
 RETURN QUERY SELECT p_user_account_id,p_new_status;
END $frms$;

CREATE OR REPLACE FUNCTION public."usp_UpdateEmployeeRoleFacility"(p_employee_id uuid,p_role_name varchar,p_facility_id uuid DEFAULT NULL)
RETURNS TABLE("EmployeeId" uuid,"RoleName" varchar,"FacilityId" uuid) LANGUAGE plpgsql AS $frms$
DECLARE v_account uuid; v_role uuid; v_prior text;
BEGIN
 PERFORM pg_advisory_xact_lock(70439102);
 IF p_role_name IS NULL OR p_role_name NOT IN ('FACILITY_STAFF','FACILITY_MANAGER','BUSINESS_OPERATIONS_MANAGER','SYSTEM_ADMINISTRATOR') THEN RAISE EXCEPTION USING ERRCODE='P1156',MESSAGE='EMPLOYEE_ROLE_INVALID'; END IF;
 IF p_role_name IN ('FACILITY_STAFF','FACILITY_MANAGER') AND p_facility_id IS NULL THEN RAISE EXCEPTION USING ERRCODE='P1157',MESSAGE='FACILITY_REQUIRED_FOR_EMPLOYEE_ROLE'; END IF;
 IF p_role_name IN ('BUSINESS_OPERATIONS_MANAGER','SYSTEM_ADMINISTRATOR') AND p_facility_id IS NOT NULL THEN RAISE EXCEPTION USING ERRCODE='P1158',MESSAGE='GLOBAL_ROLE_MUST_NOT_HAVE_FACILITY'; END IF;
 SELECT e."UserAccountId" INTO v_account FROM public."Employee" e WHERE e."EmployeeId"=p_employee_id FOR UPDATE;
 IF NOT FOUND THEN RAISE EXCEPTION USING ERRCODE='P1159',MESSAGE='EMPLOYEE_NOT_FOUND'; END IF;
 PERFORM 1 FROM public."UserAccount" a WHERE a."UserAccountId"=v_account FOR UPDATE;
 SELECT r."RoleId" INTO v_role FROM public."UserRole" r WHERE r."RoleName"=p_role_name;
 v_prior:=coalesce(current_setting('frms.role_facility_update',true),''); PERFORM set_config('frms.role_facility_update','1',true);
 UPDATE public."Employee" AS target SET "FacilityId"=p_facility_id WHERE target."EmployeeId"=p_employee_id;
 UPDATE public."UserAccount" AS target SET "RoleId"=v_role WHERE target."UserAccountId"=v_account;
 IF EXISTS(SELECT 1 FROM public."Employee" e JOIN public."UserAccount" a ON a."UserAccountId"=e."UserAccountId" JOIN public."UserRole" rr ON rr."RoleId"=a."RoleId" WHERE e."EmployeeId"=p_employee_id AND ((rr."RoleName" IN ('FACILITY_STAFF','FACILITY_MANAGER') AND e."FacilityId" IS NULL) OR (rr."RoleName" IN ('BUSINESS_OPERATIONS_MANAGER','SYSTEM_ADMINISTRATOR') AND e."FacilityId" IS NOT NULL) OR rr."RoleName"='CUSTOMER')) THEN
  RAISE EXCEPTION USING ERRCODE='P1160',MESSAGE='EMPLOYEE_ROLE_FACILITY_INVALID'; END IF;
 PERFORM set_config('frms.role_facility_update',v_prior,true);
 RETURN QUERY SELECT p_employee_id,p_role_name,p_facility_id;
END $frms$;

CREATE OR REPLACE FUNCTION public."usp_CreateCustomerAccount"(p_email varchar,p_phone_number varchar,p_password_hash varchar,p_full_name varchar,p_address varchar DEFAULT NULL,p_cccd varchar DEFAULT NULL)
RETURNS TABLE("UserAccountId" uuid,"CustomerId" uuid) LANGUAGE plpgsql AS $frms$
DECLARE v_account uuid:=gen_random_uuid(); v_customer uuid:=gen_random_uuid(); v_role uuid;
BEGIN
 PERFORM pg_advisory_xact_lock(70439104);
 IF EXISTS(SELECT 1 FROM public."UserAccount" a WHERE a."Email"=p_email) THEN RAISE EXCEPTION USING ERRCODE='P1155',MESSAGE='EMAIL_ALREADY_EXISTS'; END IF;
 IF EXISTS(SELECT 1 FROM public."UserAccount" a WHERE a."PhoneNumber"=p_phone_number) THEN RAISE EXCEPTION USING ERRCODE='P1207',MESSAGE='PHONE_NUMBER_ALREADY_EXISTS'; END IF;
 SELECT r."RoleId" INTO v_role FROM public."UserRole" r WHERE r."RoleName"='CUSTOMER';
 INSERT INTO public."UserAccount"("UserAccountId","RoleId","Email","PhoneNumber","PasswordHash","Status") VALUES(v_account,v_role,p_email,p_phone_number,p_password_hash,'ACTIVE');
 INSERT INTO public."Customer"("CustomerId","UserAccountId","FullName","Address","CCCD") VALUES(v_customer,v_account,p_full_name,p_address,p_cccd);
 RETURN QUERY SELECT v_account,v_customer;
END $frms$;

CREATE OR REPLACE FUNCTION public."usp_CreateEmployeeAccount"(p_email varchar,p_phone_number varchar,p_password_hash varchar,p_full_name varchar,p_role_name varchar,p_facility_id uuid DEFAULT NULL)
RETURNS TABLE("UserAccountId" uuid,"EmployeeId" uuid) LANGUAGE plpgsql AS $frms$
DECLARE v_account uuid:=gen_random_uuid(); v_employee uuid:=gen_random_uuid(); v_role uuid;
BEGIN
 PERFORM pg_advisory_xact_lock(70439104);
 IF p_role_name IS NULL OR p_role_name NOT IN ('FACILITY_STAFF','FACILITY_MANAGER','BUSINESS_OPERATIONS_MANAGER','SYSTEM_ADMINISTRATOR') THEN RAISE EXCEPTION USING ERRCODE='P1156',MESSAGE='EMPLOYEE_ROLE_INVALID'; END IF;
 IF p_role_name IN ('FACILITY_STAFF','FACILITY_MANAGER') AND p_facility_id IS NULL THEN RAISE EXCEPTION USING ERRCODE='P1157',MESSAGE='FACILITY_REQUIRED_FOR_EMPLOYEE_ROLE'; END IF;
 IF p_role_name IN ('BUSINESS_OPERATIONS_MANAGER','SYSTEM_ADMINISTRATOR') AND p_facility_id IS NOT NULL THEN RAISE EXCEPTION USING ERRCODE='P1158',MESSAGE='GLOBAL_ROLE_MUST_NOT_HAVE_FACILITY'; END IF;
 IF EXISTS(SELECT 1 FROM public."UserAccount" a WHERE a."Email"=p_email) THEN RAISE EXCEPTION USING ERRCODE='P1155',MESSAGE='EMAIL_ALREADY_EXISTS'; END IF;
 IF EXISTS(SELECT 1 FROM public."UserAccount" a WHERE a."PhoneNumber"=p_phone_number) THEN RAISE EXCEPTION USING ERRCODE='P1207',MESSAGE='PHONE_NUMBER_ALREADY_EXISTS'; END IF;
 SELECT rr."RoleId" INTO v_role FROM public."UserRole" rr WHERE rr."RoleName"=p_role_name;
 INSERT INTO public."UserAccount"("UserAccountId","RoleId","Email","PhoneNumber","PasswordHash","Status") VALUES(v_account,v_role,p_email,p_phone_number,p_password_hash,'INACTIVE');
 INSERT INTO public."Employee"("EmployeeId","UserAccountId","FacilityId","FullName") VALUES(v_employee,v_account,p_facility_id,p_full_name);
 RETURN QUERY SELECT v_account,v_employee;
END $frms$;

CREATE OR REPLACE FUNCTION public."usp_ReplaceInactiveEmployeeCredentialHash"(p_employee_id uuid,p_new_password_hash varchar)
RETURNS TABLE("EmployeeId" uuid,"UserAccountId" uuid,"Status" varchar) LANGUAGE plpgsql AS $frms$
DECLARE v_account uuid; v_status varchar;
BEGIN
 IF nullif(btrim(p_new_password_hash),'') IS NULL THEN RAISE EXCEPTION USING ERRCODE='P1214',MESSAGE='PASSWORD_HASH_REQUIRED'; END IF;
 SELECT a."UserAccountId",a."Status" INTO v_account,v_status FROM public."Employee" e JOIN public."UserAccount" a ON a."UserAccountId"=e."UserAccountId" WHERE e."EmployeeId"=p_employee_id FOR UPDATE OF a;
 IF NOT FOUND THEN RAISE EXCEPTION USING ERRCODE='P1150',MESSAGE='USER_ACCOUNT_NOT_FOUND'; END IF;
 IF v_status<>'INACTIVE' THEN RAISE EXCEPTION USING ERRCODE='P1213',MESSAGE='CREDENTIAL_PROVISIONING_INVALID_STATUS'; END IF;
 UPDATE public."UserAccount" AS target SET "PasswordHash"=p_new_password_hash WHERE target."UserAccountId"=v_account AND target."Status"='INACTIVE';
 IF NOT FOUND THEN RAISE EXCEPTION USING ERRCODE='P1213',MESSAGE='CREDENTIAL_PROVISIONING_INVALID_STATUS'; END IF;
 RETURN QUERY SELECT p_employee_id,v_account,'INACTIVE'::varchar;
END $frms$;
