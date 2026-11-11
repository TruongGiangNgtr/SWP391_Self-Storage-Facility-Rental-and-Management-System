# FRMS — Self-contained AI Handoff

**Project:** SWP391 Self-Storage Facility Rental and Management System  
**Repository:** `TruongGiangNgtr/SWP391_Self-Storage-Facility-Rental-and-Management-System`  
**Target branch:** `TEST_SWT` (do not assume `swt_test` exists)  
**Date:** 2026-11-11, Asia/Ho_Chi_Minh  
**Task:** maintain automated FRMS system testing and hand over state safely.

## 1. Mandatory context

The project has React/Vite frontend, ASP.NET Core/.NET 10 backend and PostgreSQL/Npgsql. Test automation uses Playwright with a 275-case registry, Node scripts and Windows PowerShell/CMD launchers. UI-observable expected results should match the running frontend/backend implementation; do not substitute error codes for visible UI outputs unless the case targets an API.

The live-ish previously used PostgreSQL is **remote/shared Supabase**. No dedicated isolated Testing DB is confirmed. Testing environment variables do **not** prove isolation. Never write a reservation, unit assignment, payment, billing, return, user state or schema mutation just to obtain fixture data. Login may add LoginHistory rows. Backend startup can trigger Reservation Expiration regardless of disabled Billing/NoShow/Notifications switches.

## 2. Current testcase coverage

| Area | Registry | Implemented | Not implemented | Review |
| --- | ---: | ---: | ---: | ---: |
| Authentication & Security | 51 | 39 | 12 | 0 |
| Storage Search & AI Size Guide | 19 | 15 | 4 | 0 |
| Reservation | 22 | 8 | 14 | 0 |
| Handover | 29 | 5 | 24 | 0 |
| Facility Operations | 10 | 2 | 8 | 0 |
| Access Visits | 25 | 8 | 14 | 3 |
| Billing & Payments | 23 | 5 | 18 | 0 |
| Renewal | 16 | 3 | 13 | 0 |
| Return & Settlement | 22 | 2 | 20 | 0 |
| Support Tickets | 25 | 4 | 16 | 5 |
| Administration | 19 | 5 | 14 | 0 |
| Pricing & Reference Data | 14 | 5 | 9 | 0 |
| **TOTAL** | **275** | **101** | **166** | **8** |

Manifest path: `tests/system/manifest/cases.json`. Automated coverage is **36.73%**.

Runtime history: user reported **51/51 PASS** in run `20261110T151721934Z`. Batch 12 added 50 mapped tests (101 implemented total). The newer 50 tests have **no whole-batch confirmed PASS evidence**. An implemented mapping is never treated as PASS by default. All registered oracles are marked pending SRS review in the manifest.

The registry distinguishes Pass, Blocked, Review, Not Run, UnconfirmedFailure. Only independently reproduced and requirements-aligned product defects may be exported/imported to Jira. A missing fixture is not a software defect.

## 3. Exact execution environment

Preferred Windows repository root:

```text
C:\Users\buidi\OneDrive\Desktop\Git\SWP391\SWP391_Self-Storage-Facility-Rental-and-Management-System
```

API `http://127.0.0.1:5164`; web UI `http://127.0.0.1:4173`; `FRMS_ENVIRONMENT=Testing`; `VITE_API_PROXY_TARGET` points to API; `CI=true`. Default Playwright 4 workers and 1 retry.

Use `RUN_FRMS_TESTS.cmd` (PowerShell wrapper `RUN_FRMS_TESTS.ps1`). Wrapper invokes `run-frms-tests.ps1`. By default **do not** use `-StartBackend`; run only with a separately authorized already-running API. `-AuditOnly` checks process keys, not live fixture correctness. Full wrapper command:
```bat
.\RUN_FRMS_TESTS.cmd -Mode all -Workers 4 -Retries 1
```
Single testcase:
```bat
.\RUN_FRMS_TESTS.cmd -Mode case -CaseId FRMS-ST-0165 -Workers 1 -Retries 0
```
Full operational commands are in [RUNBOOK.md](RUNBOOK.md).

Demo identities: Customer 1/2/3 use phone numbers `0901000001/2/3`. Staff F01 `staff.f01@example.com`; Staff F32 `staff.f32@example.com`; Manager F01 `manager.f01@example.com`; BOM `bom@example.com`; Admin `admin@example.com` use emails. Credential values are not included in this handoff. Actual ACTIVE role and facility permission must be checked.

## 4. Fixture status — key handoff facts

The user supplied a *sanitized, read-only* PostgreSQL snapshot from `2026-11-11T04:40:10Z`. It supports **candidate** IDs, not permanent data availability. The committed wrapper has 17 candidate fixture values and additional F32 date/terminated-contract metadata; machine-readable names and values are in [fixtures.json](fixtures.json).

**Blocked:** `FRMS_TEST_ASSIGNABLE_RESERVATION_ID` for `FRMS-ST-0065`. At Facility 01, Reservation `b370c765-adb7-4e16-aa19-54caf7d5b788` is `CONFIRMED` and its Visit is `CHECKED_IN`, but UnitType `b1000000-0000-0000-0000-000000000002` has 2 `IN_USE`, 1 `INSPECTION`, **0 AVAILABLE** physical units. Reservation `f1112fee-e99f-4094-879a-563ddd1f8f81` has an available matching type but Visit still `SCHEDULED`. Neither is a valid assignable ID. Do not force an ID or alter remote DB to make the case pass.

**Snapshot corroborated but runtime authorization pending:** `FRMS_TEST_OTHER_FACILITY_REFERENCE_ID=33a922ce-416f-4757-87f5-8f85aba047ef` is a **Visit ID**, not Facility ID. It belongs to Reservation `bc9b2c4c-9d74-4f16-b0d6-73156a42fe85` at Facility 32, Visit date `2026-12-05`, status `CHECKED_IN`. Confirm Staff F32 can read it and Staff F01 cannot. `FRMS-ST-0165` checks isolation, not mere UUID format.

**Unverified negative identifiers:** `FRMS_TEST_UNREGISTERED_CUSTOMER_PHONE`, `FRMS_TEST_UNREGISTERED_EMPLOYEE_EMAIL` require full-page enumeration of Admin user directory with actual phone/email data. The sanitized database export excludes these fields. They should remain missing if the Admin verification cannot complete.

**Other unresolved preconditions:** `FRMS_TEST_NO_CCCD_PHONE/PASSWORD`, `FRMS_TEST_COMPLETED_CONTRACT_ID` were not established from the snapshot. Read-only fixture discovery may be incomplete.

## 5. Critical implementation discrepancies to check before further tests

The committed `tests/system/scripts/batch12-discover.mjs` is **not** the latest patch-folder version. It defaults cross-facility discovery to Staff F02 (`FRMS_TEST_STAFF_02_EMAIL`) while the wrapper exposes `FRMS_TEST_FOREIGN_STAFF_EMAIL=staff.f32@example.com`. Its cross-facility scan looks for a same-day own/foreign work-item pair. The external F32 dedicated patch is not automatically applied by merely having patch folders tracked.

`batch12-verify-unregistered.mjs` and a root `install.mjs` exist, but the current committed `RUN_FRMS_TESTS.ps1` does **not** include the special negative-fixture pre-audit hook. The previous user output still had 3 missing fixture names. Do not claim the preflight patch is active without checking actual installed code.

The current `tests/e2e/playwright/tests/frms/batch12-staff.spec.ts` is older than the `FRMS_BATCH12_FINAL2_FIX/batch12-staff.spec.ts` patch candidate. Review differences and acceptance criteria before editing. Avoid mixing installer versions blindly.

Root `profile.md` on the tracked branch predates the 2026-11-11 full snapshot and has stale 30-case status in sections; use this folder and `tests/system/README.md` for current counts, not those old profile sections.

## 6. Repo links and file ownership

- Product docs: `README.md`, `AGENTS.md`, `docs/FRMS_SRS_V11.md`, `docs/FRMS_API_V11.md`, `docs/FRMS_Data_Dictionary_V11.md`
- Test registry: `tests/system/manifest/cases.json`
- Test specifications: `tests/e2e/playwright/tests/frms/*.spec.ts`
- Automation manual: `tests/system/README.md`
- Patch history: `tests/system/PATCH_HISTORY.md`
- Runner: `RUN_FRMS_TESTS.cmd`, `RUN_FRMS_TESTS.ps1`, `run-frms-tests.ps1`
- Utilities: `tests/system/scripts/{batch12-env-audit,batch12-discover,validate-coverage,normalize-results,export-jira-defects}.mjs`
- Optional profile exporter: `FRMS_DB_PROFILE_EXPORT/`; do not commit safe-data ZIPs.
- Reports: `tests/system/reports/<run-id>/{summary.md,results.json,jira_defects.csv}`
- Handoff: `test/handoff/` only; the actual tests remain in `tests/`.

## 7. Work protocol for next AI

1. Read this handoff, `context.json`, `fixtures.json`, `tests/system/README.md`, and actual source for the requested case.
2. Confirm current Git branch and worktree status. Do not discard local uncommitted work.
3. Classify evidence as `REPO_VERIFIED`, `SNAPSHOT_VERIFIED`, `USER_REPORTED_RUNTIME`, or `NOT_YET_VERIFIED`.
4. For test changes, derive Expected Output from source/UI behavior; do not invent labels, response codes or a PASS.
5. Favor fully read-only inspection. Require explicit isolated DB before proposing state mutations.
6. Make related changes as **one Git commit per request**, with no writes to `main`.
7. Do not say the full suite passed until an actual new `summary.md` proves it. If a case cannot run, mark Blocked and explain exact missing precondition.

## 8. Next priorities

See [NEXT_STEPS.md](NEXT_STEPS.md). Most immediate: resolve negative fixture verification safely, reconcile F32 cross-facility discovery/STAFF test versions, validate 101 testcase discovery and run evidence; leave the assignment case blocked until genuine test data become eligible.

## 9. PowerShell cleanup decision (2026-11-11)

The obsolete `APPLY_FRMS_PATCHES_TEST_SWT.ps1` was removed because its required old patch files are absent from `TEST_SWT` and patches 01–03 are already integrated. The supported command remains `.\\RUN_FRMS_TESTS.cmd`.

Keep **both** `RUN_FRMS_TESTS.ps1` (safe environment/fixture wrapper) and `run-frms-tests.ps1` (Playwright/system-test launcher): the CMD entrypoint depends on both. Also keep `backend/scripts/PostgreSql-Local.ps1` for local PostgreSQL lifecycle. The older `backend/tests/Frms.IntegrationTests/Verify-CreateReservation.ps1` targets legacy SQL Server with write operations and hardcoded paths; it is unrelated to the current automated PostgreSQL suite, retained for historical continuity only. Do not execute it on shared Supabase. This cleanup did not execute tests or validate new PASS results.
