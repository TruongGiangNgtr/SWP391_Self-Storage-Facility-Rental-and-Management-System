-- Pg020 catalog smoke check (read-only, never mutates business rows).
-- Run AFTER Pg020 on an approved PostgreSQL test database.
-- This verifies deployment wiring; it is NOT an E2E transaction test.
DO $check$
DECLARE
 v_oid oid;
 v_source text;
BEGIN
 SELECT to_regprocedure('public."usp_FinalizeReturn"(uuid,character varying,uuid)') INTO v_oid;
 IF v_oid IS NULL THEN RAISE EXCEPTION 'Missing public.usp_FinalizeReturn(uuid,varchar,uuid)'; END IF;
 SELECT pg_get_functiondef(v_oid) INTO v_source;
 IF strpos(v_source, 'SET "Status"=''CHECKED_OUT''') = 0
    OR strpos(v_source, 'target."VisitType"=''RETURN''') = 0
    OR strpos(v_source, 'target."Status"=''CHECKED_IN''') = 0
    OR strpos(v_source, 'target."ActualReturnDate" IS NOT NULL') = 0
    OR strpos(v_source, 'v_inspection."VisitId" IS NOT NULL') = 0 THEN
  RAISE EXCEPTION 'Pg020 RETURN Visit checkout guard is not deployed';
 END IF;
 RAISE NOTICE 'Pg020 catalog check PASS (not a runtime E2E pass)';
END $check$;
