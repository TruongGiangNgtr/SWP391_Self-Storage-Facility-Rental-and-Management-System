CREATE OR REPLACE FUNCTION public.frms_reject_history_mutation() RETURNS trigger
LANGUAGE plpgsql AS $frms$
BEGIN
 RAISE EXCEPTION USING ERRCODE=TG_ARGV[0], MESSAGE=TG_TABLE_NAME || ' is append-only.';
END $frms$;
CREATE TRIGGER "trg_ContractExtension_AppendOnly" BEFORE UPDATE OR DELETE ON public."ContractExtension" FOR EACH ROW EXECUTE FUNCTION public.frms_reject_history_mutation('P1018');
CREATE TRIGGER "trg_LoginHistory_AppendOnly" BEFORE UPDATE OR DELETE ON public."LoginHistory" FOR EACH ROW EXECUTE FUNCTION public.frms_reject_history_mutation('P1019');
CREATE TRIGGER "trg_AuditLog_AppendOnly" BEFORE UPDATE OR DELETE ON public."AuditLog" FOR EACH ROW EXECUTE FUNCTION public.frms_reject_history_mutation('P1020');
