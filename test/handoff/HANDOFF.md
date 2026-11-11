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

## 4. Fixture status — new 2026-11-11 06:30:28 UTC snapshot

The user uploaded **`FRMS_PROFILE_SAFE_20261111T063028Z.zip`** and **`FRMS_PROFILE_SUMMARY_20261111T063028Z.json`** (internal, not in Git). Manifest and candidate data match: 29 public tables inventoried, 28 exported, AuditLog skipped, **1,552 metadata rows**. Snapshot states include 12 Reservations (3 CONFIRMED), 21 Visits (6 CHECKED_IN, 4 SCHEDULED, **0 RESERVATION/SCHEDULED**), 386 StorageUnits (378 AVAILABLE), 8 Handover Assignments (2 ACTIVE).

**Manager F01 matching candidate found:** Reservation `f1112fee-e99f-4094-879a-563ddd1f8f81` CONFIRMED, Visit `6855fba5-942c-40a4-bf22-5658ca3be00c` RESERVATION/CHECKED_IN, physical Unit `fa72ca32-22d6-496c-aca2-20e64c040de7` AVAILABLE and matching UnitType ...0001. However, an **existing ACTIVE** assignment `d9e3672e-9708-46de-96a9-72a860f55065` already binds these exact records. It is a **snapshot-confirmed matching tuple**, not proven eligible for *new* assignment. `FRMS-ST-0065` currently makes only read-only matching/UI checks; authenticate Manager F01 and approve the testcase oracle before using the candidate. Never send assignment save/release as a diagnostic.

**Launcher/test fix applied:** `RUN_FRMS_TESTS.ps1` now supplies `FRMS_TEST_ASSIGNABLE_RESERVATION_ID=f1112fee-e99f-4094-879a-563ddd1f8f81` by default and no longer rejects it. `FRMS-ST-0065` now validates Reservation, Visit, Unit, and the existing ACTIVE HandoverUnitAssignment using only GET requests, then checks the Manager page. **A targeted 1/1 PASS console log is now supplied; local code revision and report artifacts remain unverified.** See [FRMS-ST-0065.md](FRMS-ST-0065.md).

**Scheduled-Visit fixture is now invalid:** `FRMS_TEST_STAFF_HANDOVER_VISIT_ID=6855fba5-942c-40a4-bf22-5658ca3be00c` is now CHECKED_IN, **not SCHEDULED**. No RESERVATION/SCHEDULED Visits exist among the 21 snapshot Visits. Keep any before-check-in dependent scenario Blocked until a real eligible Visit exists.

**Foreign F32 fixture unchanged:** `FRMS_TEST_OTHER_FACILITY_REFERENCE_ID=33a922ce-416f-4757-87f5-8f85aba047ef` is a Reservation Visit at Facility 32 on 2026-12-05, CHECKED_IN. F32 authorized/F01 denied API results require runtime checks; comparing only F01's 2026-12-01 work list is not sufficient.

**Targeted negative-login results (2026-11-11): `FRMS-ST-0199` PASS and `FRMS-ST-0203` PASS, as directly reported by the user.** This is `USER_REPORTED_RUNTIME`, not a report-level verification; neither individual run ID nor `summary.md`/`results.json` was supplied. The automated unregistered-identifier preflight remains in place. **Unregistered identifier policy:** `FRMS_TEST_UNREGISTERED_CUSTOMER_PHONE` and `FRMS_TEST_UNREGISTERED_EMPLOYEE_EMAIL` are withheld from the sanitized export. The new verifier must prove absence across the authenticated, complete Admin directory immediately before login tests. `FRMS_TEST_NO_CCCD_PHONE/PASSWORD` and `FRMS_TEST_COMPLETED_CONTRACT_ID` remain without verified positive fixtures. All 6 Contracts in this snapshot are ACTIVE or TERMINATED.

See [fixtures.json](fixtures.json) for structured candidates and statuses. No test PASS or login rights follow from this snapshot.

## 5. Critical implementation discrepancies to check before further tests

At GitHub baseline `81babb4`, the canonical discovery authenticates foreign Staff F32 via `FRMS_TEST_FOREIGN_STAFF_EMAIL` and searches F32's own business date independently of F01; legacy warning text still says `no same-date` and is misleading. The local HEAD of the user-reported runs is unverified; never assume they executed the patched oracle. Do not rerun archived installers.

The negative-login verifier is now integrated as `tests/system/scripts/batch12-verify-unregistered.mjs`. The main `run-frms-tests.ps1` runs it **after** API preflight/discovery, prior to the final fixture audit and Playwright for `all`, `area`, or negative-login cases `FRMS-ST-0199`/`FRMS-ST-0203`. It requires verified Admin ACTIVE role and complete nonduplicate account pages; it does not modify users. The root legacy installer/verifier were superseded. Both targeted tests are now user-reported PASS; independent inspection of their result files remains pending. An initial `-AuditOnly` warning is expected because it has no Admin/API access.

The current `tests/e2e/playwright/tests/frms/batch12-staff.spec.ts` is older than the `test/tools/FRMS_BATCH12_FINAL2_FIX/batch12-staff.spec.ts` patch candidate. Review differences and acceptance criteria before editing. Avoid mixing installer versions blindly.

Root `profile.md` has now been updated from the second sanitized snapshot and current 101-case registry. Use it with `fixtures.json` for current metadata; a fresh report is still necessary for PASS results.

## 6. Repo links and file ownership

- Product docs: `README.md`, `AGENTS.md`, `docs/FRMS_SRS_V11.md`, `docs/FRMS_API_V11.md`, `docs/FRMS_Data_Dictionary_V11.md`
- Test registry: `tests/system/manifest/cases.json`
- Test specifications: `tests/e2e/playwright/tests/frms/*.spec.ts`
- Automation manual: `tests/system/README.md`
- Patch history: `tests/system/PATCH_HISTORY.md`
- Runner: `RUN_FRMS_TESTS.cmd`, `RUN_FRMS_TESTS.ps1`, `run-frms-tests.ps1`
- Utilities: `tests/system/scripts/{batch12-env-audit,batch12-discover,validate-coverage,normalize-results,export-jira-defects}.mjs`
- Optional profile exporter: `test/tools/FRMS_DB_PROFILE_EXPORT/`; do not commit safe-data ZIPs.
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

See [NEXT_STEPS.md](NEXT_STEPS.md). Most immediate: resolve negative fixture verification safely, reconcile F32 cross-facility discovery/STAFF test versions, validate 101 testcase discovery and run evidence; run the newly configured read-only `FRMS-ST-0065` and check evidence, while prohibiting new assignment operations on shared data. Scheduled Visit tests have no valid fixture in this snapshot.

## 9. PowerShell cleanup decision (2026-11-11)

The obsolete `APPLY_FRMS_PATCHES_TEST_SWT.ps1` was removed because its required old patch files are absent from `TEST_SWT` and patches 01–03 are already integrated. The supported command remains `.\\RUN_FRMS_TESTS.cmd`.

Keep **both** `RUN_FRMS_TESTS.ps1` (safe environment/fixture wrapper) and `run-frms-tests.ps1` (Playwright/system-test launcher): the CMD entrypoint depends on both. Also keep `backend/scripts/PostgreSql-Local.ps1` for local PostgreSQL lifecycle. The older `backend/tests/Frms.IntegrationTests/Verify-CreateReservation.ps1` targets legacy SQL Server with write operations and hardcoded paths; it is unrelated to the current automated PostgreSQL suite, retained for historical continuity only. Do not execute it on shared Supabase. This cleanup did not execute tests or validate new PASS results.

**Targeted ST-0065 execution:** `.\RUN_FRMS_TESTS.cmd -Mode case -CaseId FRMS-ST-0065 -Workers 1 -Retries 0 -SkipCatalogDiscovery`. The historical 51/51 PASS result is unchanged until new Playwright evidence exists.

For negative-login scenarios see [NEGATIVE_LOGIN_FIXTURES.md](NEGATIVE_LOGIN_FIXTURES.md). The user has now reported targeted PASS for `FRMS-ST-0199` and `FRMS-ST-0203` on 2026-11-11. Do not add 2 to historical `51/51` totals without confirming overlap and actual report IDs, or infer whole-batch PASS.

## Static test-oracle hardening — 2026-11-11 (runtime pending)

On `TEST_SWT`, `FRMS-ST-0165` now has an actual F32 Staff positive control: ACTIVE F01/F32 accounts must belong to distinct Facilities, F32 must read the nominated RESERVATION Visit and see it in F32 work items on `FRMS_TEST_OTHER_FACILITY_SCOPE_DATE`, F01 must see its own work item, and only then F01 is asserted to exclude the F32 Visit on **that same foreign date** and reject foreign Visit detail (403/404). The discovery code uses `FRMS_TEST_FOREIGN_STAFF_EMAIL` (F32, not the outdated F02), searches own/foreign dates independently and does not replace explicit snapshot overrides. Missing/stale preconditions are `FRMS_FIXTURE_MISSING`, not PASS or Jira defects.

`FRMS-ST-0065` retains its GET-only checks for the pre-existing ACTIVE assignment and now also selects the matching Reservation **in the Manager UI** and confirms the saved Unit code and `Saved` state. No save/release click or data mutation is introduced.

**Evidence:** REPO_VERIFIED static source review only. Local Playwright, authenticated API, and database runtime are not accessible through the GitHub connector; `FRMS-ST-0065` and `FRMS-ST-0165` remain **PENDING_RUNTIME / Not Run** for this change. Historical 51/51 user-reported PASS and two separately reported negative-login PASS remain unchanged. No application, DB, or `main` changes.

## Targeted runtime results received — 2026-11-11

**Evidence level: USER_REPORTED_RUNTIME_CONSOLE** (user-pasted complete Windows CMD/Playwright console; not independently retrieved `results.json` or `summary.md`).

| Testcase | Run ID | Console result | Launcher / wrapper exit |
| --- | --- | --- | --- |
| `FRMS-ST-0065` | `20261111T080719965Z` | **1/1 PASS**, 15.2 seconds; 1 worker, 0 retries | 0 / 0 |
| `FRMS-ST-0165` | `20261111T081112172Z` | **1/1 PASS**, 5.7 seconds; 1 worker, 0 retries | 0 / 0 |

Both runs: backend preflight OK, six primary actor roles verified by discovery, no state-change flags, Jira CSV 0 independently confirmed defects. **Exact local commit SHA at execution is not supplied.** Therefore, the console confirms targeted runtime success, but does *not yet establish* that the patched ST-0065 saved Unit-code/Saved UI oracle and patched ST-0165 F32 positive-control/correct-date oracle were the versions executed. Compare `git rev-parse HEAD` from the original worktree with the known baseline code SHA `81babb4`; retrieve `tests/system/reports/<run-id>/{summary.md,results.json}` privately before promoting to `REPORT_VERIFIED`.

The warning `Staff cross-facility: no same-date other-facility work item pair confirmed` is **stale diagnostic wording** in GitHub TEST_SWT's discovery code, whose date-aware search no longer requires a same-day pair; the wrapper's snapshot defaults may have supplied the foreign Visit separately. `Batch 12 fixture discovery: 0 verified value(s) supplied` indicates no newly injected variables, not proof that all existing defaults are current. These warnings do not negate Playwright PASS, but source revision and real fixture control should be audited. `FRMS-ST-0067` remains Blocked (no RESERVATION/SCHEDULED Visit).

Keep historic **51/51 user-reported regression**, ST-0199/0203 reported PASS (without run IDs) and these two targeted run IDs as **separate** evidence. Do not merge their counts or imply whole Batch 12 / 101-case PASS; oracleApproval remains pending SRS review.
