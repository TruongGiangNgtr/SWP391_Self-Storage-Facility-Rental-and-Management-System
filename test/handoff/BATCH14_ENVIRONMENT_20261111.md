# FRMS Batch 14 — Environment and secrets preflight

**Date:** 2026-11-11. **Reference:** repository `profile.md`, `test/handoff/fixtures.json`, `RUN_FRMS_TESTS.ps1`, `tests/system/scripts/batch12-env-audit.mjs`.

## Required process environment for Batch 14

| Key | Expected source or state | Cases |
| --- | --- | --- |
| `FRMS_ENVIRONMENT` | `Testing` (process local, verified by launcher) | all |
| `FRMS_API_URL` | `http://127.0.0.1:5164` | all |
| `FRMS_UI_URL` | `http://127.0.0.1:4173` | all UI |
| `VITE_API_PROXY_TARGET` | same Testing API | browser network |
| `FRMS_TEST_DB_ISOLATED` | `false` (shared Supabase is **NOT** isolated) | all |
| `FRMS_ALLOW_STATE_CHANGES` | `NO` (must remain disabled) | all |
| `FRMS_ALLOW_AI_CALLS` | not `YES`; AI not used | all |
| `FRMS_TEST_CUSTOMER_1_PHONE` and `FRMS_TEST_CUSTOMER_1_PASSWORD` | existing Customer 1 phone/fallback and configured password | billing, return, renewal, support |
| `FRMS_TEST_CUSTOMER_2_PHONE` and `FRMS_TEST_CUSTOMER_2_PASSWORD` | existing explicit Customer 2 fixture (do not invent identity) | billing, return, support |
| `FRMS_TEST_STAFF_01_EMAIL` and `FRMS_TEST_STAFF_PASSWORD` | existing verified F01 Staff identity / password | pending inspection, handover |
| `FRMS_TEST_MANAGER_01_EMAIL` and `FRMS_TEST_MANAGER_PASSWORD` | existing explicitly verified F01 Facility Manager account | handover assignment precondition |
| `FRMS_TEST_ADMIN_EMAIL` and `FRMS_TEST_ADMIN_PASSWORD` | existing verified System Administrator account | Facility selection |
| `FRMS_TEST_ACTIVE_CONTRACT_ID` | current owner-visible ACTIVE Contract | renewal preview |
| `FRMS_TEST_HANOI_FACILITY_ID` | current Facility F01 ID from snapshot; verify live actor scope | handover |
| `FRMS_TEST_STAFF_HANDOVER_RESERVATION_ID` / `FRMS_TEST_STAFF_HANDOVER_VISIT_ID` | **already configured**; require CONFIRMED / CHECKED_IN for `0061` | handover |

**No new mandatory variables** are introduced by Batch 14. Each test obtains its other IDs/statuses through authenticated read-only API requests; absence of the required live state yields a **Blocked/Skipped** result. This does not establish that every pre-existing fallback/credential is still valid: the runner's per-actor `/auth/me` checks remain mandatory.

Do not copy database passwords, connection strings, `service_role` keys or JWTs to `profile.md`, `context.json`, GitHub, logs or Jira. The high-privilege PostgreSQL password disclosed earlier should be rotated outside this testing patch; do not use it in a new fixture installer.

## Existing fixture keys for four deferred cases

- `0067`: `FRMS_TEST_STAFF_HANDOVER_RESERVATION_ID`, `FRMS_TEST_STAFF_HANDOVER_VISIT_ID` already exist but the snapshot Visit is `CHECKED_IN`, **not** `SCHEDULED`. A fresh valid state pair (if one exists) must be verified via authorized Staff GET before setting process-local overrides. Do not create it on shared DB.
- `0071`: existing F01 Manager and Staff identities suffice to search real `CONFIRMED` + `CHECKED_IN` Reservations and verify lack of ACTIVE assignment; no extra secret should be needed if a candidate exists.
- `0109` and `0188`: `FRMS_TEST_STAFF_IN_PROGRESS_INSPECTION_ID` already references the snapshot `IN_PROGRESS` Inspection, but `EmployeeId` ownership against the logged-in F01 Staff is not proven. If the actual owner is another Staff, credentials for that **existing authorized actor** may be needed for a future separate fixture-controlled test; do not ask for passwords in chat or reassign the Inspection.

## Read-only environment commands

```bat
git branch --show-current
git status --short
.\RUN_FRMS_TESTS.cmd -AuditOnly
.\RUN_FRMS_TESTS.cmd -Mode coverage
curl.exe http://127.0.0.1:5164/health
```

Do not interpret `-AuditOnly` as actual credential or DB validation. Preflight and the targeted Playwright runs will verify roles and current state. Do **not** start a new backend instance with `-StartBackend` connected to shared Supabase: background jobs can mutate records.
