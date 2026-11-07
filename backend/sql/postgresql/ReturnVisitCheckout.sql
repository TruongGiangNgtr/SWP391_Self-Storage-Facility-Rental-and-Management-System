-- Pg020 / FWP-05, FWP-07, SSP-12, SSP-14.
-- Forward-only replacement of usp_FinalizeReturn. Preserve Pg005's historical
-- Returns.sql source; the new routine closes a physically confirmed RETURN
-- Visit atomically with the existing settlement, Contract, and Unit transitions.
-- External recovery has Inspection.VisitId = NULL and does not create a Visit.
CREATE OR REPLACE FUNCTION public."usp_FinalizeReturn"(p_contract_id uuid,p_storage_unit_status varchar,p_employee_id uuid)
RETURNS void LANGUAGE plpgsql AS $frms$
DECLARE v_contract public."Contract"%ROWTYPE; v_unit public."StorageUnit"%ROWTYPE; v_inspection public."Inspection"%ROWTYPE; v_actual date; v_actor uuid; v_deposit numeric(18,2); v_late numeric(18,2); v_extra numeric(18,2); v_damage numeric(18,2); v_total numeric(18,2); v_refund numeric(18,2); v_additional numeric(18,2); v_terminal varchar; v_prior_actor text;
BEGIN
 PERFORM pg_advisory_xact_lock(70439102);
 IF p_contract_id IS NULL OR p_employee_id IS NULL OR p_storage_unit_status IS NULL OR p_storage_unit_status NOT IN ('AVAILABLE','MAINTENANCE') THEN RAISE EXCEPTION USING ERRCODE='P1280',MESSAGE='VALIDATION_ERROR'; END IF;
 SELECT * INTO v_contract FROM public."Contract" c WHERE c."ContractId"=p_contract_id FOR UPDATE;
 IF NOT FOUND THEN RAISE EXCEPTION USING ERRCODE='P1281',MESSAGE='RESOURCE_NOT_FOUND'; END IF;
 SELECT * INTO v_unit FROM public."StorageUnit" s WHERE s."StorageUnitId"=v_contract."StorageUnitId" FOR UPDATE;
 IF v_contract."Status"<>'ACTIVE' OR EXISTS(SELECT 1 FROM public."DepositSettlement" d WHERE d."ContractId"=p_contract_id) THEN RAISE EXCEPTION USING ERRCODE='P1282',MESSAGE='RETURN_ALREADY_FINALIZED'; END IF;
 SELECT e."UserAccountId" INTO v_actor FROM public."Employee" e JOIN public."UserAccount" a ON a."UserAccountId"=e."UserAccountId" JOIN public."UserRole" rr ON rr."RoleId"=a."RoleId" WHERE e."EmployeeId"=p_employee_id AND e."FacilityId"=v_contract."FacilityId" AND a."Status"='ACTIVE' AND rr."RoleName"='FACILITY_STAFF' FOR SHARE OF e,a,rr;
 IF NOT FOUND THEN RAISE EXCEPTION USING ERRCODE='P1283',MESSAGE='FORBIDDEN'; END IF;
 IF v_unit."Status"<>'INSPECTION' THEN RAISE EXCEPTION USING ERRCODE='P1284',MESSAGE='UNIT_INVALID_STATUS'; END IF;
 IF EXISTS(SELECT 1 FROM public."Inspection" i WHERE i."ContractId"=p_contract_id AND i."StorageUnitId"=v_unit."StorageUnitId" AND i."Status"<>'COMPLETED') THEN RAISE EXCEPTION USING ERRCODE='P1285',MESSAGE='INSPECTION_INVALID_STATUS'; END IF;
 SELECT * INTO v_inspection FROM public."Inspection" i WHERE i."ContractId"=p_contract_id AND i."StorageUnitId"=v_unit."StorageUnitId" AND i."Status"='COMPLETED' ORDER BY i."CompletedAt" DESC NULLS LAST,i."InspectionId" DESC LIMIT 1 FOR UPDATE;
 IF NOT FOUND THEN RAISE EXCEPTION USING ERRCODE='P1285',MESSAGE='INSPECTION_INVALID_STATUS'; END IF;
 IF v_inspection."VisitId" IS NOT NULL THEN
  SELECT v."ActualReturnDate" INTO v_actual FROM public."Visit" v WHERE v."VisitId"=v_inspection."VisitId" AND v."EntityId"=p_contract_id AND v."VisitType"='RETURN' FOR UPDATE;
  IF v_actual IS NULL THEN RAISE EXCEPTION USING ERRCODE='P1286',MESSAGE='VISIT_ENTITY_MISMATCH'; END IF;
 END IF;
 -- Damage decision writers share the Contract lock, preventing a phantom PENDING decision.
 IF EXISTS(SELECT 1 FROM public."DamageRecord" d WHERE d."InspectionId"=v_inspection."InspectionId" AND d."Status"='PENDING') THEN RAISE EXCEPTION USING ERRCODE='P1287',MESSAGE='DAMAGE_DECISION_PENDING'; END IF;
 SELECT sum(i."AmountDue") INTO v_deposit FROM public."Invoice" i WHERE i."EntityId"=v_contract."ReservationId" AND i."InvoiceType"='DEPOSIT' AND i."Status"='PAID';
 IF v_deposit IS NULL THEN RAISE EXCEPTION USING ERRCODE='P1288',MESSAGE='VALIDATION_ERROR'; END IF;
 SELECT coalesce(sum(l."Amount"),0) INTO v_late FROM public."LateFee" l JOIN public."Invoice" i ON i."InvoiceId"=l."InvoiceId" WHERE i."EntityId"=p_contract_id AND i."InvoiceType"='RENTAL_FEE' AND i."Status"<>'CANCELLED';
 SELECT coalesce(sum(e."Amount"),0) INTO v_extra FROM public."ExtraFee" e WHERE e."InspectionId"=v_inspection."InspectionId";
 SELECT coalesce(sum(d."DamageAmount"),0) INTO v_damage FROM public."DamageRecord" d WHERE d."InspectionId"=v_inspection."InspectionId" AND d."Status"='APPROVED';
 v_total:=v_late+v_extra+v_damage; v_additional:=greatest(v_total-v_deposit,0);
 IF v_actual IS NOT NULL AND v_actual<(v_contract."EndMonth"+interval '1 month - 1 day')::date THEN v_terminal:='TERMINATED'; v_refund:=0;
 ELSE v_terminal:='COMPLETED'; v_refund:=greatest(v_deposit-v_total,0); END IF;
 INSERT INTO public."DepositSettlement"("DepositSettlementId","ContractId","TotalDeduction","RefundAmount","AdditionalAmountDue","Status","CalculatedAt") VALUES(gen_random_uuid(),p_contract_id,v_total,v_refund,v_additional,'FINALIZED',clock_timestamp());
 UPDATE public."Contract" AS target SET "Status"=v_terminal WHERE target."ContractId"=p_contract_id AND target."Status"='ACTIVE';
 IF NOT FOUND THEN RAISE EXCEPTION USING ERRCODE='P1282',MESSAGE='RETURN_ALREADY_FINALIZED'; END IF;
 UPDATE public."StorageUnit" AS target SET "Status"=p_storage_unit_status WHERE target."StorageUnitId"=v_unit."StorageUnitId" AND target."Status"='INSPECTION';
 IF NOT FOUND THEN RAISE EXCEPTION USING ERRCODE='P1284',MESSAGE='UNIT_INVALID_STATUS'; END IF;
 -- Closing the visit is part of the same transaction, not a separate OPS-003 call.
 -- The guard rejects stale/non-confirmed RETURN Visits and rolls back ALL writes.
 IF v_inspection."VisitId" IS NOT NULL THEN
  -- The Visit status-audit trigger records the finalizing Staff, not an anonymous
  -- database session. Preserve the caller's transaction-local audit context.
  v_prior_actor:=coalesce(current_setting('frms.user_account_id',true),'');
  PERFORM set_config('frms.user_account_id',v_actor::text,true);
  UPDATE public."Visit" AS target
     SET "Status"='CHECKED_OUT'
   WHERE target."VisitId"=v_inspection."VisitId"
     AND target."EntityId"=p_contract_id
     AND target."VisitType"='RETURN'
     AND target."Status"='CHECKED_IN'
     AND target."ActualReturnDate" IS NOT NULL;
  IF NOT FOUND THEN RAISE EXCEPTION USING ERRCODE='P1112',MESSAGE='VISIT_INVALID_STATUS'; END IF;
  PERFORM set_config('frms.user_account_id',v_prior_actor,true);
 END IF;
 INSERT INTO public."AuditLog"("UserAccountId","Action","EntityType","EntityId","CreatedAt") VALUES(v_actor,'FINALIZE_RETURN','CONTRACT',p_contract_id,clock_timestamp());
END $frms$;
