CREATE OR REPLACE FUNCTION public."usp_QueueNotification"(p_user_account_id uuid,p_notification_type varchar,p_content varchar)
RETURNS TABLE("NotificationLogId" uuid) LANGUAGE plpgsql AS $frms$
DECLARE v_id uuid:=gen_random_uuid();
BEGIN
 INSERT INTO public."NotificationLog"("NotificationLogId","UserAccountId","NotificationType","Content","Status","SentAt") VALUES(v_id,p_user_account_id,p_notification_type,p_content,'PENDING',NULL);
 RETURN QUERY SELECT v_id;
END $frms$;

CREATE OR REPLACE FUNCTION public."usp_MarkNotificationSent"(p_notification_log_id uuid,p_sent_at timestamptz DEFAULT NULL)
RETURNS void LANGUAGE plpgsql AS $frms$
BEGIN
 UPDATE public."NotificationLog" AS n SET "Status"='SENT',"SentAt"=coalesce(p_sent_at,clock_timestamp()) WHERE n."NotificationLogId"=p_notification_log_id AND n."Status"='PENDING';
 IF NOT FOUND THEN RAISE EXCEPTION USING ERRCODE='P1151',MESSAGE='NOTIFICATION_NOT_PENDING'; END IF;
END $frms$;

CREATE OR REPLACE FUNCTION public."usp_RetryPendingNotification"()
RETURNS TABLE("NotificationLogId" uuid,"UserAccountId" uuid,"NotificationType" varchar,"Content" varchar,"CreatedAt" timestamptz) LANGUAGE plpgsql AS $frms$
BEGIN
 RETURN QUERY SELECT n."NotificationLogId",n."UserAccountId",n."NotificationType",n."Content",n."CreatedAt" FROM public."NotificationLog" n WHERE n."Status"='PENDING' ORDER BY n."CreatedAt" ASC LIMIT 1 FOR UPDATE SKIP LOCKED;
END $frms$;
