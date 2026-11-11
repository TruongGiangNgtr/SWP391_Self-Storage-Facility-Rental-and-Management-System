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
| Handover | 29 | 17 | 12 | 0 |
| Facility Operations | 10 | 6 | 4 | 0 |
| Access Visits | 25 | 13 | 9 | 3 |
| Billing & Payments | 23 | 10 | 13 | 0 |
| Renewal | 16 | 3 | 13 | 0 |
| Return & Settlement | 22 | 2 | 20 | 0 |
| Support Tickets | 25 | 8 | 12 | 5 |
| Administration | 19 | 7 | 12 | 0 |
| Pricing & Reference Data | 14 | 10 | 4 | 0 |
| **TOTAL** | **275** | **138** | **129** | **8** |

Manifest path: `tests/system/manifest/cases.json`. Automated implementation coverage is **50.18%**.

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

Root `profile.md` has now been updated from the second sanitized snapshot and 138-case mapping registry (new tests still PENDING_RUNTIME). Use it with `fixtures.json` for current metadata; a fresh report is still necessary for PASS results.

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

## Batch 13 — 50.18% implemented coverage (2026-11-11)

The `TEST_SWT` registry now contains **275 unique cases: 138 IMPLEMENTED (50.18%), 129 NOT_IMPLEMENTED, 8 REVIEW**. Batch 13 adds 37 distinct case IDs mapped to five new Playwright specs and a source-checked fixture helper. Read `test/handoff/BATCH13_COVERAGE.md` for exact cases, fixture gates and commands.

**Evidence: REPO_VERIFIED_SOURCE_MAPPING_ONLY; PENDING_RUNTIME**. No new Playwright runtime PASS, TypeScript compilation, approved SRS oracle or Jira defect was produced by this change. Historical 51/51 and individual user-reported ST-0199, ST-0203, ST-0065, ST-0165 remain separate evidence. Both new access Visit creation cases require an isolated disposable DB and `isolatedWrite()`; all new shared-safe cases are authentication/GET/browser-only. Foreign physical Unit tests require an authenticated real foreign Manager positive control; do not invent fixtures. Missing preconditions remain Blocked.

## Batch 13 coverage validation and first runtime failure (2026-11-11)

**Coverage audit:** Windows `.\\RUN_FRMS_TESTS.cmd -Mode coverage` returned `exit code 0`; `validate-coverage.mjs` printed **275 registered / 275 unique / 138 implemented / 8 review / 129 outstanding** and `Registry structure and executable mappings valid`. Evidence: `USER_REPORTED_RUNTIME_CONSOLE`; 50.18% means implementation mapping only, **not** runtime PASS.

**FRMS-ST-0183-B** — run `20261111T084456202Z`, 1 worker, 0 retries, **1 executed / 1 failed**, runner and wrapper exit code **1**. The first page-header assertion in `batch13-pricing.spec.ts:11` failed with Playwright **strict mode violation**: `getByRole('heading', {name:'Unit Type Pricing'})` matched both page `<h1>Unit Type Pricing</h1>` and `<h2>Global Unit Type Pricing Registry</h2>`. The frontend implementation was not shown to be defective; the test did not reach the numeric input validator. **Classify as `AUTOMATION_DEFECT` / `USER_REPORTED_RUNTIME_CONSOLE`, not confirmed product defect, and do not import to Jira.** The log reports Jira CSV **0 independently confirmed defects**.

**Fix in TEST_SWT:** select exactly the page heading with `page.getByRole('heading',{name:'Unit Type Pricing',exact:true,level:1})`; source reviewed against frontend heading markup. Similar Batch 13 heading checks were reviewed for this substring ambiguity; no other confirmed duplicate was identified from supplied source. This is a code-only oracle fix, not a new PASS. **Rerun required** after pulling the patch and inspect `tests/system/reports/<new-run-id>/summary.md` and `results.json`. Historical 51/51 plus earlier separately reported ST-0065, ST-0165, ST-0199 and ST-0203 remain unchanged. No app, DB, or `main` change.

## Full-run Batch 13 evidence — 2026-11-11

**Run `20261111T085924993Z`**, user-pasted `summary.md` content: 275 selected/registered, 138 implemented mappings executed, **95 Pass, 7 Flaky, 18 Blocked (14 PLAYWRIGHT_SKIPPED, 4 TEST_FIXTURE_OR_INFRA), 18 UnconfirmedFailure, 8 Review, 129 Not Run**. Stable PASS among implemented = **68.84%**; implementation coverage remains **50.18%**. Evidence level `USER_REPORTED_SUMMARY_TEXT`, not independently `REPORT_VERIFIED`; the actual `results.json` and local HEAD were not supplied. See `test/handoff/RUN_20261111T085924993Z.md` and the machine-readable JSON for complete per-ID classifications. **Zero confirmed Jira product defects from this evidence.** Do not merge historical 51/51 or individual earlier runs into these counts. Neither tests nor application/DB changed.

## Automation repairs from uploaded full-run results — 2026-11-11

Received and parsed **user-uploaded `results(1).json`** for `20261111T085924993Z`. It reports exact execution SHA `54905be56c96fcc9e6a148e32a678197c60f1447`, environment Testing and historical **95 Pass / 7 Flaky / 18 Blocked / 18 UnconfirmedFailure / 8 Review / 129 Not Run**. The source-only repair touches **17 testcase IDs**, adds Staff/Inspection positive fixture controls, fixes deterministic selectors/type assertions and reduces repeated in-scenario auth verification. Details: `test/handoff/FIX_RUN_20261111T085924993Z.md`. **New runtime: NOT RUN; all old statuses unchanged**. API /facilities 500, /health 503, login 500/timeouts and missing state fixtures remain unresolved. No Jira product defect confirmed; 138/275 IMPLEMENTED remains 50.18%.

## Targeted reruns after automation repair — 2026-11-11

**`USER_REPORTED_RUNTIME_CONSOLE`**: four independent single-case, 1-worker, 0-retry runs after fast-forward pull to `8826f5e` each reported **1 selected / 1 executed / 1 PASS / exit code 0**: `FRMS-ST-0081` (`20261111T094923472Z`), `FRMS-ST-0156` (`20261111T095025709Z`), `FRMS-ST-0235` (`20261111T095126418Z`), `FRMS-ST-0183-B` (`20261111T095244321Z`). Coverage validation PASS with **138/275 implemented (50.18%)**. Historical full-run 95 PASS stays unchanged; no independent report JSON, no new aggregate full-suite PASS, and no Jira defects confirmed. Staff SCHEDULED Visit fixture still missing. Details: `test/handoff/TARGETED_RERUNS_20261111_AFTER_8826f5e.md`.

## Targeted reruns after 1547d67 (2026-11-11)

USER_REPORTED_RUNTIME_CONSOLE: three targeted 1/1 PASS (ST-0160 run 20261111T095951885Z; ST-0085 run 20261111T100054156Z; ST-0184 run 20261111T100258194Z), and one automation locator FAIL (ST-0141 run 20261111T100152311Z). All were run with 1 worker, 0 retries after a reported fast-forward pull. ST-0141's compound Facility label caused getByLabel('Facility',{exact:true}) to find nothing; source now targets the dialog-scoped Facility combobox and verifies presence for Facility Manager / absence for BOM. Fix PENDING_RUNTIME; no ST-0141 PASS claimed. Details: test/handoff/TARGETED_RERUNS_20261111_AFTER_1547d67.md.

Historical full run remains 95 Pass / 7 Flaky / 18 Blocked / 18 UnconfirmedFailure / 8 Review / 129 Not Run. Registry unchanged: 138/275 IMPLEMENTED (50.18%). Across both post-repair targeted groups, seven distinct cases separately PASS; do not sum these into full-run counts. Zero confirmed Jira product defects.

## FRMS-ST-0141 targeted rerun PASS — 2026-11-11

**USER_REPORTED_RUNTIME_CONSOLE:** `FRMS-ST-0141` run `20261111T101339028Z` reports **1 selected, 1 executed, 1 PASS (7.6s)** with one worker, zero retries and launcher/wrapper exit code 0. This is separate from the prior `20261111T100152311Z` automation FAIL and from full-run `20261111T085924993Z`. The actual executing SHA and results.json are not supplied. **Eight distinct repaired case IDs now have individual targeted PASS evidence; nine of 17 still lack a fresh successful targeted rerun.** No Jira defects confirmed. Historical full run remains 95 PASS, and registry coverage remains 138/275 (50.18%). See `test/handoff/TARGETED_RERUN_FRMS-ST-0141_20261111T101339028Z.md`.

## Static re-verification audit — 2026-11-11

At `TEST_SWT` commit `d84eb3d`, all **275 manifest IDs are unique** and all **138 IMPLEMENTED** cases have exactly one matching executable Playwright title across 16 declared specs (no unmapped titles). Registered: 138 IMPLEMENTED / 129 NOT_IMPLEMENTED / 8 REVIEW; coverage still **50.18%**. **All 275 SRS oracles remain PENDING_SRS_REVIEW**; 53 sourceKey values contain trailing backticks (traceability cleanup pending). Of the 17 previously repaired cases, 8 have distinct user-console targeted PASS and 9 still lack successful new runs. Static source/original-workbook comparison flags **oracle/traceability gaps** in invoice snapshot/payment, invalid ticket category API, legacy price minimum, old access time-slot behavior, and several partial UI assertions. **No tests or registry expectations were modified and no new runtime PASS was claimed.** See `test/handoff/TESTCASE_REVERIFICATION_20261111.md` for the 17-case matrix and approved-SRS decisions needed. Historical full run remains 95 PASS.

## Five additional targeted PASS runs — 2026-11-11

**USER_REPORTED_RUNTIME_CONSOLE**, each 1 selected / 1 executed / 1 PASS, workers=1, retries=0, wrapper exit 0: ST-0220 `20261111T103359756Z`, ST-0184-B `20261111T103506500Z`, ST-0236 `20261111T103603990Z`, ST-0072 `20261111T103705001Z`, ST-0176 `20261111T103912045Z`. This brings distinct repaired-case targeted PASS evidence to **13 of 17**; four cases `0067/0071/0109/0188` still lack a new targeted PASS. Partial or conflicting oracles for `0184-B/0236/0176/0072` remain open even though current Playwright assertions passed. Original full run remains 95 Pass; implementation mapping 138/275=50.18%, no Jira defects confirmed. Full provenance and caveats: `test/handoff/TARGETED_RERUNS_20261111_FIVE_PASS.md`.

## Batch 14: 15 read-only Playwright mappings — 2026-11-11

Added **15** source-backed system tests across Billing (0091-0094), Renewal preview (0103-0104), Return/Settlement (0116-0118, 0122, 0243-0244), Handover offline acknowledgment (0061), cross-customer Support (0134), and Admin Facility selection (0194). Manifest is now **275 registered, 153 IMPLEMENTED, 114 NOT_IMPLEMENTED, 8 REVIEW** (**55.64% implementation mapping**). All 15 new cases are **PENDING_RUNTIME**, not PASS, and all 275 SRS oracles remain pending approval. Specs use authenticated GET-backed positive fixtures and no business-state write operations; missing real state must SKIP/Blocked. Historical full run 20261111T085924993Z remains 95 PASS and 13 distinct repaired-case targeted PASS remain separate. Environment uses existing Testing API/UI, Customer1/2, Staff F01, Manager F01 and Admin credentials + existing profile fixture IDs; **no new required environment keys**. Shared Supabase remains nonisolated; no write enables or startup flags. See `test/handoff/BATCH14_IMPLEMENTATION_20261111.md`, `BATCH14_ENVIRONMENT_20261111.md`, and `BLOCKED_FIXTURES_0067_0071_0109_0188.md`.

## 2026-11-11 — Batch 14 targeted rerun evidence

Three new IDs were observed PASS in user console logs: `0103` (`20261111T114719268Z`), `0104` (`20261111T114821972Z`), `0194` (`20261111T114924460Z`). `0061` (`20261111T115025397Z`) **FAILED** due to Playwright strict-mode locator ambiguity between two valid offline-payment notices. Patched only the locator within `.handover-operation form > p.muted`; **ST-0061 remains PENDING_TARGETED_RERUN**, not PASS. Total mappings stay **153/275 (55.64%)**, 114 outstanding, 8 review. Shared DB was not modified; no Jira defect confirmed. See `test/handoff/BATCH14_TARGETED_RUNS_20261111.md`.

## 2026-11-11 — ST-0061 targeted PASS after locator fix

User CMD log confirmed `FRMS-ST-0061` run `20261111T120242294Z`: **1/1 PASS, 24.3s**, workers=1, retries=0, exit codes 0. The local branch was fast-forwarded to `fb5f216` before execution. Earlier `20261111T115025397Z` strict-locator FAIL remains historical; **0061 now has post-fix targeted PASS evidence**. Batch 14 thus has **4/15 distinct targeted PASS**, 11 without runtime, no newly confirmed Jira defect from this run; DB remains read-only. Since this run, remote branch also added ST-0276 as NOT_IMPLEMENTED, increasing registry to **276 cases / 153 IMPLEMENTED / 115 NOT_IMPLEMENTED / 8 REVIEW (55.43%)**. The old full run (275 cases, 95 PASS) and 0061's own 275-case runner snapshot remain unchanged. See `test/handoff/BATCH14_ST0061_PASS_20261111T120242294Z.md`.

## Batch 14 targeted evidence and coverage repair — 2026-11-11

User CMD logs show `FRMS-ST-0093` (`20261111T122312270Z`, 11.9s) and `FRMS-ST-0134` (`20261111T122412231Z`, 13.5s) each **1/1 PASS**, one worker, zero retries and exit 0. Batch 14 therefore has **6/15 distinct targeted PASS**, nine pending runtime. The same log shows **coverage exit 3** under the previous validator (`Expected 275 IDs, received 276`, unknown primary `Return Inspection` on ST-0276); it is a registry-validator defect, not a failing Playwright testcase. The validator has been patched to enforce the 275-case original SRS baseline separately from `Supplemental:` cases, retaining duplicate/executable-mapping checks. Additional commits independently registered ST-0277, ST-0278, ST-0279 as NOT_IMPLEMENTED. Current source snapshot: **279 registered, 153 IMPLEMENTED, 118 NOT_IMPLEMENTED, 8 REVIEW, 54.84% implementation coverage**; **275 baseline + 4 supplemental**. This patch was statically audited only; **coverage has not yet been rerun after the patch**. Historical full run remains 95 PASS from the earlier 275-case run; zero new confirmed Jira defects from the two targeted runs. See `test/handoff/BATCH14_COVERAGE_FIX_AND_RERUNS_20261111.md`.

## Batch 14: coverage verified and ST-0091/ST-0092 targeted PASS — 2026-11-11

User CMD output confirmed `.\RUN_FRMS_TESTS.cmd -Mode coverage` **PASS, exit 0** using the 279-case snapshot (275 baseline + 4 Supplemental), 153 implemented / 118 not implemented / 8 review. `FRMS-ST-0091` run `20261111T125511933Z` **1/1 PASS** (14.0s) and `FRMS-ST-0092` run `20261111T125616085Z` **1/1 PASS** (19.2s), both 1 worker, 0 retries, exit 0. Batch 14 therefore has **8/15 unique targeted PASS**, seven without runtime evidence. These frontend checks do **not** independently verify a Day-7/Day-8 late-fee formula; SRS approval remains pending. The old full run is still 95 PASS in its 275-case snapshot; do not merge targeted totals into it. Safe read-only DB settings unchanged. Asynchronous manual supplement additions after the 279-case run have already increased the registry to **at least 281** (ST-0280 and ST-0281 added); use live manifest/context for current counts. Full evidence: `test/handoff/BATCH14_COVERAGE_PASS_AND_ST0091_ST0092_20261111.md`.

