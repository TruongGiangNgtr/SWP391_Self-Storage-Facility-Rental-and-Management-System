# FRMS — Windows testing runbook

**Target:** `TEST_SWT`, repository root. Commands below use **CMD**, not PowerShell syntax. Actual tests live in `tests/system` and `tests/e2e/playwright`, not `test/handoff`.

## 1. Pre-run Git and safety checks

```bat
cd /d "C:\Users\buidi\OneDrive\Desktop\Git\SWP391\SWP391_Self-Storage-Facility-Rental-and-Management-System"
git branch --show-current
git status --short
git fetch origin TEST_SWT
```

Expected branch: `TEST_SWT`. Do not do a blind `git pull` if branch history has been rewritten or there are uncommitted changes. The previous README cleanup was squash-rewritten; inspect upstream/local commits first.

Verify backend was started by an authorized owner with acceptable DB/job configuration. The wrapper defaults to `-SkipBackend`; `ASPNETCORE_ENVIRONMENT=Testing` does **not** isolate a remote DB.

```bat
curl http://127.0.0.1:5164/health
```

Expected body: `Healthy`. Health does not prove that a safe DB or valid fixture is available.

## 2. Dependencies

```bat
pnpm install --frozen-lockfile
pnpm --dir tests/e2e/playwright exec playwright install chromium
```

Node, pnpm, Git, .NET SDK and Playwright test dependencies must already be available.

## 3. Audit and coverage

```bat
.\RUN_FRMS_TESTS.cmd -AuditOnly
.\RUN_FRMS_TESTS.cmd -Mode coverage
.\RUN_FRMS_TESTS.cmd -Mode preflight
```

**Important:** `-AuditOnly` runs before live fixture discovery, so missing keys at this stage are a configuration inventory, not proof the test will be blocked in an actual run.

## 4. Run tests

```bat
rem All implemented mappings (not all 275 have executable tests)
.\RUN_FRMS_TESTS.cmd -Mode all -Workers 4 -Retries 1

rem Exact case
.\RUN_FRMS_TESTS.cmd -Mode case -CaseId FRMS-ST-0165 -Workers 1 -Retries 0

rem Authentication and Security functional area
.\RUN_FRMS_TESTS.cmd -Mode area -Area "Authentication & Security" -Workers 2 -Retries 0

rem Optional: rely on live discovery instead of 2026-11-11 snapshot default values
.\RUN_FRMS_TESTS.cmd -Mode all -LiveDiscoveryOnly -Workers 1 -Retries 0
```

Only use `-StartBackend` after reviewing the *actual* configured PostgreSQL and Reservation Expiration job; the safe default does not start it.

## 5. Expected process environment

| Variable | Expected baseline |
| --- | --- |
| `FRMS_ENVIRONMENT` | `Testing` |
| `ASPNETCORE_ENVIRONMENT`, `DOTNET_ENVIRONMENT` | `Testing` inside wrapper process |
| `FRMS_API_URL` | `http://127.0.0.1:5164` |
| `FRMS_UI_URL` | `http://127.0.0.1:4173` |
| `VITE_API_PROXY_TARGET` | `http://127.0.0.1:5164` |
| `CI` | `true` |
| `FRMS_TEST_DB_ISOLATED` | `false` |
| `FRMS_ALLOW_STATE_CHANGES` | `NO` |
| `FRMS_ALLOW_AI_CALLS` | `NO` |
| `FRMS_PLAYWRIGHT_WORKERS` | `4` unless overridden |
| `FRMS_PLAYWRIGHT_RETRIES` | `1` unless overridden |

The wrapper defaults to demo identifiers from the existing source; actual secrets belong in local environment/ignored `backend/Frms.Api/.env`, not in this handoff. The active backend must have its **own** safe environment; changing variables in the test wrapper does not reconfigure an already-running backend.

## 6. Missing fixtures and what to do

```text
FRMS_TEST_ASSIGNABLE_RESERVATION_ID     BLOCKED by snapshot business data (FRMS-ST-0065)
FRMS_TEST_UNREGISTERED_CUSTOMER_PHONE  Must verify absent via complete Admin directory (FRMS-ST-0199)
FRMS_TEST_UNREGISTERED_EMPLOYEE_EMAIL  Must verify absent via complete Admin directory (FRMS-ST-0203)
```

Do **not** replace missing fixtures with unrelated UUIDs or invented unregistered identities. If a newly authorized, isolated test DB is provisioned, build a deterministic fixture factory and validate same Facility + UnitType + Visit status, then rerun case and record evidence.

The active discovery code may still use Staff F02 by default even though the fixture uses Staff F32. An F32 dedicated patch exists outside the canonical script; **check installed source before reapplying**. `FRMS_TEST_OTHER_FACILITY_REFERENCE_ID` is a Visit ID with reference date `2026-12-05`.

## 7. Reports and Jira

Look in `tests/system/reports/<run-id>/`:

- `summary.md`: pass/fail/blocked and registry coverage.
- `results.json`: per-case outcome and evidence.
- `playwright-results.json`: runner details, if generated.
- `jira_defects.csv`: only independently confirmed defects (often header-only).

Exit code `3` may indicate unimplemented/review/blocked registry entries; read the report instead of assuming a test assertion failed. Do not treat runtime timeout or missing infrastructure as a product defect without independent reproduction.

## 8. Read-only PostgreSQL profile refresh

The repository includes `FRMS_DB_PROFILE_EXPORT/` (Python + psycopg). The **tracked** `RUN_EXPORT.cmd` may be the older version that requires functioning system pip. On Windows Python 3.14 with broken pip, use a dedicated virtual environment if needed; consult the exporter README and actual script before execution.

Never push `.env`, database connections, raw dumps, emails/phones, credentials, JWTs or exported snapshot ZIPs. Sanitized snapshots still contain business IDs and should be shared only with authorized reviewers.
