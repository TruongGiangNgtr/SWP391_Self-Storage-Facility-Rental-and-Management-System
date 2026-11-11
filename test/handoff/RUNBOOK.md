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
FRMS_TEST_ASSIGNABLE_RESERVATION_ID     CONFIGURED for read-only ST-0065; ACTIVE assignment exists, Manager runtime verification pending
FRMS_TEST_UNREGISTERED_CUSTOMER_PHONE  Auto-verified from full Admin directory just before FRMS-ST-0199; runtime pending
FRMS_TEST_UNREGISTERED_EMPLOYEE_EMAIL  Auto-verified from full Admin directory just before FRMS-ST-0203; runtime pending
FRMS_TEST_STAFF_HANDOVER_VISIT_ID     CHECKED_IN now; no RESERVATION/SCHEDULED Visit in snapshot for pre-check-in tests
```

Do **not** replace missing fixtures with unrelated UUIDs or invented unregistered identities. The new matching F01 Reservation is already linked to an ACTIVE HandoverUnitAssignment; only consider read-only matching assertions after Manager API and oracle review. The wrapper now accepts and provides this exact Reservation ID; actual current state is checked by the read-only testcase. No assignment mutation is allowed. If a newly authorized, isolated test DB is provisioned, build a deterministic fixture factory and validate same Facility + UnitType + Visit status, then rerun case and record evidence.

The active discovery code may still use Staff F02 by default even though the fixture uses Staff F32. An F32 dedicated patch exists outside the canonical script; **check installed source before reapplying**. `FRMS_TEST_OTHER_FACILITY_REFERENCE_ID` is a Visit ID with reference date `2026-12-05`.

## 7. Reports and Jira

Look in `tests/system/reports/<run-id>/`:

- `summary.md`: pass/fail/blocked and registry coverage.
- `results.json`: per-case outcome and evidence.
- `playwright-results.json`: runner details, if generated.
- `jira_defects.csv`: only independently confirmed defects (often header-only).

Exit code `3` may indicate unimplemented/review/blocked registry entries; read the report instead of assuming a test assertion failed. Do not treat runtime timeout or missing infrastructure as a product defect without independent reproduction.

## 8. Read-only PostgreSQL profile refresh

The repository includes `test/tools/FRMS_DB_PROFILE_EXPORT/` (Python + psycopg). The current `RUN_EXPORT.cmd` creates/reuses an isolated venv at `%TEMP%\frms-db-profile-export-venv` and installs `psycopg[binary]` on first use, avoiding the broken system Python 3.14 pip. If venv creation or package download fails, the script exits without attempting a database export; see the exporter README.

Never push `.env`, database connections, raw dumps, emails/phones, credentials, JWTs or exported snapshot ZIPs. Sanitized snapshots still contain business IDs and should be shared only with authorized reviewers.

**DB profile exporter report location (from `RUN_EXPORT.cmd`):** `test/reports/db-profile/` (inside this repository, ignored by `test/reports/.gitignore`). The exporter generates `FRMS_PROFILE_SAFE_<UTC>.zip` and `FRMS_PROFILE_SUMMARY_<UTC>.json` there. Invocation: `.\test\tools\FRMS_DB_PROFILE_EXPORT\RUN_EXPORT.cmd`. To override the location, append `--out "D:\safe-output"`. This directory is **not** `tests/system/reports/`, which stores Playwright test evidence. If invoking Python directly, include `--out ".\test\reports\db-profile"`; otherwise Python still defaults to the home directory.

## 9. FRMS-ST-0065 prepared case

```bat
.\RUN_FRMS_TESTS.cmd -AuditOnly
.\RUN_FRMS_TESTS.cmd -Mode case -CaseId FRMS-ST-0065 -Workers 1 -Retries 0 -SkipCatalogDiscovery
```

The wrapper supplies F01 Facility `f0000000-0000-0000-0000-000000000001`, Reservation `f1112fee-e99f-4094-879a-563ddd1f8f81`, physical Unit `fa72ca32-22d6-496c-aca2-20e64c040de7`, Manager F01 demo identity. The test checks GET reservation/visit/unit/active assignment and then the Manager page, without assignment writes. When preconditions changed, `FRMS_FIXTURE_MISSING` is normalized as Blocked. For detailed API/data evidence, see [FRMS-ST-0065.md](FRMS-ST-0065.md).

### Negative login fixture audit (added 2026-11-11)

Use `.\RUN_FRMS_TESTS.cmd -Mode case -CaseId FRMS-ST-0199 -Workers 1 -Retries 0 -SkipCatalogDiscovery`, then run the same command with `FRMS-ST-0203`. The launcher verifies Admin `/auth/me`, retrieves **every** `/admin/users` page (pageSize=100), rejects incomplete/duplicate pagination and only then injects absent phone/email values into the Playwright child process. `-AuditOnly` is static and does not perform this authenticated verification. On a non-negative targeted case such as ST-0065, these two unrelated variables are excluded from the final missing-fixture list. No account creation or direct database changes. See [NEGATIVE_LOGIN_FIXTURES.md](NEGATIVE_LOGIN_FIXTURES.md).

## Batch 13 (2026-11-11): 50.18% implementation coverage

Manifest now contains **138 IMPLEMENTED / 129 NOT_IMPLEMENTED / 8 REVIEW (275 total)**, after 37 new uniquely mapped Playwright cases. See `test/handoff/BATCH13_COVERAGE.md`. All new cases are **PENDING_RUNTIME**, with SRS oracle review pending. No new PASS, Jira defect, app edit or DB mutation is claimed. Access Visit creation ST-0077 and ST-0080 are isolated-write gated and cannot run on shared Supabase under normal wrapper defaults. Foreign physical Unit cases require a positively verified real foreign Facility Manager.
