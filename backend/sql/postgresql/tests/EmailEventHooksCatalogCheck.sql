-- Read-only catalog verification. Run AFTER Pg014 on disposable PostgreSQL DB.
-- Does not enable notifications, mutate rows or send emails.
DO $check$
DECLARE found_count integer;
BEGIN
    SELECT count(*) INTO found_count
      FROM pg_trigger t
      JOIN pg_class c ON c.oid=t.tgrelid
      JOIN pg_namespace n ON n.oid=c.relnamespace
      WHERE n.nspname='public' AND NOT t.tgisinternal
        AND t.tgname LIKE 'trg_Email_%';
    IF found_count <> 11 THEN
        RAISE EXCEPTION 'Expected 11 event triggers, got %', found_count;
    END IF;
    IF to_regprocedure('public.frms_email_events_enabled()') IS NULL
       OR to_regprocedure('public.frms_email_queue_customer_event(uuid,character varying,character varying)') IS NULL THEN
        RAISE EXCEPTION 'Pg014 event queue functions are missing';
    END IF;
    IF current_setting('frms.email_events_enabled', true) IS NOT NULL
       AND current_setting('frms.email_events_enabled', true) = 'on' THEN
        RAISE EXCEPTION 'Read-only verification must be run with event delivery gate disabled';
    END IF;
END $check$;
