CREATE OR REPLACE FUNCTION public."usp_CreateSupportTicket"(p_contract_id uuid,p_customer_id uuid,p_category varchar,p_description varchar)
RETURNS TABLE("SupportTicketId" uuid,"Status" varchar) LANGUAGE plpgsql AS $frms$
DECLARE v_id uuid:=gen_random_uuid();
BEGIN
 PERFORM 1 FROM public."Contract" c WHERE c."ContractId"=p_contract_id AND c."CustomerId"=p_customer_id FOR SHARE;
 IF NOT FOUND THEN RAISE EXCEPTION USING ERRCODE='P1152',MESSAGE='SUPPORT_CONTRACT_NOT_OWNED'; END IF;
 INSERT INTO public."SupportTicket"("SupportTicketId","ContractId","CustomerId","AssignedEmployeeId","Category","Description","Status","ResultNote","CompletedAt") VALUES(v_id,p_contract_id,p_customer_id,NULL,p_category,p_description,'OPEN',NULL,NULL);
 RETURN QUERY SELECT v_id,'OPEN'::varchar;
END $frms$;

CREATE OR REPLACE FUNCTION public."usp_AssignSupportTicket"(p_support_ticket_id uuid,p_manager_employee_id uuid,p_staff_employee_id uuid)
RETURNS TABLE("SupportTicketId" uuid,"AssignedEmployeeId" uuid,"Status" varchar) LANGUAGE plpgsql AS $frms$
DECLARE v_facility uuid; v_status varchar;
BEGIN
 SELECT c."FacilityId",t."Status" INTO v_facility,v_status FROM public."SupportTicket" t JOIN public."Contract" c ON c."ContractId"=t."ContractId" WHERE t."SupportTicketId"=p_support_ticket_id FOR UPDATE OF t;
 IF NOT FOUND OR v_status<>'OPEN' THEN RAISE EXCEPTION USING ERRCODE='P1146',MESSAGE='SUPPORT_TICKET_INVALID_STATUS'; END IF;
 PERFORM 1 FROM public."Employee" e JOIN public."UserAccount" a ON a."UserAccountId"=e."UserAccountId" JOIN public."UserRole" r ON r."RoleId"=a."RoleId" WHERE e."EmployeeId"=p_manager_employee_id AND e."FacilityId"=v_facility AND a."Status"='ACTIVE' AND r."RoleName"='FACILITY_MANAGER' FOR SHARE OF e,a,r;
 IF NOT FOUND THEN RAISE EXCEPTION USING ERRCODE='P1147',MESSAGE='FACILITY_MANAGER_SCOPE_INVALID'; END IF;
 PERFORM 1 FROM public."Employee" e JOIN public."UserAccount" a ON a."UserAccountId"=e."UserAccountId" JOIN public."UserRole" r ON r."RoleId"=a."RoleId" WHERE e."EmployeeId"=p_staff_employee_id AND e."FacilityId"=v_facility AND a."Status"='ACTIVE' AND r."RoleName"='FACILITY_STAFF' FOR SHARE OF e,a,r;
 IF NOT FOUND THEN RAISE EXCEPTION USING ERRCODE='P1114',MESSAGE='STAFF_FACILITY_OR_ROLE_INVALID'; END IF;
 UPDATE public."SupportTicket" AS target SET "AssignedEmployeeId"=p_staff_employee_id,"Status"='IN_PROGRESS' WHERE target."SupportTicketId"=p_support_ticket_id AND target."Status"='OPEN';
 RETURN QUERY SELECT p_support_ticket_id,p_staff_employee_id,'IN_PROGRESS'::varchar;
END $frms$;

CREATE OR REPLACE FUNCTION public."usp_CompleteSupportTicket"(p_support_ticket_id uuid,p_staff_employee_id uuid,p_result_note varchar,p_now_utc timestamptz DEFAULT NULL)
RETURNS TABLE("SupportTicketId" uuid,"Status" varchar) LANGUAGE plpgsql AS $frms$
BEGIN
 UPDATE public."SupportTicket" AS target SET "ResultNote"=p_result_note,"CompletedAt"=coalesce(p_now_utc,clock_timestamp()),"Status"='COMPLETED' WHERE target."SupportTicketId"=p_support_ticket_id AND target."AssignedEmployeeId"=p_staff_employee_id AND target."Status"='IN_PROGRESS';
 IF NOT FOUND THEN RAISE EXCEPTION USING ERRCODE='P1146',MESSAGE='SUPPORT_TICKET_INVALID_STATUS_OR_ACTOR'; END IF;
 RETURN QUERY SELECT p_support_ticket_id,'COMPLETED'::varchar;
END $frms$;

CREATE OR REPLACE FUNCTION public."usp_CancelSupportTicket"(p_support_ticket_id uuid,p_customer_id uuid)
RETURNS TABLE("SupportTicketId" uuid,"Status" varchar) LANGUAGE plpgsql AS $frms$
BEGIN
 UPDATE public."SupportTicket" AS target SET "Status"='CANCELLED' WHERE target."SupportTicketId"=p_support_ticket_id AND target."CustomerId"=p_customer_id AND target."Status" IN ('OPEN','IN_PROGRESS');
 IF NOT FOUND THEN RAISE EXCEPTION USING ERRCODE='P1146',MESSAGE='SUPPORT_TICKET_INVALID_STATUS_OR_OWNER'; END IF;
 RETURN QUERY SELECT p_support_ticket_id,'CANCELLED'::varchar;
END $frms$;
