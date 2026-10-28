-- REV-2026-10-29-INAPP-NOTIFICATIONS, approved NOT-D01/NOT-D02.
-- New notices use NotificationLog.IsInApp; no publication/backfill of legacy rows.
-- Serialize migration with an existing sender lease before changing routing.
SELECT pg_advisory_xact_lock(70439120);

CREATE OR REPLACE FUNCTION public.frms_is_od06_notification_type(p_type varchar)
RETURNS boolean LANGUAGE sql IMMUTABLE PARALLEL SAFE AS $frms$
 SELECT coalesce(p_type COLLATE "C" = ANY(ARRAY[
  'RESERVATION_CONFIRMED','RESERVATION_CANCELLED',
  'DEPOSIT_INVOICE_CREATED','RENTAL_INVOICE_CREATED','PAYMENT_CONFIRMED','INVOICE_OVERDUE','INVOICE_CANCELLED',
  'VISIT_SCHEDULED','VISIT_RESCHEDULED','VISIT_CHECKED_IN','VISIT_CHECKED_OUT','VISIT_CANCELLED',
  'CONTRACT_CREATED','CONTRACT_CLOSED','CONTRACT_RENEWED',
  'SUPPORT_TICKET_CREATED','SUPPORT_TICKET_STATUS','DEPOSIT_SETTLEMENT_FINALIZED'
 ]::varchar[]),false);
$frms$;

ALTER TABLE public."NotificationLog" ADD CONSTRAINT "CK_NotificationLog_InApp"
 CHECK (NOT "IsInApp" OR (public.frms_is_od06_notification_type("NotificationType") AND "Status"='PENDING' AND "SentAt" IS NULL));

-- Existing Pg014 trigger functions still call this helper. Record in the SAME
-- transaction, using the existing authoritative Customer -> UserAccount mapping.
-- Preserve the Pg014 active-recipient behavior and generic, non-sensitive text.
CREATE OR REPLACE FUNCTION public.frms_email_queue_customer_event(
 p_customer_id uuid,p_type varchar,p_content varchar)
RETURNS void LANGUAGE plpgsql AS $frms$
DECLARE v_user_account_id uuid;
BEGIN
 IF p_customer_id IS NULL THEN RETURN; END IF;
 IF NOT public.frms_is_od06_notification_type(p_type) THEN
  RAISE EXCEPTION USING ERRCODE='P1152',MESSAGE='NOTIFICATION_EVENT_NOT_SUPPORTED';
 END IF;
 SELECT c."UserAccountId" INTO v_user_account_id
 FROM public."Customer" c JOIN public."UserAccount" u ON u."UserAccountId"=c."UserAccountId"
 WHERE c."CustomerId"=p_customer_id AND u."Status"='ACTIVE';
 IF FOUND THEN
  INSERT INTO public."NotificationLog"("NotificationLogId","UserAccountId","NotificationType","Content","Status","SentAt","IsInApp")
  VALUES(gen_random_uuid(),v_user_account_id,p_type,p_content,'PENDING',NULL,true);
 END IF;
END $frms$;

-- Worker filtering also covers old PENDING business notices without rewriting
-- their delivery evidence. Generic/account email queue behavior stays intact.
CREATE OR REPLACE FUNCTION public."usp_RetryPendingNotification"()
RETURNS TABLE("NotificationLogId" uuid,"UserAccountId" uuid,"NotificationType" varchar,"Content" varchar,"CreatedAt" timestamptz)
LANGUAGE plpgsql AS $frms$
BEGIN
 RETURN QUERY SELECT n."NotificationLogId",n."UserAccountId",n."NotificationType",n."Content",n."CreatedAt"
 FROM public."NotificationLog" n
 WHERE n."Status"='PENDING' AND NOT n."IsInApp"
  AND NOT public.frms_is_od06_notification_type(n."NotificationType")
 ORDER BY n."CreatedAt" ASC,n."NotificationLogId" ASC LIMIT 1 FOR UPDATE SKIP LOCKED;
END $frms$;

CREATE OR REPLACE FUNCTION public."usp_MarkNotificationSent"(p_notification_log_id uuid,p_sent_at timestamptz DEFAULT NULL)
RETURNS void LANGUAGE plpgsql AS $frms$
BEGIN
 UPDATE public."NotificationLog" AS n SET "Status"='SENT',"SentAt"=coalesce(p_sent_at,clock_timestamp())
 WHERE n."NotificationLogId"=p_notification_log_id AND n."Status"='PENDING'
  AND NOT n."IsInApp" AND NOT public.frms_is_od06_notification_type(n."NotificationType");
 IF NOT FOUND THEN RAISE EXCEPTION USING ERRCODE='P1151',MESSAGE='NOTIFICATION_NOT_PENDING'; END IF;
END $frms$;