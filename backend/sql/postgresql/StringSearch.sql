-- PostgreSQL17 cannot use LIKE on nondeterministic collations. Match literal
-- substrings using the unchanged ICU equality semantics, including equivalences
-- whose codepoint lengths differ. No LIKE wildcard or lowercasing substitution.
CREATE OR REPLACE FUNCTION public.frms_contains_ci_as(p_value text,p_fragment text)
RETURNS boolean LANGUAGE plpgsql IMMUTABLE PARALLEL SAFE
SET search_path = pg_catalog, public AS $frms$
DECLARE
 v_length integer;
 v_fragment_length integer;
 v_start integer;
 v_width integer;
BEGIN
 IF p_value IS NULL OR p_fragment IS NULL THEN RETURN false; END IF;
 IF '' COLLATE public.frms_ci_as = p_fragment COLLATE public.frms_ci_as THEN RETURN true; END IF;
 IF p_value COLLATE public.frms_ci_as = p_fragment COLLATE public.frms_ci_as THEN RETURN true; END IF;
 v_length := char_length(p_value);
 v_fragment_length := char_length(p_fragment);
 IF v_length=0 THEN RETURN false; END IF;
 -- The common equal-codepoint-length match takes one linear pass.
 FOR v_start IN 1..v_length LOOP
  IF substring(p_value FROM v_start FOR v_fragment_length) COLLATE public.frms_ci_as = p_fragment COLLATE public.frms_ci_as THEN RETURN true; END IF;
 END LOOP;
 -- ICU can equate different codepoint sequences (e.g. NFC/NFD and ligatures).
 -- Do not discard those matches by assuming a fixed fragment length.
 FOR v_start IN 1..v_length LOOP
  FOR v_width IN 1..(v_length-v_start+1) LOOP
   IF v_width<>v_fragment_length AND substring(p_value FROM v_start FOR v_width) COLLATE public.frms_ci_as = p_fragment COLLATE public.frms_ci_as THEN RETURN true; END IF;
  END LOOP;
 END LOOP;
 RETURN false;
END $frms$;
