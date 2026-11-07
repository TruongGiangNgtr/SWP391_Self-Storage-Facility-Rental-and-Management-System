-- Pg020 DBT-RET-CHECKOUT: run ONLY against an approved disposable PostgreSQL
-- database migrated through Pg020; e.g. psql -v ON_ERROR_STOP=1 -f <file>.
-- Exercises actual stored procedures and rolls back all fixture rows.
-- Case 1: normal physical return; case 2: early return; case 3: external
-- recovery (no Visit); case 4: stale Visit must roll back settlement/Unit.
BEGIN;
SET LOCAL frms.email_events_enabled = 'off';
DO $test$
DECLARE
 v_customer_account uuid:=gen_random_uuid();
 v_staff_account uuid:=gen_random_uuid();
 v_manager_account uuid:=gen_random_uuid();
 v_customer uuid:=gen_random_uuid();
 v_staff uuid:=gen_random_uuid();
 v_manager uuid:=gen_random_uuid();
 v_facility uuid:=gen_random_uuid();
 v_type uuid:=gen_random_uuid();
 v_role_customer uuid;
 v_role_staff uuid;
 v_role_manager uuid;
 v_policy uuid;
 v_case integer;
 v_res uuid;
 v_handover_visit uuid;
 v_unit uuid;
 v_return_visit uuid;
 v_contract uuid;
 v_inspection uuid;
 v_deposit_invoice uuid;
 v_actual date;
 v_end_month date;
 v_visit_status varchar;
 v_contract_status varchar;
 v_unit_status varchar;
 v_settlements bigint;
 v_checkout_actor uuid;
BEGIN
 SELECT "RoleId" INTO v_role_customer FROM public."UserRole" WHERE "RoleName"='CUSTOMER';
 SELECT "RoleId" INTO v_role_staff FROM public."UserRole" WHERE "RoleName"='FACILITY_STAFF';
 SELECT "RoleId" INTO v_role_manager FROM public."UserRole" WHERE "RoleName"='FACILITY_MANAGER';
 SELECT "PolicyId" INTO v_policy FROM public."Policy" ORDER BY "Version" DESC LIMIT 1;
 IF v_role_customer IS NULL OR v_role_staff IS NULL OR v_role_manager IS NULL OR v_policy IS NULL THEN
  RAISE EXCEPTION 'Test database lacks expected master role/policy seed';
 END IF;
 INSERT INTO public."UserAccount"("UserAccountId","RoleId","Email","PhoneNumber","PasswordHash","Status")
 VALUES
  (v_customer_account,v_role_customer,v_customer_account::text||'@example.invalid',substr(replace(v_customer_account::text,'-',''),1,20),'fixture-only','ACTIVE'),
  (v_staff_account,v_role_staff,v_staff_account::text||'@example.invalid',substr(replace(v_staff_account::text,'-',''),1,20),'fixture-only','ACTIVE'),
  (v_manager_account,v_role_manager,v_manager_account::text||'@example.invalid',substr(replace(v_manager_account::text,'-',''),1,20),'fixture-only','ACTIVE');
 INSERT INTO public."Customer"("CustomerId","UserAccountId","FullName")
 VALUES(v_customer,v_customer_account,'Return checkout regression');
 INSERT INTO public."Facility"("FacilityId","Name","Address","Status")
 VALUES(v_facility,'Return checkout '||v_facility::text,'Disposable only','ACTIVE');
 INSERT INTO public."UnitType"("UnitTypeId","Name","Mode","Size","RentalPrice")
 VALUES(v_type,'Return test '||v_type::text,'PUBLIC','test',12500.00);
 INSERT INTO public."Employee"("EmployeeId","UserAccountId","FacilityId","FullName")
 VALUES(v_staff,v_staff_account,v_facility,'Staff fixture'),
       (v_manager,v_manager_account,v_facility,'Manager fixture');

 FOR v_case IN 1..4 LOOP
  v_res:=gen_random_uuid();
  v_handover_visit:=gen_random_uuid();
  v_unit:=gen_random_uuid();
  v_return_visit:=gen_random_uuid();
  v_deposit_invoice:=gen_random_uuid();
  v_actual:=CASE WHEN v_case=2 THEN DATE '2027-01-15' ELSE DATE '2027-01-31' END;
  v_end_month:=CASE WHEN v_case=2 THEN DATE '2027-02-01' ELSE DATE '2027-01-01' END;
  INSERT INTO public."StorageUnit"("StorageUnitId","FacilityId","UnitTypeId","UnitCode","Status")
  VALUES(v_unit,v_facility,v_type,'RT-'||v_unit::text,'AVAILABLE');
  INSERT INTO public."Reservation"("ReservationId","CustomerId","FacilityId","UnitTypeId","PolicyId","StartMonth","EndMonth","LockedRentalPrice","DepositAmount","Status")
  VALUES(v_res,v_customer,v_facility,v_type,v_policy,DATE '2027-01-01',v_end_month,12500.00,12500.00,'CONFIRMED');
  INSERT INTO public."Invoice"("InvoiceId","EntityId","InvoiceType","BillingMonth","BaseAmount","DiscountAmount","AmountDue","DueDate","Status")
  VALUES(v_deposit_invoice,v_res,'DEPOSIT',NULL,12500.00,0,12500.00,clock_timestamp(),'PAID');
  INSERT INTO public."Visit"("VisitId","EntityId","VisitType","VisitDate","Status","EmployeeId")
  VALUES(v_handover_visit,v_res,'RESERVATION',DATE '2027-01-03','CHECKED_IN',v_staff);
  PERFORM public."usp_AssignHandoverUnit"(v_res,v_unit,v_manager_account);
  SELECT "ContractId" INTO v_contract
    FROM public."usp_CompleteHandover"(v_res,v_handover_visit,v_unit,v_staff);
  IF v_contract IS NULL THEN RAISE EXCEPTION 'Fixture handover did not create Contract'; END IF;

  IF v_case <> 3 THEN
   INSERT INTO public."Visit"("VisitId","EntityId","VisitType","VisitDate","Status","EmployeeId")
   VALUES(v_return_visit,v_contract,'RETURN',v_actual,'CHECKED_IN',v_staff);
   SELECT "InspectionId" INTO v_inspection
     FROM public."usp_ConfirmActualReturn"(v_contract,v_staff,v_return_visit,v_actual);
  ELSE
   SELECT "InspectionId" INTO v_inspection
     FROM public."usp_ConfirmActualReturn"(v_contract,v_staff,NULL,v_actual);
  END IF;
  IF v_inspection IS NULL THEN RAISE EXCEPTION 'Fixture return did not create Inspection'; END IF;
  UPDATE public."Inspection" SET "Status"='IN_PROGRESS',"EmployeeId"=v_staff
   WHERE "InspectionId"=v_inspection AND "Status"='PENDING';
  UPDATE public."Inspection" SET "Status"='COMPLETED',"ConditionNote"='No damage',"CompletedAt"=clock_timestamp()
   WHERE "InspectionId"=v_inspection AND "Status"='IN_PROGRESS';

  IF v_case=4 THEN
   -- Force a stale Visit before finalization: failed checkout MUST undo the
   -- settlement, terminal Contract, and Unit release made earlier in the SP.
   UPDATE public."Visit" SET "Status"='CHECKED_OUT' WHERE "VisitId"=v_return_visit;
   BEGIN
    PERFORM public."usp_FinalizeReturn"(v_contract,'AVAILABLE',v_staff);
    RAISE EXCEPTION 'Expected VISIT_INVALID_STATUS';
   EXCEPTION WHEN SQLSTATE 'P1112' THEN NULL;
   END;
  ELSE
   PERFORM public."usp_FinalizeReturn"(v_contract,
     CASE WHEN v_case=2 THEN 'MAINTENANCE' ELSE 'AVAILABLE' END,v_staff);
  END IF;
  SELECT "Status" INTO v_contract_status FROM public."Contract" WHERE "ContractId"=v_contract;
  SELECT "Status" INTO v_unit_status FROM public."StorageUnit" WHERE "StorageUnitId"=v_unit;
  SELECT count(*) INTO v_settlements FROM public."DepositSettlement" WHERE "ContractId"=v_contract;
  IF v_case<>3 THEN
   SELECT "Status" INTO v_visit_status FROM public."Visit" WHERE "VisitId"=v_return_visit;
  ELSE
   IF EXISTS(SELECT 1 FROM public."Visit" WHERE "EntityId"=v_contract AND "VisitType"='RETURN') THEN
    RAISE EXCEPTION 'External recovery created an unexpected RETURN Visit';
   END IF;
  END IF;
  IF v_case=1 AND (v_visit_status<>'CHECKED_OUT' OR v_contract_status<>'COMPLETED' OR v_unit_status<>'AVAILABLE' OR v_settlements<>1) THEN
   RAISE EXCEPTION 'Normal return mismatch: visit %, contract %, unit %, settlements %',v_visit_status,v_contract_status,v_unit_status,v_settlements;
  ELSIF v_case=2 AND (v_visit_status<>'CHECKED_OUT' OR v_contract_status<>'TERMINATED' OR v_unit_status<>'MAINTENANCE' OR v_settlements<>1) THEN
   RAISE EXCEPTION 'Early return mismatch: visit %, contract %, unit %, settlements %',v_visit_status,v_contract_status,v_unit_status,v_settlements;
  ELSIF v_case=3 AND (v_contract_status<>'COMPLETED' OR v_unit_status<>'AVAILABLE' OR v_settlements<>1) THEN
   RAISE EXCEPTION 'External recovery mismatch: contract %, unit %, settlements %',v_contract_status,v_unit_status,v_settlements;
  ELSIF v_case=4 AND (v_visit_status<>'CHECKED_OUT' OR v_contract_status<>'ACTIVE' OR v_unit_status<>'INSPECTION' OR v_settlements<>0) THEN
   RAISE EXCEPTION 'Failed-finalize rollback mismatch: visit %, contract %, unit %, settlements %',v_visit_status,v_contract_status,v_unit_status,v_settlements;
  END IF;
  IF v_case IN (1,2) THEN
   SELECT "UserAccountId" INTO v_checkout_actor
     FROM public."AuditLog"
    WHERE "EntityId"=v_return_visit AND "EntityType"='VISIT'
      AND "Action"='CHECK_OUT_VISIT'
    ORDER BY "CreatedAt" DESC, "AuditLogId" DESC LIMIT 1;
   IF v_checkout_actor IS DISTINCT FROM v_staff_account THEN
    RAISE EXCEPTION 'Checkout audit actor mismatch: %, expected %',v_checkout_actor,v_staff_account;
   END IF;
  END IF;
  IF v_case=1 THEN
   BEGIN
    PERFORM public."usp_FinalizeReturn"(v_contract,'MAINTENANCE',v_staff);
    RAISE EXCEPTION 'Expected RETURN_ALREADY_FINALIZED on retry';
   EXCEPTION WHEN SQLSTATE 'P1282' THEN NULL;
   END;
   IF (SELECT "Status" FROM public."StorageUnit" WHERE "StorageUnitId"=v_unit)<>'AVAILABLE'
      OR (SELECT count(*) FROM public."DepositSettlement" WHERE "ContractId"=v_contract)<>1 THEN
    RAISE EXCEPTION 'Retry modified the original finalization outcome';
   END IF;
  END IF;
 END LOOP;
 RAISE NOTICE 'Pg020 DBT-RET-CHECKOUT: 4 scenarios and retry passed; rolling back all fixture data';
END $test$;
ROLLBACK;
