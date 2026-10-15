-- Existing inspection claim, damage, fee, evidence and decision routines.
-- Inspection completion continues through the existing EF repository path;
-- do not import legacy CompleteInspection that released a Unit too early.
CREATE OR REPLACE FUNCTION public."usp_ClaimInspection"(p_inspection_id uuid,p_employee_id uuid)
RETURNS TABLE("InspectionId" uuid,"EmployeeId" uuid,"Status" varchar) LANGUAGE plpgsql AS $frms$
DECLARE v_facility uuid;
BEGIN
 PERFORM pg_advisory_xact_lock(70439102);
 SELECT c."FacilityId" INTO v_facility FROM public."Inspection" i JOIN public."Contract" c ON c."ContractId"=i."ContractId" WHERE i."InspectionId"=p_inspection_id;
 PERFORM 1 FROM public."Employee" e JOIN public."UserAccount" a ON a."UserAccountId"=e."UserAccountId" JOIN public."UserRole" rr ON rr."RoleId"=a."RoleId" WHERE e."EmployeeId"=p_employee_id AND e."FacilityId"=v_facility AND a."Status"='ACTIVE' AND rr."RoleName"='FACILITY_STAFF' FOR SHARE OF e,a,rr;
 IF NOT FOUND THEN RAISE EXCEPTION USING ERRCODE='P1114',MESSAGE='STAFF_FACILITY_OR_ROLE_INVALID'; END IF;
 UPDATE public."Inspection" AS target SET "Status"='IN_PROGRESS',"EmployeeId"=p_employee_id WHERE target."InspectionId"=p_inspection_id AND target."Status"='PENDING';
 IF NOT FOUND THEN RAISE EXCEPTION USING ERRCODE='P1125',MESSAGE='INSPECTION_ALREADY_CLAIMED'; END IF;
 RETURN QUERY SELECT p_inspection_id,p_employee_id,'IN_PROGRESS'::varchar;
END $frms$;

CREATE OR REPLACE FUNCTION public."usp_RecordDamage"(p_inspection_id uuid,p_damage_type_id uuid,p_damage_amount numeric DEFAULT NULL,p_note varchar DEFAULT NULL)
RETURNS TABLE("DamageRecordId" uuid) LANGUAGE plpgsql AS $frms$
DECLARE v_amount numeric(18,2); v_default numeric(18,2); v_id uuid:=gen_random_uuid();
BEGIN
 PERFORM pg_advisory_xact_lock(70439102);
 PERFORM 1 FROM public."Inspection" i WHERE i."InspectionId"=p_inspection_id AND i."Status"='IN_PROGRESS' FOR UPDATE;
 IF NOT FOUND THEN RAISE EXCEPTION USING ERRCODE='P1126',MESSAGE='INSPECTION_INVALID_STATUS'; END IF;
 SELECT d."DefaultAmount" INTO v_default FROM public."DamageType" d WHERE d."DamageTypeId"=p_damage_type_id AND d."Status"='ACTIVE' FOR SHARE;
 IF NOT FOUND THEN RAISE EXCEPTION USING ERRCODE='P1212',MESSAGE='DAMAGE_TYPE_NOT_ACTIVE'; END IF;
 v_amount:=coalesce(p_damage_amount,v_default);
 IF v_amount IS NULL OR v_amount<0 THEN RAISE EXCEPTION USING ERRCODE='P1127',MESSAGE='DAMAGE_AMOUNT_REQUIRED'; END IF;
 INSERT INTO public."DamageRecord"("DamageRecordId","InspectionId","DamageTypeId","DamageAmount","Note","Status") VALUES(v_id,p_inspection_id,p_damage_type_id,v_amount,p_note,'PENDING');
 RETURN QUERY SELECT v_id;
END $frms$;

-- Named arguments preserve the legacy Amount/Reason contract; PostgreSQL
-- requires required arguments to precede arguments with defaults.
CREATE OR REPLACE FUNCTION public."usp_RecordExtraFee"(p_inspection_id uuid,p_extra_fee_type_id uuid,p_reason varchar,p_amount numeric DEFAULT NULL)
RETURNS TABLE("ExtraFeeId" uuid) LANGUAGE plpgsql AS $frms$
DECLARE v_amount numeric(18,2); v_default numeric(18,2); v_id uuid:=gen_random_uuid();
BEGIN
 PERFORM pg_advisory_xact_lock(70439102);
 PERFORM 1 FROM public."Inspection" i WHERE i."InspectionId"=p_inspection_id AND i."Status"='IN_PROGRESS' FOR UPDATE;
 IF NOT FOUND THEN RAISE EXCEPTION USING ERRCODE='P1126',MESSAGE='INSPECTION_INVALID_STATUS'; END IF;
 SELECT e."DefaultAmount" INTO v_default FROM public."ExtraFeeType" e WHERE e."ExtraFeeTypeId"=p_extra_fee_type_id AND e."Status"='ACTIVE' FOR SHARE;
 IF v_default IS NULL THEN RAISE EXCEPTION USING ERRCODE='P1128',MESSAGE='EXTRA_FEE_TYPE_NOT_ACTIVE'; END IF;
 v_amount:=coalesce(p_amount,v_default);
 IF v_amount<0 THEN RAISE EXCEPTION USING ERRCODE='P1129',MESSAGE='EXTRA_FEE_AMOUNT_INVALID'; END IF;
 INSERT INTO public."ExtraFee"("ExtraFeeId","InspectionId","ExtraFeeTypeId","Amount","Reason") VALUES(v_id,p_inspection_id,p_extra_fee_type_id,v_amount,p_reason);
 RETURN QUERY SELECT v_id;
END $frms$;

CREATE OR REPLACE FUNCTION public."usp_AddInspectionEvidence"(p_inspection_id uuid,p_file_data bytea,p_evidence_type varchar)
RETURNS TABLE("InspectionEvidenceId" uuid) LANGUAGE plpgsql AS $frms$
DECLARE v_id uuid:=gen_random_uuid();
BEGIN
 PERFORM pg_advisory_xact_lock(70439102);
 PERFORM 1 FROM public."Inspection" i WHERE i."InspectionId"=p_inspection_id AND i."Status" IN ('PENDING','IN_PROGRESS') FOR SHARE;
 IF NOT FOUND THEN RAISE EXCEPTION USING ERRCODE='P1126',MESSAGE='INSPECTION_INVALID_STATUS'; END IF;
 IF p_file_data IS NULL OR octet_length(p_file_data)=0 THEN RAISE EXCEPTION USING ERRCODE='P1130',MESSAGE='EVIDENCE_FILE_REQUIRED'; END IF;
 INSERT INTO public."InspectionEvidence"("InspectionEvidenceId","InspectionId","FileData","EvidenceType") VALUES(v_id,p_inspection_id,p_file_data,p_evidence_type);
 RETURN QUERY SELECT v_id;
END $frms$;

CREATE OR REPLACE FUNCTION public."usp_DecideDamage"(p_damage_record_id uuid,p_manager_employee_id uuid,p_decision varchar)
RETURNS TABLE("DamageRecordId" uuid,"Status" varchar) LANGUAGE plpgsql AS $frms$
DECLARE v_facility uuid; v_status varchar; v_actor uuid;
BEGIN
 PERFORM pg_advisory_xact_lock(70439102);
 IF p_decision IS NULL OR p_decision NOT IN ('APPROVED','REJECTED') THEN RAISE EXCEPTION USING ERRCODE='P1209',MESSAGE='DAMAGE_INVALID_STATUS'; END IF;
 SELECT c."FacilityId",d."Status" INTO v_facility,v_status FROM public."DamageRecord" d JOIN public."Inspection" i ON i."InspectionId"=d."InspectionId" JOIN public."Contract" c ON c."ContractId"=i."ContractId" WHERE d."DamageRecordId"=p_damage_record_id FOR UPDATE OF d;
 IF NOT FOUND THEN RAISE EXCEPTION USING ERRCODE='P1210',MESSAGE='DAMAGE_RECORD_NOT_FOUND'; END IF;
 IF v_status<>'PENDING' THEN RAISE EXCEPTION USING ERRCODE='P1209',MESSAGE='DAMAGE_INVALID_STATUS'; END IF;
 SELECT e."UserAccountId" INTO v_actor FROM public."Employee" e JOIN public."UserAccount" a ON a."UserAccountId"=e."UserAccountId" JOIN public."UserRole" rr ON rr."RoleId"=a."RoleId" WHERE e."EmployeeId"=p_manager_employee_id AND e."FacilityId"=v_facility AND a."Status"='ACTIVE' AND rr."RoleName"='FACILITY_MANAGER' FOR SHARE OF e,a,rr;
 IF NOT FOUND THEN RAISE EXCEPTION USING ERRCODE='P1147',MESSAGE='FACILITY_MANAGER_SCOPE_INVALID'; END IF;
 UPDATE public."DamageRecord" AS target SET "Status"=p_decision WHERE target."DamageRecordId"=p_damage_record_id AND target."Status"='PENDING';
 IF NOT FOUND THEN RAISE EXCEPTION USING ERRCODE='P1209',MESSAGE='DAMAGE_INVALID_STATUS'; END IF;
 INSERT INTO public."AuditLog"("UserAccountId","Action","EntityType","EntityId","NewValue","CreatedAt") VALUES(v_actor,'DECIDE_DAMAGE','DAMAGE_RECORD',p_damage_record_id,jsonb_build_object('Status',p_decision)::text,clock_timestamp());
 RETURN QUERY SELECT p_damage_record_id,p_decision;
END $frms$;

CREATE OR REPLACE FUNCTION public."usp_SetDamageRecordStatus"(p_damage_record_id uuid,p_manager_employee_id uuid,p_new_status varchar)
RETURNS TABLE("DamageRecordId" uuid,"Status" varchar) LANGUAGE sql AS $frms$
 SELECT * FROM public."usp_DecideDamage"(p_damage_record_id,p_manager_employee_id,p_new_status);
$frms$;
