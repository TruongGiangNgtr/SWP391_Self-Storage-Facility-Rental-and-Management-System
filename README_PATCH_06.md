# FRMS Patch 06 — Catalogue API diagnostics (TEST_SWT)

Prerequisite: Patches 01–05 already applied. This patch does not change backend, customer data, or database schema.

## Scope

- Add read-only `test:frms:catalog-probe` to query the same three date windows used by `FRMS-ST-0032`, `FRMS-ST-0035`, and `FRMS-ST-0036` against the localhost Testing API. The probe reports only status, elapsed milliseconds, collection count, and error code; no secrets or response bodies.
- Add Playwright network diagnostics to those three cases: timeouts now distinguish unresolved backend/API calls (`FRMS_INFRA_UNAVAILABLE`) from a frontend that never rendered a result despite completed requests (`FRMS_CATALOG_RESULTS_TIMEOUT`).
- Increase the catalogue wait to 45 seconds, with a 90-second test deadline; the original assertions for actual results/empty/error are retained. No fake Pass or automatic Jira defects.
- The remaining five blocked login cases still require actual Testing account passwords; catalogue cases `0027` and `0028` still require known catalog fixture values.

## Apply in CMD, from repository root

```cmd
git branch --show-current
git apply --check .\06_FRMS_CATALOG_API_DIAGNOSTICS.patch
git apply .\06_FRMS_CATALOG_API_DIAGNOSTICS.patch
pnpm run test:frms:coverage
```

## Run diagnosis, from CMD

Keep the Testing backend running, and set the API endpoint in the same terminal:

```cmd
set "FRMS_ENVIRONMENT=Testing"
set "FRMS_API_URL=http://127.0.0.1:5164"
set "FRMS_UI_URL=http://127.0.0.1:4173"
set "VITE_API_PROXY_TARGET=http://127.0.0.1:5164"
set "CI=true"
pnpm run test:frms:catalog-probe
pnpm run test:frms:case -- --id FRMS-ST-0032
pnpm run test:frms:case -- --id FRMS-ST-0035
pnpm run test:frms:case -- --id FRMS-ST-0036
```

The diagnostic JSON file is stored at `tests/system/reports/catalog-probe-<timestamp>.json`. Individual Playwright executions create their own `tests/system/reports/<run-id>/` directories.

**How to interpret failures:**

- API direct probe times out/HTTP 5xx: backend or its database is suspect; inspect backend logs. Not independently proven a product defect.
- API direct probe returns 200, but Playwright says `FRMS_CATALOG_RESULTS_TIMEOUT`: investigate frontend fetch, proxies, rendering, or response parsing. Attach the original run's `error-context.md` and trace.
- API direct probe succeeds and the Playwright test passes: previous timeout was transient or a slow dependency; do not call it a proven fix.
- Catalog login and price fixtures have not been created by this patch; the seven blocked tests remain intentionally blocked until valid isolated fixtures are provided.

Only run against a database approved for testing. A healthy `/health` does not prove the database is isolated or safe for mutation.
