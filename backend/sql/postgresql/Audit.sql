-- SRS 0.0.2/16.5: authoritative payment results have one audit;
-- pending attempt/session writes do not fabricate result events.
-- Safe structured audit metadata only: no passwords, credentials, URLs or binary evidence.
CREATE OR REPLACE FUNCTION public.frms_audit_change() RETURNS trigger LANGUAGE plpgsql AS $frms$
DECLARE v_action varchar; v_actor uuid; v_entity uuid; v_actor_text text;
BEGIN
 v_actor_text:=nullif(current_setting('frms.user_account_id',true),'');
 BEGIN v_actor:=v_actor_text::uuid; EXCEPTION WHEN invalid_text_representation THEN v_actor:=NULL; END;
 v_entity:=(to_jsonb(NEW)->>TG_ARGV[1])::uuid;
 CASE TG_TABLE_NAME
 WHEN 'Reservation' THEN v_action:=CASE WHEN TG_OP='INSERT' THEN 'CREATE_RESERVATION' WHEN OLD."Status"<>NEW."Status" AND NEW."Status"='CONFIRMED' THEN 'CONFIRM_RESERVATION' WHEN OLD."Status"<>NEW."Status" AND NEW."Status"='CANCELLED' THEN 'CANCEL_RESERVATION' ELSE 'UPDATE_RESERVATION' END;
 WHEN 'Visit' THEN v_action:=CASE WHEN TG_OP='INSERT' THEN 'CREATE_VISIT' WHEN OLD."Status"<>NEW."Status" AND NEW."Status"='CHECKED_IN' THEN 'CHECK_IN_VISIT' WHEN OLD."Status"<>NEW."Status" AND NEW."Status"='CHECKED_OUT' THEN 'CHECK_OUT_VISIT' WHEN OLD."Status"<>NEW."Status" AND NEW."Status"='CANCELLED' THEN 'CANCEL_VISIT' ELSE 'UPDATE_VISIT' END;
 WHEN 'Contract' THEN v_action:=CASE WHEN TG_OP='INSERT' THEN 'COMPLETE_HANDOVER' WHEN OLD."Status"<>NEW."Status" AND NEW."Status" IN ('COMPLETED','TERMINATED') THEN 'FINALIZE_RETURN' ELSE 'UPDATE_CONTRACT' END;
 WHEN 'Invoice' THEN v_action:=CASE WHEN TG_OP='INSERT' AND NEW."InvoiceType"='RENTAL_FEE' THEN 'CREATE_RENTAL_INVOICE' WHEN TG_OP='INSERT' THEN 'CREATE_DEPOSIT_INVOICE' WHEN OLD."Status"<>NEW."Status" AND NEW."Status"='OVERDUE' THEN 'MARK_INVOICE_OVERDUE' ELSE 'UPDATE_INVOICE' END;
 WHEN 'Payment' THEN
  -- The authoritative result function emits its richer result audit atomically.
  IF current_setting('frms.payment_result',true)='1' THEN RETURN NEW; END IF;
  IF TG_OP<>'UPDATE' OR OLD."Status"<>'PENDING' OR NEW."Status" NOT IN ('SUCCESS','FAILED') THEN RETURN NEW; END IF;
  v_action:='PAYMENT_RESULT';
 WHEN 'ContractExtension' THEN v_action:='RENEW_CONTRACT';
 WHEN 'Inspection' THEN v_action:=CASE WHEN TG_OP='INSERT' THEN 'CREATE_INSPECTION' WHEN OLD."Status"<>NEW."Status" AND NEW."Status"='IN_PROGRESS' THEN 'CLAIM_INSPECTION' WHEN OLD."Status"<>NEW."Status" AND NEW."Status"='COMPLETED' THEN 'COMPLETE_INSPECTION' ELSE 'UPDATE_INSPECTION' END;
 WHEN 'ExtraFee' THEN v_action:='RECORD_EXTRA_FEE';
 WHEN 'DepositSettlement' THEN v_action:='FINALIZE_RETURN';
 WHEN 'Policy' THEN v_action:='CREATE_POLICY_VERSION';
 WHEN 'UnitType' THEN IF NEW."RentalPrice"=OLD."RentalPrice" THEN RETURN NEW; END IF; v_action:='UPDATE_UNIT_PRICE';
 WHEN 'Discount' THEN v_action:='CREATE_OR_UPDATE_DISCOUNT';
 WHEN 'SupportTicket' THEN v_action:=CASE WHEN TG_OP='INSERT' THEN 'CREATE_SUPPORT_TICKET' WHEN OLD."AssignedEmployeeId" IS NULL AND NEW."AssignedEmployeeId" IS NOT NULL THEN 'ASSIGN_SUPPORT_TICKET' WHEN OLD."Status"<>NEW."Status" AND NEW."Status"='COMPLETED' THEN 'COMPLETE_SUPPORT_TICKET' WHEN OLD."Status"<>NEW."Status" AND NEW."Status"='CANCELLED' THEN 'CANCEL_SUPPORT_TICKET' ELSE 'UPDATE_SUPPORT_TICKET' END;
 WHEN 'UserAccount' THEN IF NEW."Status"=OLD."Status" AND NEW."RoleId"=OLD."RoleId" THEN RETURN NEW; END IF; v_action:=CASE WHEN NEW."Status"<>OLD."Status" THEN 'CHANGE_ACCOUNT_STATUS' ELSE 'CHANGE_ACCOUNT' END;
 WHEN 'Employee' THEN IF NEW."FacilityId" IS NOT DISTINCT FROM OLD."FacilityId" THEN RETURN NEW; END IF; v_action:='CHANGE_EMPLOYEE_ROLE_FACILITY';
 WHEN 'DamageRecord' THEN IF TG_OP='UPDATE' AND NEW."Status"<>OLD."Status" THEN RETURN NEW; END IF; v_action:=CASE WHEN TG_OP='INSERT' THEN 'RECORD_DAMAGE' ELSE 'UPDATE_DAMAGE_RECORD' END;
 END CASE;
 INSERT INTO public."AuditLog"("UserAccountId","Action","EntityType","EntityId") VALUES(v_actor,v_action,TG_ARGV[0],v_entity);
 RETURN NEW;
END $frms$;
CREATE TRIGGER "trg_Audit_Reservation" AFTER INSERT OR UPDATE ON public."Reservation" FOR EACH ROW EXECUTE FUNCTION public.frms_audit_change('RESERVATION','ReservationId');
CREATE TRIGGER "trg_Audit_Visit" AFTER INSERT OR UPDATE ON public."Visit" FOR EACH ROW EXECUTE FUNCTION public.frms_audit_change('VISIT','VisitId');
CREATE TRIGGER "trg_Audit_Contract" AFTER INSERT OR UPDATE ON public."Contract" FOR EACH ROW EXECUTE FUNCTION public.frms_audit_change('CONTRACT','ContractId');
CREATE TRIGGER "trg_Audit_Invoice" AFTER INSERT OR UPDATE ON public."Invoice" FOR EACH ROW EXECUTE FUNCTION public.frms_audit_change('INVOICE','InvoiceId');
CREATE TRIGGER "trg_Audit_Payment" AFTER UPDATE ON public."Payment" FOR EACH ROW EXECUTE FUNCTION public.frms_audit_change('PAYMENT','PaymentId');
CREATE TRIGGER "trg_Audit_ContractExtension" AFTER INSERT ON public."ContractExtension" FOR EACH ROW EXECUTE FUNCTION public.frms_audit_change('CONTRACT_EXTENSION','ContractExtensionId');
CREATE TRIGGER "trg_Audit_Inspection" AFTER INSERT OR UPDATE ON public."Inspection" FOR EACH ROW EXECUTE FUNCTION public.frms_audit_change('INSPECTION','InspectionId');
CREATE TRIGGER "trg_Audit_ExtraFee" AFTER INSERT ON public."ExtraFee" FOR EACH ROW EXECUTE FUNCTION public.frms_audit_change('EXTRA_FEE','ExtraFeeId');
CREATE TRIGGER "trg_Audit_DepositSettlement" AFTER INSERT ON public."DepositSettlement" FOR EACH ROW EXECUTE FUNCTION public.frms_audit_change('DEPOSIT_SETTLEMENT','DepositSettlementId');
CREATE TRIGGER "trg_Audit_Policy" AFTER INSERT ON public."Policy" FOR EACH ROW EXECUTE FUNCTION public.frms_audit_change('POLICY','PolicyId');
CREATE TRIGGER "trg_Audit_UnitType" AFTER UPDATE ON public."UnitType" FOR EACH ROW EXECUTE FUNCTION public.frms_audit_change('UNIT_TYPE','UnitTypeId');
CREATE TRIGGER "trg_Audit_Discount" AFTER INSERT OR UPDATE ON public."Discount" FOR EACH ROW EXECUTE FUNCTION public.frms_audit_change('DISCOUNT','DiscountId');
CREATE TRIGGER "trg_Audit_SupportTicket" AFTER INSERT OR UPDATE ON public."SupportTicket" FOR EACH ROW EXECUTE FUNCTION public.frms_audit_change('SUPPORT_TICKET','SupportTicketId');
CREATE TRIGGER "trg_Audit_UserAccount" AFTER UPDATE ON public."UserAccount" FOR EACH ROW EXECUTE FUNCTION public.frms_audit_change('USER_ACCOUNT','UserAccountId');
CREATE TRIGGER "trg_Audit_Employee" AFTER UPDATE ON public."Employee" FOR EACH ROW EXECUTE FUNCTION public.frms_audit_change('EMPLOYEE','EmployeeId');
CREATE TRIGGER "trg_Audit_DamageRecord" AFTER INSERT OR UPDATE ON public."DamageRecord" FOR EACH ROW EXECUTE FUNCTION public.frms_audit_change('DAMAGE_RECORD','DamageRecordId');
