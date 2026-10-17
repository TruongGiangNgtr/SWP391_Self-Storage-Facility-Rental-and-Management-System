-- Operational repair for an existing payOS channel; not a bootstrap migration.
-- The caller must verify @NextOrderCode is unused on the configured channel,
-- use an explicit transaction and hold SHARE ROW EXCLUSIVE on public."Payment".
-- Only advance: retain every historical Payment and never move allocation back.
-- setval is not rolled back by PostgreSQL; do not lower it after a failed run.
SELECT pg_catalog.setval('public."ProviderOrderCodeSequence"'::regclass, @NextOrderCode, false)
WHERE @NextOrderCode > (SELECT last_value FROM public."ProviderOrderCodeSequence")
  AND @NextOrderCode > COALESCE((SELECT max("ProviderOrderCode") FROM public."Payment"), 0);
