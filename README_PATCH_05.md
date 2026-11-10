# Patch 05 — FRMS locator and catalogue diagnostics

Branch: TEST_SWT. Prerequisites: Patch 01–04 already applied.

Scope (test code only):
- FRMS-ST-0001: Scope the Registration Sign In link to the customer main region.
- FRMS-ST-0026: Target the exact Storage search region and keep this layout test independent of catalogue fixtures (FRMS-ST-0027 tests cards).
- FRMS-ST-0028 and FRMS-ST-0032: Target the exact month inputs instead of the calendar trigger.
- FRMS-ST-0035 and FRMS-ST-0036: Distinguish loaded/empty results from the Retry Search API error, waiting up to 15 seconds. This is **diagnostics**, not a fabricated fix for backend failures.

Apply from the repository root in CMD or PowerShell:
```text
git branch --show-current
git apply --check .\05_FRMS_RESULT_LOCATORS_AND_CATALOG_DIAGNOSTICS.patch
git apply .\05_FRMS_RESULT_LOCATORS_AND_CATALOG_DIAGNOSTICS.patch
pnpm run test:frms:coverage
pnpm run test:frms:case -- --id FRMS-ST-0001
pnpm run test:frms:case -- --id FRMS-ST-0026
pnpm run test:frms:case -- --id FRMS-ST-0035
pnpm run test:frms:case -- --id FRMS-ST-0036
```

If 0035/0036 report FRMS_CATALOG_QUERY_FAILED, inspect API logs and the Playwright error-context.md/trace; do not mark as a confirmed product defect without separate reproduction and approved oracle.

Blocked auth cases require FRMS_TEST_CUSTOMER_1_PASSWORD and FRMS_TEST_STAFF_PASSWORD for dedicated ACTIVE accounts; catalog 0027 needs complete FRMS_TEST_* catalogue fixtures.

This patch changes no backend or production data. No browser/runtime Pass is claimed.
