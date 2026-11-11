# FRMS — Verified unregistered-login fixtures

**Branch:** TEST_SWT  
**Cases:** FRMS-ST-0199 (unknown customer phone) and FRMS-ST-0203 (unknown employee email)  
**Current state (2026-11-11):** `FRMS-ST-0199` and `FRMS-ST-0203` both **PASS according to the user's runtime confirmation**. Exact run IDs and report files were not provided; report-level validation is pending.

## What changed

The missing values `FRMS_TEST_UNREGISTERED_CUSTOMER_PHONE` and `FRMS_TEST_UNREGISTERED_EMPLOYEE_EMAIL` are not hardcoded. The sanitized PostgreSQL export does not contain customer phones or employee emails and cannot prove a chosen identifier is absent.

A dedicated Node preflight now runs within `run-frms-tests.ps1` after the API is healthy and before Playwright. It signs in as the configured Admin and verifies `/api/v1/auth/me` reports ACTIVE SYSTEM_ADMINISTRATOR. It scans **every** page of `GET /api/v1/admin/users?page=N&pageSize=100`, checks expected totals, page sequence, and uniqueness of account IDs, then verifies that candidate identifiers are syntactically valid and absent from the scanned directory. It never prints existing emails/phones or bearer tokens.

A verified value is exported **only into the child test process**. A missing/incomplete Admin directory, invalid credentials, bad pagination or registered override clears both keys; the cases are Blocked, not PASS. The only POST is normal Admin login and the negative-login attempts by Playwright; LoginHistory may change. No user account is created or modified.

## CMD commands

From the repository root, with an already-running authorized backend:

```bat
git status --short
git pull --ff-only origin TEST_SWT
curl http://127.0.0.1:5164/health

.\RUN_FRMS_TESTS.cmd -Mode case -CaseId FRMS-ST-0199 -Workers 1 -Retries 0 -SkipCatalogDiscovery
.\RUN_FRMS_TESTS.cmd -Mode case -CaseId FRMS-ST-0203 -Workers 1 -Retries 0 -SkipCatalogDiscovery
```

Expected preflight confirmation when the Admin API can verify all accounts:

```text
Verified negative login fixtures: phone and email absent from N complete Admin account records; no values logged.
... applicable fixture keys from 20 Batch 12 references; 0 missing ...
```

The number `N` is read from the current API, not assumed to equal a historical DB count. Preflight success alone is not testcase PASS; separately, the user confirmed both targeted Playwright testcases PASS on 2026-11-11. Their underlying reports have not been independently inspected.

## Interpretation of audit output

- `RUN_FRMS_TESTS.cmd -AuditOnly` performs **no backend or Admin requests**, so these two keys may still appear missing there. This is expected.
- Targeted `FRMS-ST-0065` does not require either negative-login fixture; the final fixture audit suppresses those unrelated keys.
- During `FRMS-ST-0199`, `FRMS-ST-0203`, `-Mode all`, or `-Mode area`, the Admin verifier runs first. If it fails, the output explains why and those cases remain Blocked.
- Evaluate the new `tests/system/reports/<run-id>/summary.md` and `results.json`. Do not report a Jira product defect from incomplete fixtures.

**Safety:** No `-StartBackend` unless backend job effects and shared Supabase scope are authorized. Do not commit credentials, Admin user directories or raw DB export files.

## Runtime results reported by user — 2026-11-11

| Case ID | Result | Evidence level | Run ID / report |
| --- | --- | --- | --- |
| `FRMS-ST-0199` | **PASS** | `USER_REPORTED_RUNTIME` | Not supplied |
| `FRMS-ST-0203` | **PASS** | `USER_REPORTED_RUNTIME` | Not supplied |

Do not invent run IDs or treat these two targeted successes as an independently checked report, a 53/53 aggregate, or a complete Batch 12 PASS. The `-AuditOnly` preflight can still list both fixture names as missing because it does not contact Admin API.
