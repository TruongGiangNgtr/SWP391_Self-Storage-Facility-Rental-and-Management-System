# FRMS — Evidence ledger (latest sanitized DB snapshot)

**Source:** User-uploaded `FRMS_PROFILE_SAFE_20261111T063028Z.zip` and `FRMS_PROFILE_SUMMARY_20261111T063028Z.json`. **Generated:** `2026-11-11T06:30:28.186617Z`. The ZIP passed integrity checking and contains matching manifest/candidates in both files.

Evidence levels: `REPO_VERIFIED` (committed TEST_SWT source), `SNAPSHOT_VERIFIED` (safe exported rows), `USER_REPORTED_RUNTIME` (historical test result), `PENDING_RUNTIME` (no recent authorized API/Playwright outcome).

| Item | Value | Evidence and limitation |
| --- | --- | --- |
| Registry | 275 / 138 IMPLEMENTED / 129 NOT_IMPLEMENTED / 8 REVIEW | REPO_VERIFIED; implemented is not passed |
| Historic regression | 51/51 PASS, run `20261110T151721934Z` | USER_REPORTED_RUNTIME; 50 Batch 12 still lack whole-batch PASS |
| FRMS-ST-0199 | PASS (unknown customer phone login) | USER_REPORTED_RUNTIME, 2026-11-11; run ID/report not provided |
| FRMS-ST-0203 | PASS (unknown employee email login) | USER_REPORTED_RUNTIME, 2026-11-11; run ID/report not provided |
| FRMS-ST-0065 | **PASS**; console 1/1, 15.2 s, exit 0 | USER_REPORTED_RUNTIME_CONSOLE; run `20261111T080719965Z`; executing SHA and report artifacts not supplied |
| FRMS-ST-0165 | **PASS**; console 1/1, 5.7 s, exit 0 | USER_REPORTED_RUNTIME_CONSOLE; run `20261111T081112172Z`; executing SHA and report artifacts not supplied |
| Snapshot | 29 tables; 28 exported; 1 skipped; **1,552 rows** | SNAPSHOT_VERIFIED; AuditLog excluded |
| Facility | 63 ACTIVE | SNAPSHOT_VERIFIED |
| UserAccount | 132 (131 ACTIVE, 1 INACTIVE) | SNAPSHOT_VERIFIED; email/phone withheld |
| StorageUnit | 386: 378 AVAILABLE, 3 MAINTENANCE, 3 INSPECTION, 2 IN_USE | SNAPSHOT_VERIFIED; availability may change |
| Reservation | 12: 3 CONFIRMED, 6 COMPLETED, 2 PENDING_DEPOSIT, 1 CANCELLED | SNAPSHOT_VERIFIED |
| Visit | 21: 6 CHECKED_IN, 10 CHECKED_OUT, 4 SCHEDULED, 1 CANCELLED | SNAPSHOT_VERIFIED; **zero RESERVATION/SCHEDULED** |
| Contract | 6: 3 ACTIVE, 3 TERMINATED | SNAPSHOT_VERIFIED; zero COMPLETED |
| Invoice / Payment | 22 (19 PAID, 3 OVERDUE) / 18 (15 SUCCESS, 3 PENDING) | SNAPSHOT_VERIFIED |
| Handover assignment | 8: 2 ACTIVE, 6 CONSUMED | SNAPSHOT_VERIFIED; includes F01 and F32 ACTIVE assignments |
| Inspection | 4: 1 IN_PROGRESS, 3 COMPLETED | SNAPSHOT_VERIFIED |
| LoginHistory | 613: 589 SUCCESS, 24 FAILED | SNAPSHOT_VERIFIED; login may append history |
| Candidate F01 Reservation | `f1112fee-e99f-4094-879a-563ddd1f8f81` | CONFIRMED / CHECKED_IN / AVAILABLE matching Unit, but **already ACTIVE assignment** |
| Staff scheduled handover Visit | `6855fba5-942c-40a4-bf22-5658ca3be00c` | CHECKED_IN now; **not valid** for SCHEDULED scenario |
| Foreign Facility 32 Visit | `33a922ce-416f-4757-87f5-8f85aba047ef` | CHECKED_IN, 2026-12-05; F32/F01 permissions PENDING_RUNTIME |

## F01 handover evidence, including missing condition

The export's `assignable_manager_f01.candidateCount=1` matches Reservation `f1112fee-e99f-4094-879a-563ddd1f8f81`, Visit `6855fba5-942c-40a4-bf22-5658ca3be00c`, physical Unit `fa72ca32-22d6-496c-aca2-20e64c040de7` in Facility 01 and UnitType `b1000000-0000-0000-0000-000000000001`. **Crucially**, `HandoverUnitAssignment` row `d9e3672e-9708-46de-96a9-72a860f55065` has Status ACTIVE for the same Reservation and Unit.

This triple can support a *read-only matching-UnitType investigation* after Manager F01 access and oracle approval. It cannot be assumed to allow a *new* assignment; never call save/release to test eligibility on shared Supabase. The wrapper has now been corrected to provide this ID as a read-only ST-0065 snapshot default and not reject it. The Playwright case verifies the existing ACTIVE assignment via GET. Live Manager F01 access and test PASS are still pending.

Earlier Reservation `b370c765-adb7-4e16-aa19-54caf7d5b788` still has no matching AVAILABLE UnitType ...0002 in Facility 01 (2 IN_USE, 1 INSPECTION).

## Scheduled Visit and other fixture blockers

The old `FRMS_TEST_STAFF_HANDOVER_VISIT_ID` is now CHECKED_IN. Remaining four scheduled Visits are three ACCESS and one RETURN, not RESERVATION. Therefore, the recorded snapshot cannot justify a `RESERVATION/SCHEDULED` test input. Other missing inputs include unregistered phone/email (PII excluded), COMPLETED Contract (none), and no-CCCD account (not verified).

## Source discrepancies and validation rules

- `frontend/src/features/handover-unit-selection/HandoverUnitSelectionPage.tsx` filters Reservation CONFIRMED, Visit RESERVATION/CHECKED_IN, matching AVAILABLE Unit; it also reads existing assignments.
- `tests/e2e/playwright/tests/frms/batch12-staff.spec.ts` implements `FRMS-ST-0065` with GET Reservation, Unit, ACTIVE Assignment, and Manager UI heading; it does not create/release assignments.
- Canonical discovery at `81babb4` uses F32 and date-aware foreign verification; the residual `no same-date` warning is outdated. Supplied console logs do not establish the executing local SHA.
- A snapshot ID is not evidence of current API role authorization or application truth; live tests may alter data or be blocked by the active launcher.
- No new runtime Playwright PASS was produced during profile maintenance. Export ZIP and summary are **not committed** to public Git; only minimal test metadata is recorded.

## User-confirmed targeted test results — 2026-11-11

The user explicitly reported **FRMS-ST-0199 PASS** and **FRMS-ST-0203 PASS** after the negative-account discovery integration and launcher parser fix. Evidence category: `USER_REPORTED_RUNTIME`, not `REPORT_VERIFIED`. No run ID, `summary.md`, `results.json`, screenshot, or Playwright trace has been supplied for either run. This does not change the separate historical `51/51` regression result or prove all 50 newly mapped Batch 12 cases passed.

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
