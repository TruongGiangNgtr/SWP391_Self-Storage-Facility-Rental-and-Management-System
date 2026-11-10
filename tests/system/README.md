# FRMS system automation — staged implementation

Source basis: `main` commit `c89b417c50a985bad29cc82f7d4b46f3e9c6d549`; applied to existing `TEST_SWT` commit `45e94710477f9a9085ca22dbc69523c914de4d62`; testcase file `FRMS_Testcase_Final(2).md`.

The registry contains **275 unique cases across 12 areas**. This patch series provides **30 executable Playwright cases** (19 authentication/registration, 8 catalogue/month validation, 3 security/health API). It does **not** contain test implementations, verified fixtures or approved oracles for the remaining cases. There are 8 known test design gaps (REVIEW) and 237 outstanding cases. A registry entry alone does not execute a case.

## Safety requirements

- Use a dedicated local **Testing** backend and PostgreSQL test database; never run against a live/shared environment. The preflight refuses non-loopback API/UI URLs and non-Testing configuration.
- Before any authenticated test, seed and verify an ACTIVE customer and an ACTIVE Facility Staff; the runner does not create or mutate accounts. Login identifiers default to Customer 1 phone `0901000001` and Staff 01 email `staff.f01@example.com`. Secrets are read from environment, not source. Authenticated tests are marked Blocked if required password variables are absent. Catalog card tests also require `FRMS_TEST_ACTIVE_FACILITY_NAME`, `FRMS_TEST_AVAILABLE_UNIT_NAME`, `FRMS_TEST_AVAILABLE_UNIT_SIZE`, `FRMS_TEST_AVAILABLE_UNIT_MODE`, and `FRMS_TEST_AVAILABLE_UNIT_RENT_VND` for an actually available unit in the selected next-month period.
- Browser run is headless; frontend starts using the existing Playwright `webServer` on port 4173. Start the test API on port 5164 yourself. The webServer's `reuseExistingServer` is disabled when `CI=true`.
- API security tests use independent HTTP requests and check status, error-code and trace-id; they do not treat HTTP status as frontend output.
- Code-derived assertions do not imply an approved SRS oracle. Every case records `oracleApproval: PENDING_SRS_REVIEW`; automated failures are candidates only. There is no automatic Jira defect creation by merely failing an assertion.

## Usage — PowerShell from repository root

```powershell
pnpm install --frozen-lockfile
pnpm --dir tests/e2e/playwright run install:browsers

# Start backend and dedicated PostgreSQL Testing environment separately.
$env:FRMS_ENVIRONMENT = 'Testing'
$env:FRMS_API_URL = 'http://127.0.0.1:5164'
$env:FRMS_UI_URL = 'http://127.0.0.1:4173'
# Set these securely for ACTIVE seeded fixture accounts:
# $env:FRMS_TEST_CUSTOMER_1_PASSWORD = <from secret manager>
# $env:FRMS_TEST_STAFF_PASSWORD = <from secret manager>

pnpm run test:frms:coverage
pnpm run test:frms:preflight
pnpm run test:frms:case -- --id FRMS-ST-0002
pnpm run test:frms:case -- --area 'Authentication & Security'
pnpm run test:frms:all
```

For no existing authorized users, start with registration negatives and API-only tests; other cases remain Blocked rather than Pass. Some runners exit nonzero until all 275 are implemented, approved and executed. A full-run exit code 3 with Not Run/Review cases is expected at this stage.

Reports are at `tests/system/reports/<run-id>/{results.json,summary.md,report.html,jira_defects.csv,artifacts/}`. The CSV has headers only unless **independent confirmation** attaches the exact required `confirmation` evidence to a `ConfirmedProductDefect` row. The current runner does not infer confirmation from retries, since persistent script/fixture errors can mimic defects.

## Next batches

1. Implement isolated data factory and fixtures for registration and existing users, role/status and data-dependent public catalog assertions.
2. Reservation capacity, concurrency, deposit payment with signed callbacks, and handover with Playwright + NUnit/integration DB invariants.
3. Visits, billing, renewal, return, support tickets, administration, pricing and reference data.
4. Run all 275, approve oracle traceability, CI orchestration, normalise NUnit/Postman evidence, confirm defects and only then import Jira CSV.

Do not mark `NOT_IMPLEMENTED` cases as skipped Playwright tests or manufacture a pass. The manifest deliberately retains every unimplemented case as `Not Run` in the report.
