-- Ports the frozen SQL Server invariant triggers; no lifecycle orchestration.
CREATE OR REPLACE FUNCTION public.frms_validate_entity() RETURNS trigger
LANGUAGE plpgsql AS $frms$
BEGIN
 IF TG_TABLE_NAME='Visit' THEN
  IF (NEW."VisitType"='RESERVATION' AND NOT EXISTS(SELECT 1 FROM public."Reservation" r WHERE r."ReservationId"=NEW."EntityId"))
   OR (NEW."VisitType" IN ('ACCESS','RETURN') AND NOT EXISTS(SELECT 1 FROM public."Contract" c WHERE c."ContractId"=NEW."EntityId")) THEN
   RAISE EXCEPTION USING ERRCODE='P1001', MESSAGE='VISIT_ENTITY_MISMATCH'; END IF;
  IF NEW."VisitType"<>'RETURN' AND NEW."ActualReturnDate" IS NOT NULL THEN
   RAISE EXCEPTION USING ERRCODE='P1002', MESSAGE='ActualReturnDate is allowed only for RETURN visits.'; END IF;
 ELSIF TG_TABLE_NAME='Invoice' THEN
  IF (NEW."InvoiceType"='DEPOSIT' AND NOT EXISTS(SELECT 1 FROM public."Reservation" r WHERE r."ReservationId"=NEW."EntityId"))
   OR (NEW."InvoiceType"='RENTAL_FEE' AND NOT EXISTS(SELECT 1 FROM public."Contract" c WHERE c."ContractId"=NEW."EntityId")) THEN
   RAISE EXCEPTION USING ERRCODE='P1003', MESSAGE='INVOICE_ENTITY_MISMATCH'; END IF;
  IF (NEW."InvoiceType"='DEPOSIT' AND (NEW."DiscountId" IS NOT NULL OR NEW."DiscountAmount"<>0))
   OR (NEW."InvoiceType"='RENTAL_FEE' AND NEW."DiscountId" IS NOT NULL AND EXISTS(
    SELECT 1 FROM public."Contract" c WHERE c."ContractId"=NEW."EntityId" AND c."DiscountId" IS DISTINCT FROM NEW."DiscountId")) THEN
   RAISE EXCEPTION USING ERRCODE='P1004', MESSAGE='Invoice Discount must match the captured Contract; Deposit cannot use Discount.'; END IF;
 ELSIF TG_TABLE_NAME='Inspection' THEN
  IF NEW."VisitId" IS NOT NULL AND NOT EXISTS(SELECT 1 FROM public."Visit" v WHERE v."VisitId"=NEW."VisitId" AND v."VisitType"='RETURN' AND v."EntityId"=NEW."ContractId") THEN
   RAISE EXCEPTION USING ERRCODE='P1012', MESSAGE='Inspection VisitId must reference a RETURN Visit for the same Contract.'; END IF;
 ELSIF TG_TABLE_NAME='Contract' THEN
  IF NEW."DiscountId" IS NOT NULL AND (TG_OP='INSERT' OR NEW."DiscountId" IS DISTINCT FROM OLD."DiscountId")
   AND NOT EXISTS(SELECT 1 FROM public."Discount" d WHERE d."DiscountId"=NEW."DiscountId" AND d."CustomerId"=NEW."CustomerId" AND d."Status"='ACTIVE'
    AND d."EffectiveFrom"<=clock_timestamp() AND (d."EffectiveTo" IS NULL OR d."EffectiveTo">=clock_timestamp())) THEN
   RAISE EXCEPTION USING ERRCODE='P1021', MESSAGE='DISCOUNT_NOT_VALID_OR_NOT_OWNED_BY_CUSTOMER'; END IF;
 ELSIF TG_TABLE_NAME='LateFee' THEN
  IF EXISTS(SELECT 1 FROM public."Invoice" i LEFT JOIN public."Contract" c ON c."ContractId"=i."EntityId" AND i."InvoiceType"='RENTAL_FEE'
   LEFT JOIN public."Policy" p ON p."PolicyId"=c."PolicyId" WHERE i."InvoiceId"=NEW."InvoiceId" AND
   (i."InvoiceType"<>'RENTAL_FEE' OR i."Status" NOT IN ('OVERDUE','PAID') OR (i."Status"='PAID' AND NOT EXISTS(
    SELECT 1 FROM public."Payment" pay WHERE pay."InvoiceId"=i."InvoiceId" AND pay."Status"='SUCCESS' AND pay."PaidAt">=
    ((i."BillingMonth" + (LEAST(p."OverdueStartDay",extract(day FROM (i."BillingMonth"+interval '1 month - 1 day')))::int-1))::timestamp AT TIME ZONE 'Asia/Ho_Chi_Minh'))))) THEN
   RAISE EXCEPTION USING ERRCODE='P1024', MESSAGE='LateFee is valid only for an overdue Rental Fee invoice.'; END IF;
 END IF;
 RETURN NEW;
END $frms$;

CREATE OR REPLACE FUNCTION public.frms_validate_transition() RETURNS trigger LANGUAGE plpgsql AS $frms$
BEGIN
 IF TG_TABLE_NAME='Invoice' THEN
  IF NEW."Status" IS DISTINCT FROM OLD."Status" AND NOT ((OLD."Status"='UNPAID' AND NEW."Status" IN ('PAID','CANCELLED')) OR (OLD."Status"='UNPAID' AND NEW."Status"='OVERDUE' AND OLD."InvoiceType"='RENTAL_FEE') OR (OLD."Status"='OVERDUE' AND NEW."Status"='PAID')) THEN
   RAISE EXCEPTION USING ERRCODE='P1216', MESSAGE='INVOICE_INVALID_STATUS'; END IF;
 END IF;
 IF TG_TABLE_NAME='Reservation' THEN
  IF NEW."Status" IS DISTINCT FROM OLD."Status" AND NOT ((OLD."Status"='PENDING_DEPOSIT' AND NEW."Status" IN ('CONFIRMED','CANCELLED')) OR (OLD."Status"='CONFIRMED' AND NEW."Status" IN ('COMPLETED','CANCELLED'))) THEN
   RAISE EXCEPTION USING ERRCODE='P1005', MESSAGE='RESERVATION_INVALID_STATUS'; END IF;
 END IF;
 IF TG_TABLE_NAME='Visit' THEN
  IF NEW."Status" IS DISTINCT FROM OLD."Status" AND NOT ((OLD."Status"='SCHEDULED' AND NEW."Status" IN ('CHECKED_IN','CANCELLED')) OR (OLD."Status"='CHECKED_IN' AND NEW."Status"='CHECKED_OUT')) THEN
   RAISE EXCEPTION USING ERRCODE='P1007', MESSAGE='VISIT_INVALID_STATUS'; END IF;
 END IF;
 IF TG_TABLE_NAME='Contract' THEN
  IF NEW."Status" IS DISTINCT FROM OLD."Status" AND NOT (OLD."Status"='ACTIVE' AND NEW."Status" IN ('COMPLETED','TERMINATED')) THEN
   RAISE EXCEPTION USING ERRCODE='P1008', MESSAGE='CONTRACT_INVALID_STATUS'; END IF;
 END IF;
 IF TG_TABLE_NAME='Inspection' THEN
  IF NEW."Status" IS DISTINCT FROM OLD."Status" AND NOT ((OLD."Status"='PENDING' AND NEW."Status"='IN_PROGRESS') OR (OLD."Status"='IN_PROGRESS' AND NEW."Status"='COMPLETED')) THEN
   RAISE EXCEPTION USING ERRCODE='P1010', MESSAGE='INSPECTION_INVALID_STATUS'; END IF;
 END IF;
 IF TG_TABLE_NAME='SupportTicket' THEN
  IF NEW."Status" IS DISTINCT FROM OLD."Status" AND NOT ((OLD."Status"='OPEN' AND NEW."Status" IN ('IN_PROGRESS','CANCELLED')) OR (OLD."Status"='IN_PROGRESS' AND NEW."Status" IN ('COMPLETED','CANCELLED'))) THEN
   RAISE EXCEPTION USING ERRCODE='P1014', MESSAGE='SUPPORT_TICKET_INVALID_STATUS'; END IF;
 END IF;
 IF TG_TABLE_NAME='StorageUnit' THEN
  IF NEW."Status" IS DISTINCT FROM OLD."Status" AND NOT ((OLD."Status"='AVAILABLE' AND NEW."Status" IN ('IN_USE','MAINTENANCE')) OR (OLD."Status"='IN_USE' AND NEW."Status"='INSPECTION') OR (OLD."Status"='INSPECTION' AND NEW."Status" IN ('AVAILABLE','MAINTENANCE')) OR (OLD."Status"='MAINTENANCE' AND NEW."Status"='AVAILABLE')) THEN
   RAISE EXCEPTION USING ERRCODE='P1013', MESSAGE='Invalid StorageUnit status transition.'; END IF;
 END IF;
 IF TG_TABLE_NAME='DamageRecord' THEN
  IF NEW."Status" IS DISTINCT FROM OLD."Status" AND NOT (OLD."Status"='PENDING' AND NEW."Status" IN ('APPROVED','REJECTED')) THEN
   RAISE EXCEPTION USING ERRCODE='P1205', MESSAGE='DAMAGE_INVALID_STATUS'; END IF;
 END IF;
 RETURN NEW;
END $frms$;
CREATE OR REPLACE FUNCTION public.frms_protect_snapshot() RETURNS trigger LANGUAGE plpgsql AS $frms$
BEGIN
 IF TG_TABLE_NAME='Invoice' THEN
  IF (to_jsonb(NEW)-ARRAY['Status','PaidAt']) IS DISTINCT FROM (to_jsonb(OLD)-ARRAY['Status','PaidAt']) THEN
   RAISE EXCEPTION USING ERRCODE='P1215', MESSAGE='INVOICE_SNAPSHOT_IMMUTABLE'; END IF;
 ELSIF TG_TABLE_NAME='Reservation' THEN
  IF ROW(NEW."CustomerId",NEW."FacilityId",NEW."UnitTypeId",NEW."PolicyId",NEW."StartMonth",NEW."EndMonth",NEW."LockedRentalPrice",NEW."DepositAmount")
    IS DISTINCT FROM ROW(OLD."CustomerId",OLD."FacilityId",OLD."UnitTypeId",OLD."PolicyId",OLD."StartMonth",OLD."EndMonth",OLD."LockedRentalPrice",OLD."DepositAmount") THEN
   RAISE EXCEPTION USING ERRCODE='P1006', MESSAGE='Reservation business snapshot fields are immutable.'; END IF;
 ELSIF TG_TABLE_NAME='Contract' THEN
  IF NEW."DiscountId" IS DISTINCT FROM OLD."DiscountId" THEN RAISE EXCEPTION USING ERRCODE='P1009', MESSAGE='Contract-selected Discount is immutable in V1.'; END IF;
 ELSIF TG_TABLE_NAME='Inspection' THEN
  IF OLD."Status"='IN_PROGRESS' AND NEW."EmployeeId" IS DISTINCT FROM OLD."EmployeeId" THEN
   RAISE EXCEPTION USING ERRCODE='P1011', MESSAGE='Inspection claim actor cannot be overwritten.'; END IF;
 ELSIF TG_TABLE_NAME='Policy' THEN
  IF TG_OP='DELETE' THEN RAISE EXCEPTION USING ERRCODE='P1015', MESSAGE='Historical Policy rows cannot be deleted.'; END IF;
  IF (to_jsonb(NEW)-ARRAY['Status','EffectiveTo']) IS DISTINCT FROM (to_jsonb(OLD)-ARRAY['Status','EffectiveTo']) THEN
   RAISE EXCEPTION USING ERRCODE='P1016', MESSAGE='Historical Policy business values are immutable.'; END IF;
  IF NEW."EffectiveTo" IS DISTINCT FROM OLD."EffectiveTo" AND NOT (OLD."Status"='ACTIVE' AND NEW."Status"='INACTIVE' AND OLD."EffectiveTo" IS NULL AND NEW."EffectiveTo" IS NOT NULL) THEN
   RAISE EXCEPTION USING ERRCODE='P1016', MESSAGE='Historical Policy EffectiveTo is immutable after inactivation.'; END IF;
  IF NEW."Status" IS DISTINCT FROM OLD."Status" AND NOT (OLD."Status"='ACTIVE' AND NEW."Status"='INACTIVE') THEN
   RAISE EXCEPTION USING ERRCODE='P1017', MESSAGE='Invalid Policy status transition.'; END IF;
 ELSIF TG_TABLE_NAME='Discount' THEN
  IF EXISTS(SELECT 1 FROM public."Contract" c WHERE c."DiscountId"=NEW."DiscountId") AND
   ROW(NEW."CustomerId",NEW."Percentage",NEW."EffectiveFrom",NEW."EffectiveTo") IS DISTINCT FROM ROW(OLD."CustomerId",OLD."Percentage",OLD."EffectiveFrom",OLD."EffectiveTo") THEN
   RAISE EXCEPTION USING ERRCODE='P1204', MESSAGE='REFERENCED_DISCOUNT_SEMANTICS_IMMUTABLE'; END IF;
 ELSIF TG_TABLE_NAME='StorageUnit' THEN
  IF OLD."Status"='AVAILABLE' AND NEW."Status"='MAINTENANCE' AND EXISTS(SELECT 1 FROM public."Contract" c WHERE c."StorageUnitId"=NEW."StorageUnitId" AND c."Status"='ACTIVE') THEN
   RAISE EXCEPTION USING ERRCODE='P1202', MESSAGE='UNIT_ACTIVE_CONTRACT_BLOCKS_MAINTENANCE'; END IF;
  IF NEW."UnitTypeId" IS DISTINCT FROM OLD."UnitTypeId" AND (OLD."Status" IN ('IN_USE','INSPECTION') OR NEW."Status" IN ('IN_USE','INSPECTION') OR EXISTS(
   SELECT 1 FROM public."Contract" c WHERE c."StorageUnitId"=NEW."StorageUnitId" AND c."Status"='ACTIVE')) THEN
   RAISE EXCEPTION USING ERRCODE='P1203', MESSAGE='UNIT_TYPE_REASSIGNMENT_BLOCKED'; END IF;
 ELSIF TG_TABLE_NAME='UserAccount' THEN
  IF NEW."Email" IS DISTINCT FROM OLD."Email" OR NEW."PhoneNumber" IS DISTINCT FROM OLD."PhoneNumber" THEN
   RAISE EXCEPTION USING ERRCODE='P1206', MESSAGE='LOGIN_IDENTIFIER_IMMUTABLE'; END IF;
 END IF;
 RETURN NEW;
END $frms$;

CREATE OR REPLACE FUNCTION public.frms_validate_role_profile() RETURNS trigger LANGUAGE plpgsql AS $frms$
DECLARE v_role varchar; v_facility uuid; v_account uuid;
BEGIN
 IF current_setting('frms.role_facility_update',true)='1' THEN RETURN NEW; END IF;
 IF TG_TABLE_NAME='Employee' THEN
  SELECT r."RoleName" INTO v_role FROM public."UserAccount" a JOIN public."UserRole" r ON r."RoleId"=a."RoleId" WHERE a."UserAccountId"=NEW."UserAccountId";
  IF (v_role IN ('FACILITY_STAFF','FACILITY_MANAGER') AND NEW."FacilityId" IS NULL)
   OR (v_role IN ('BUSINESS_OPERATIONS_MANAGER','SYSTEM_ADMINISTRATOR') AND NEW."FacilityId" IS NOT NULL) OR v_role='CUSTOMER' THEN
   RAISE EXCEPTION USING ERRCODE='P1022', MESSAGE='EMPLOYEE_ROLE_FACILITY_INVALID'; END IF;
 ELSE
  SELECT r."RoleName" INTO v_role FROM public."UserRole" r WHERE r."RoleId"=NEW."RoleId";
  IF (v_role='CUSTOMER' AND EXISTS(SELECT 1 FROM public."Employee" e WHERE e."UserAccountId"=NEW."UserAccountId"))
   OR (v_role<>'CUSTOMER' AND EXISTS(SELECT 1 FROM public."Customer" c WHERE c."UserAccountId"=NEW."UserAccountId"))
   OR EXISTS(SELECT 1 FROM public."Employee" e WHERE e."UserAccountId"=NEW."UserAccountId" AND (
    (v_role IN ('FACILITY_STAFF','FACILITY_MANAGER') AND e."FacilityId" IS NULL) OR (v_role IN ('BUSINESS_OPERATIONS_MANAGER','SYSTEM_ADMINISTRATOR') AND e."FacilityId" IS NOT NULL))) THEN
   RAISE EXCEPTION USING ERRCODE='P1023', MESSAGE='ACCOUNT_ROLE_PROFILE_INVALID'; END IF;
 END IF;
 RETURN NEW;
END $frms$;
CREATE TRIGGER "trg_Visit_ValidateEntity" BEFORE INSERT OR UPDATE ON public."Visit" FOR EACH ROW EXECUTE FUNCTION public.frms_validate_entity();
CREATE TRIGGER "trg_Invoice_ValidateEntity" BEFORE INSERT OR UPDATE ON public."Invoice" FOR EACH ROW EXECUTE FUNCTION public.frms_validate_entity();
CREATE TRIGGER "trg_Inspection_ValidateVisit" BEFORE INSERT OR UPDATE ON public."Inspection" FOR EACH ROW EXECUTE FUNCTION public.frms_validate_entity();
CREATE TRIGGER "trg_Contract_ValidateDiscountOwnership" BEFORE INSERT OR UPDATE ON public."Contract" FOR EACH ROW EXECUTE FUNCTION public.frms_validate_entity();
CREATE TRIGGER "trg_LateFee_ValidateInvoice" BEFORE INSERT OR UPDATE ON public."LateFee" FOR EACH ROW EXECUTE FUNCTION public.frms_validate_entity();
CREATE TRIGGER "trg_Invoice_StatusTransition" BEFORE UPDATE ON public."Invoice" FOR EACH ROW EXECUTE FUNCTION public.frms_validate_transition();
CREATE TRIGGER "trg_Reservation_StatusTransition" BEFORE UPDATE ON public."Reservation" FOR EACH ROW EXECUTE FUNCTION public.frms_validate_transition();
CREATE TRIGGER "trg_Visit_StatusTransition" BEFORE UPDATE ON public."Visit" FOR EACH ROW EXECUTE FUNCTION public.frms_validate_transition();
CREATE TRIGGER "trg_Contract_StatusTransition" BEFORE UPDATE ON public."Contract" FOR EACH ROW EXECUTE FUNCTION public.frms_validate_transition();
CREATE TRIGGER "trg_Inspection_StatusTransition" BEFORE UPDATE ON public."Inspection" FOR EACH ROW EXECUTE FUNCTION public.frms_validate_transition();
CREATE TRIGGER "trg_SupportTicket_StatusTransition" BEFORE UPDATE ON public."SupportTicket" FOR EACH ROW EXECUTE FUNCTION public.frms_validate_transition();
CREATE TRIGGER "trg_StorageUnit_StatusTransition" BEFORE UPDATE ON public."StorageUnit" FOR EACH ROW EXECUTE FUNCTION public.frms_validate_transition();
CREATE TRIGGER "trg_DamageRecord_StatusTransition" BEFORE UPDATE ON public."DamageRecord" FOR EACH ROW EXECUTE FUNCTION public.frms_validate_transition();
CREATE TRIGGER "trg_Invoice_ProtectSnapshot" BEFORE UPDATE ON public."Invoice" FOR EACH ROW EXECUTE FUNCTION public.frms_protect_snapshot();
CREATE TRIGGER "trg_Reservation_ProtectSnapshot" BEFORE UPDATE ON public."Reservation" FOR EACH ROW EXECUTE FUNCTION public.frms_protect_snapshot();
CREATE TRIGGER "trg_Contract_ProtectSnapshot" BEFORE UPDATE ON public."Contract" FOR EACH ROW EXECUTE FUNCTION public.frms_protect_snapshot();
CREATE TRIGGER "trg_Inspection_ProtectSnapshot" BEFORE UPDATE ON public."Inspection" FOR EACH ROW EXECUTE FUNCTION public.frms_protect_snapshot();
CREATE TRIGGER "trg_Policy_ProtectSnapshot" BEFORE UPDATE OR DELETE ON public."Policy" FOR EACH ROW EXECUTE FUNCTION public.frms_protect_snapshot();
CREATE TRIGGER "trg_Discount_ProtectSnapshot" BEFORE UPDATE ON public."Discount" FOR EACH ROW EXECUTE FUNCTION public.frms_protect_snapshot();
CREATE TRIGGER "trg_StorageUnit_ProtectSnapshot" BEFORE UPDATE ON public."StorageUnit" FOR EACH ROW EXECUTE FUNCTION public.frms_protect_snapshot();
CREATE TRIGGER "trg_UserAccount_ProtectSnapshot" BEFORE UPDATE ON public."UserAccount" FOR EACH ROW EXECUTE FUNCTION public.frms_protect_snapshot();
CREATE TRIGGER "trg_Employee_ValidateFacilityByRole" BEFORE INSERT OR UPDATE ON public."Employee" FOR EACH ROW EXECUTE FUNCTION public.frms_validate_role_profile();
CREATE TRIGGER "trg_UserAccount_ValidateRoleProfile" BEFORE UPDATE ON public."UserAccount" FOR EACH ROW EXECUTE FUNCTION public.frms_validate_role_profile();

-- PostgreSQL has no SQL Server key-range locking hints. Serialize capacity
-- writers before acquiring row locks, including EF-issued statements. The
-- coarse transaction lock is a correctness-first local baseline; it never
-- allocates a physical Unit and does not change lifecycle decisions.
CREATE OR REPLACE FUNCTION public.frms_lock_capacity_writes() RETURNS trigger LANGUAGE plpgsql AS $frms$
BEGIN PERFORM pg_advisory_xact_lock(70439102); RETURN NULL; END $frms$;
CREATE TRIGGER "trg_Reservation_CapacityWriterLock" BEFORE INSERT OR UPDATE OR DELETE ON public."Reservation" FOR EACH STATEMENT EXECUTE FUNCTION public.frms_lock_capacity_writes();
CREATE TRIGGER "trg_Contract_CapacityWriterLock" BEFORE INSERT OR UPDATE OR DELETE ON public."Contract" FOR EACH STATEMENT EXECUTE FUNCTION public.frms_lock_capacity_writes();
CREATE TRIGGER "trg_StorageUnit_CapacityWriterLock" BEFORE INSERT OR UPDATE OR DELETE ON public."StorageUnit" FOR EACH STATEMENT EXECUTE FUNCTION public.frms_lock_capacity_writes();
CREATE TRIGGER "trg_Facility_CapacityWriterLock" BEFORE INSERT OR UPDATE OR DELETE ON public."Facility" FOR EACH STATEMENT EXECUTE FUNCTION public.frms_lock_capacity_writes();
CREATE TRIGGER "trg_UnitType_CapacityWriterLock" BEFORE INSERT OR UPDATE OR DELETE ON public."UnitType" FOR EACH STATEMENT EXECUTE FUNCTION public.frms_lock_capacity_writes();
CREATE TRIGGER "trg_Inspection_LifecycleWriterLock" BEFORE INSERT OR UPDATE OR DELETE ON public."Inspection" FOR EACH STATEMENT EXECUTE FUNCTION public.frms_lock_capacity_writes();
CREATE TRIGGER "trg_DamageRecord_LifecycleWriterLock" BEFORE INSERT OR UPDATE OR DELETE ON public."DamageRecord" FOR EACH STATEMENT EXECUTE FUNCTION public.frms_lock_capacity_writes();
CREATE TRIGGER "trg_ExtraFee_LifecycleWriterLock" BEFORE INSERT OR UPDATE OR DELETE ON public."ExtraFee" FOR EACH STATEMENT EXECUTE FUNCTION public.frms_lock_capacity_writes();
CREATE TRIGGER "trg_LateFee_LifecycleWriterLock" BEFORE INSERT OR UPDATE OR DELETE ON public."LateFee" FOR EACH STATEMENT EXECUTE FUNCTION public.frms_lock_capacity_writes();
CREATE TRIGGER "trg_DepositSettlement_LifecycleWriterLock" BEFORE INSERT OR UPDATE OR DELETE ON public."DepositSettlement" FOR EACH STATEMENT EXECUTE FUNCTION public.frms_lock_capacity_writes();
