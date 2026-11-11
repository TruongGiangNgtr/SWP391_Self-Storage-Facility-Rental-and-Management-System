# FRMS — Next actions and acceptance gates

Actions are prioritized. **Do not mark an action done until new source or runtime evidence supports it.**

## P0 — Safe baseline and discovery

1. **Verify local branch state.** Confirm `TEST_SWT`, check `git status`, avoid rewriting local uncommitted work, ensure one coherent commit per change batch.
2. **Validate the case registry.** Run `.\RUN_FRMS_TESTS.cmd -Mode coverage`; expected current manifest mapping: 275 registered, 101 implemented, 166 not implemented, 8 review. If counts change, regenerate `context.json` and this handoff from the actual manifest.
3. **Audit the active discovery scripts.** Compare `tests/system/scripts/batch12-discover.mjs` and `tests/e2e/playwright/tests/frms/batch12-staff.spec.ts` against optional patch directories. Identify required F32 behavior, do not blindly overwrite.
4. **Targeted negative login tests — PASS reported (2026-11-11):** User confirmed `FRMS-ST-0199` and `FRMS-ST-0203` both passed. The integrated Admin preflight remains the required fixture mechanism. If audit-grade provenance is needed, collect each run's `summary.md` and `results.json` (run IDs not supplied); do not reclassify as independently report-verified yet.
5. **Review the newly matching F01 read-only tuple.** Snapshot 06:30:28 UTC identifies `f1112fee-e99f-4094-879a-563ddd1f8f81` CONFIRMED + CHECKED_IN Visit + AVAILABLE matching Unit, but an ACTIVE assignment already links it. The wrapper guard is fixed and fixture default added; next run `.\RUN_FRMS_TESTS.cmd -Mode case -CaseId FRMS-ST-0065 -Workers 1 -Retries 0 -SkipCatalogDiscovery` against authorized API. Confirm Manager GET results, UI heading and report before PASS. Do not save/release assignments on shared Supabase; stale data must classify as Blocked.
6. **Find a genuine Reservation/SCHEDULED Visit.** Snapshot has zero and the old Staff handover Visit is now CHECKED_IN; keep pre-check-in gating tests Blocked. Do not modify DB to create a fixture.

## P1 — Runtime evidence for Batch 12

7. Confirm API `/health` and exact DB target before tests. Do not use `-StartBackend` on shared DB by default; login itself can log LoginHistory.
8. Audit the **reported PASS** of `FRMS-ST-0165` (run `20261111T081112172Z`) and `FRMS-ST-0065` (run `20261111T080719965Z`): collect each report and local commit SHA to verify which oracle was executed; do not rerun without a concrete reason. Continue the other unverified cases after that. The user has already reported both `FRMS-ST-0199` and `FRMS-ST-0203` PASS; their report artifacts still need archiving for independent audit.
9. Run `.\RUN_FRMS_TESTS.cmd -Mode all -Workers 4 -Retries 1` only with accepted read-only scope. Archive the new `tests/system/reports/<run-id>/summary.md` and `results.json` privately.
10. Reconcile Pass/Blocked/Not Run/Review/UnconfirmedFailure. Independently reproduce any suspected product defects before Jira CSV export. Update runtime counts **only with a real new run ID**.

## P2 — Remaining testcase implementation

11. Plan 129 not-implemented cases by functional area and 8 REVIEW cases by SRS oracle decision.
12. For mutation, payment, concurrency and data lifecycle coverage, require an isolated disposable PostgreSQL test DB, deterministic factories, cleanup/rollback, controlled background jobs and payOS sandbox where needed.
13. Extend automated suites and coverage only after source-aligned UI expected outputs and business-state assertions are known. Never change production business rules just to satisfy tests.

## Definition of complete

- All intended `TEST_SWT` code/document changes in **one** commit for the batch.
- No modifications to `main`, database, secret files or user local uncommitted changes.
- Manifest updated only for real executable testcase implementations.
- Current runner/fixture preflight and results are recorded with evidence levels.
- Missing fixtures remain Blocked, not silently assigned.
- User can hand this directory to another AI and reproduce the run commands safely.

## Source audit follow-up — 2026-11-11

The F32/foreign-date oracle and discovery hardening are now implemented in canonical active files, without running an old installer. Run the targeted cases from an authorized, already-running Testing API, without `-StartBackend`:

```bat
.\RUN_FRMS_TESTS.cmd -Mode case -CaseId FRMS-ST-0065 -Workers 1 -Retries 0 -SkipCatalogDiscovery
.\RUN_FRMS_TESTS.cmd -Mode case -CaseId FRMS-ST-0165 -Workers 1 -Retries 0 -SkipCatalogDiscovery
```

Inspect each `tests/system/reports/<run-id>/summary.md` and `results.json`. For ST-0165, positive F32 and own-F01 preconditions are required; a missing/stale Visit is Blocked. For ST-0065, the Manager UI must show the **saved Unit code**, not just a heading. **Neither test has a new runtime PASS from this source-only patch.** Confirm local worktree status independently before pulling.

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

P0: Verify updated TEST_SWT local source, run `.\RUN_FRMS_TESTS.cmd -Mode coverage` and selected read-only Batch 13 tests; archive `summary.md`/`results.json`; classify fixture gaps as Blocked. Do not execute write-gated Access Visit tests on shared Supabase.

## Batch 13 coverage validation and first runtime failure (2026-11-11)

**Coverage audit:** Windows `.\\RUN_FRMS_TESTS.cmd -Mode coverage` returned `exit code 0`; `validate-coverage.mjs` printed **275 registered / 275 unique / 138 implemented / 8 review / 129 outstanding** and `Registry structure and executable mappings valid`. Evidence: `USER_REPORTED_RUNTIME_CONSOLE`; 50.18% means implementation mapping only, **not** runtime PASS.

**FRMS-ST-0183-B** — run `20261111T084456202Z`, 1 worker, 0 retries, **1 executed / 1 failed**, runner and wrapper exit code **1**. The first page-header assertion in `batch13-pricing.spec.ts:11` failed with Playwright **strict mode violation**: `getByRole('heading', {name:'Unit Type Pricing'})` matched both page `<h1>Unit Type Pricing</h1>` and `<h2>Global Unit Type Pricing Registry</h2>`. The frontend implementation was not shown to be defective; the test did not reach the numeric input validator. **Classify as `AUTOMATION_DEFECT` / `USER_REPORTED_RUNTIME_CONSOLE`, not confirmed product defect, and do not import to Jira.** The log reports Jira CSV **0 independently confirmed defects**.

**Fix in TEST_SWT:** select exactly the page heading with `page.getByRole('heading',{name:'Unit Type Pricing',exact:true,level:1})`; source reviewed against frontend heading markup. Similar Batch 13 heading checks were reviewed for this substring ambiguity; no other confirmed duplicate was identified from supplied source. This is a code-only oracle fix, not a new PASS. **Rerun required** after pulling the patch and inspect `tests/system/reports/<new-run-id>/summary.md` and `results.json`. Historical 51/51 plus earlier separately reported ST-0065, ST-0165, ST-0199 and ST-0203 remain unchanged. No app, DB, or `main` change.

### Next targeted verification after locator fix

```bat
git status --short
git pull --ff-only origin TEST_SWT
.\\RUN_FRMS_TESTS.cmd -Mode coverage
.\\RUN_FRMS_TESTS.cmd -Mode case -CaseId FRMS-ST-0183-B -Workers 1 -Retries 0 -SkipCatalogDiscovery
```

Read the new report rather than reusing `20261111T084456202Z`. If it passes, test the other four Batch 13 Pricing cases individually (some require unavailable isolated fixtures) and only then proceed to a broader read-only run. Missing fixture is Blocked/Skipped; new failures must be diagnosed before Jira.

## Full-run Batch 13 evidence — 2026-11-11

**Run `20261111T085924993Z`**, user-pasted `summary.md` content: 275 selected/registered, 138 implemented mappings executed, **95 Pass, 7 Flaky, 18 Blocked (14 PLAYWRIGHT_SKIPPED, 4 TEST_FIXTURE_OR_INFRA), 18 UnconfirmedFailure, 8 Review, 129 Not Run**. Stable PASS among implemented = **68.84%**; implementation coverage remains **50.18%**. Evidence level `USER_REPORTED_SUMMARY_TEXT`, not independently `REPORT_VERIFIED`; the actual `results.json` and local HEAD were not supplied. See `test/handoff/RUN_20261111T085924993Z.md` and the machine-readable JSON for complete per-ID classifications. **Zero confirmed Jira product defects from this evidence.** Do not merge historical 51/51 or individual earlier runs into these counts. Neither tests nor application/DB changed.


### P0 — investigate the current full run before code patches

1. Inspect `results.json` and `error-context.md` for `UnconfirmedFailure` (18) and `Flaky` (7); match each to the existing fixture and SRS oracle, checking the original local HEAD.
2. Diagnose shared-root-cause groups in order: Access Visits (0072, 0081, 0156, 0160), Handover (0067, 0071, 0220, 0225), Billing (0085, 0235, 0236), then remaining domains.
3. Investigate Flaky 0155, 0036, 0101-B, 0145, 0183-B, 0184 and 0184-B separately; retry success is not stable PASS. Never reclassify the historical run.
4. Resolve only legitimate fixture dependencies for 18 Blocked; preserve isolated DB gates for 0077/0080 and foreign-facility positive controls.
5. Keep 8 Review pending SRS decision. Jira CSV remains limited to defects reproduced with approved oracle and valid non-mutating fixture.

## Automation repairs from uploaded full-run results — 2026-11-11

Received and parsed **user-uploaded `results(1).json`** for `20261111T085924993Z`. It reports exact execution SHA `54905be56c96fcc9e6a148e32a678197c60f1447`, environment Testing and historical **95 Pass / 7 Flaky / 18 Blocked / 18 UnconfirmedFailure / 8 Review / 129 Not Run**. The source-only repair touches **17 testcase IDs**, adds Staff/Inspection positive fixture controls, fixes deterministic selectors/type assertions and reduces repeated in-scenario auth verification. Details: `test/handoff/FIX_RUN_20261111T085924993Z.md`. **New runtime: NOT RUN; all old statuses unchanged**. API /facilities 500, /health 503, login 500/timeouts and missing state fixtures remain unresolved. No Jira product defect confirmed; 138/275 IMPLEMENTED remains 50.18%.

### Next action after source patch

Use `test/handoff/FIX_RUN_20261111T085924993Z.md` for the read-only targeted rerun commands. Verify the exact TEST_SWT HEAD locally and collect new report files. Keep 18 Blocked and remaining 5xx/timeouts separate from deterministic test defects; no fixture seeding on shared Supabase.

## Targeted reruns after automation repair — 2026-11-11

**`USER_REPORTED_RUNTIME_CONSOLE`**: four independent single-case, 1-worker, 0-retry runs after fast-forward pull to `8826f5e` each reported **1 selected / 1 executed / 1 PASS / exit code 0**: `FRMS-ST-0081` (`20261111T094923472Z`), `FRMS-ST-0156` (`20261111T095025709Z`), `FRMS-ST-0235` (`20261111T095126418Z`), `FRMS-ST-0183-B` (`20261111T095244321Z`). Coverage validation PASS with **138/275 implemented (50.18%)**. Historical full-run 95 PASS stays unchanged; no independent report JSON, no new aggregate full-suite PASS, and no Jira defects confirmed. Staff SCHEDULED Visit fixture still missing. Details: `test/handoff/TARGETED_RERUNS_20261111_AFTER_8826f5e.md`.

### Post-patch targeted verification follow-up

Four targeted cases now have separate 1/1 PASS evidence, but the remaining 13 repaired cases and full suite still need new execution. Check `-AuditOnly`, current API health, `git rev-parse HEAD`, then rerun remaining repaired cases with `-Workers 1 -Retries 0 -SkipCatalogDiscovery`, preserving each report. Pay particular attention to `FRMS-ST-0067` (requires genuinely SCHEDULED Staff Visit; do not fabricate shared data), flaky pricing boundaries `0184/0184-B`, and server 500/503/timeouts. After stable targeted validation, repeat `-Mode all -Workers 1 -Retries 0` and use its own `summary.md`/`results.json` as a new baseline.

## Targeted reruns after 1547d67 (2026-11-11)

USER_REPORTED_RUNTIME_CONSOLE: three targeted 1/1 PASS (ST-0160 run 20261111T095951885Z; ST-0085 run 20261111T100054156Z; ST-0184 run 20261111T100258194Z), and one automation locator FAIL (ST-0141 run 20261111T100152311Z). All were run with 1 worker, 0 retries after a reported fast-forward pull. ST-0141's compound Facility label caused getByLabel('Facility',{exact:true}) to find nothing; source now targets the dialog-scoped Facility combobox and verifies presence for Facility Manager / absence for BOM. Fix PENDING_RUNTIME; no ST-0141 PASS claimed. Details: test/handoff/TARGETED_RERUNS_20261111_AFTER_1547d67.md.

Historical full run remains 95 Pass / 7 Flaky / 18 Blocked / 18 UnconfirmedFailure / 8 Review / 129 Not Run. Registry unchanged: 138/275 IMPLEMENTED (50.18%). Across both post-repair targeted groups, seven distinct cases separately PASS; do not sum these into full-run counts. Zero confirmed Jira product defects.


Immediate verification: rerun FRMS-ST-0141 with -Mode case -Workers 1 -Retries 0 -SkipCatalogDiscovery after pulling TEST_SWT. Inspect new summary.md and results.json; do not rewrite historical runs or seed shared Supabase.

## FRMS-ST-0141 targeted rerun PASS — 2026-11-11

**USER_REPORTED_RUNTIME_CONSOLE:** `FRMS-ST-0141` run `20261111T101339028Z` reports **1 selected, 1 executed, 1 PASS (7.6s)** with one worker, zero retries and launcher/wrapper exit code 0. This is separate from the prior `20261111T100152311Z` automation FAIL and from full-run `20261111T085924993Z`. The actual executing SHA and results.json are not supplied. **Eight distinct repaired case IDs now have individual targeted PASS evidence; nine of 17 still lack a fresh successful targeted rerun.** No Jira defects confirmed. Historical full run remains 95 PASS, and registry coverage remains 138/275 (50.18%). See `test/handoff/TARGETED_RERUN_FRMS-ST-0141_20261111T101339028Z.md`.

### ST-0141 fix verified by targeted console run

The new run `20261111T101339028Z` reports 1/1 PASS after the selector source patch. Do not rerun this solely to clear the old historical failure. Prioritize the nine repaired cases without successful post-patch targeted evidence, separately diagnose backend 500/503/timeouts, then collect a new full run with one worker and zero retries. Preserve read-only shared database policy.

## Static re-verification audit — 2026-11-11

At `TEST_SWT` commit `d84eb3d`, all **275 manifest IDs are unique** and all **138 IMPLEMENTED** cases have exactly one matching executable Playwright title across 16 declared specs (no unmapped titles). Registered: 138 IMPLEMENTED / 129 NOT_IMPLEMENTED / 8 REVIEW; coverage still **50.18%**. **All 275 SRS oracles remain PENDING_SRS_REVIEW**; 53 sourceKey values contain trailing backticks (traceability cleanup pending). Of the 17 previously repaired cases, 8 have distinct user-console targeted PASS and 9 still lack successful new runs. Static source/original-workbook comparison flags **oracle/traceability gaps** in invoice snapshot/payment, invalid ticket category API, legacy price minimum, old access time-slot behavior, and several partial UI assertions. **No tests or registry expectations were modified and no new runtime PASS was claimed.** See `test/handoff/TESTCASE_REVERIFICATION_20261111.md` for the 17-case matrix and approved-SRS decisions needed. Historical full run remains 95 PASS.

Next P0: obtain approved SRS decisions for pricing threshold, legacy time-slot rules, persisted invoice snapshot/payment mutation and invalid-category API behavior before asserting full requirement coverage. Keep the nine unverified repaired cases PENDING_RUNTIME; run read-only targeted cases first, isolate all write-sensitive tests, and preserve original full-run statuses.

## Five additional targeted PASS runs — 2026-11-11

**USER_REPORTED_RUNTIME_CONSOLE**, each 1 selected / 1 executed / 1 PASS, workers=1, retries=0, wrapper exit 0: ST-0220 `20261111T103359756Z`, ST-0184-B `20261111T103506500Z`, ST-0236 `20261111T103603990Z`, ST-0072 `20261111T103705001Z`, ST-0176 `20261111T103912045Z`. This brings distinct repaired-case targeted PASS evidence to **13 of 17**; four cases `0067/0071/0109/0188` still lack a new targeted PASS. Partial or conflicting oracles for `0184-B/0236/0176/0072` remain open even though current Playwright assertions passed. Original full run remains 95 Pass; implementation mapping 138/275=50.18%, no Jira defects confirmed. Full provenance and caveats: `test/handoff/TARGETED_RERUNS_20261111_FIVE_PASS.md`.


### Remaining repaired-case runtime checks after 13/17 targeted PASS

`FRMS-ST-0067`: legitimate Staff-accessible SCHEDULED Reservation Visit required, currently absent; do not re-use CHECKED_IN fixture. `FRMS-ST-0071`: CHECKED_IN reservation with no ACTIVE assignment required. `FRMS-ST-0109` and `FRMS-ST-0188`: signed-in Staff must own an IN_PROGRESS Inspection. Preserve Blocked for missing genuine fixtures and do not modify shared Supabase. SRS/negative API oracle gaps remain open even after five new targeted UI passes. A fresh full regression should be taken only after fixture/backend issues are addressed.

## Batch 14: 15 read-only Playwright mappings — 2026-11-11

Added **15** source-backed system tests across Billing (0091-0094), Renewal preview (0103-0104), Return/Settlement (0116-0118, 0122, 0243-0244), Handover offline acknowledgment (0061), cross-customer Support (0134), and Admin Facility selection (0194). Manifest is now **275 registered, 153 IMPLEMENTED, 114 NOT_IMPLEMENTED, 8 REVIEW** (**55.64% implementation mapping**). All 15 new cases are **PENDING_RUNTIME**, not PASS, and all 275 SRS oracles remain pending approval. Specs use authenticated GET-backed positive fixtures and no business-state write operations; missing real state must SKIP/Blocked. Historical full run 20261111T085924993Z remains 95 PASS and 13 distinct repaired-case targeted PASS remain separate. Environment uses existing Testing API/UI, Customer1/2, Staff F01, Manager F01 and Admin credentials + existing profile fixture IDs; **no new required environment keys**. Shared Supabase remains nonisolated; no write enables or startup flags. See `test/handoff/BATCH14_IMPLEMENTATION_20261111.md`, `BATCH14_ENVIRONMENT_20261111.md`, and `BLOCKED_FIXTURES_0067_0071_0109_0188.md`.


### Batch 14 targeted validation (new source, not runtime verified)

Check clean `TEST_SWT` worktree, pull, existing API health, `-AuditOnly` and `-Mode coverage` first. Use `-Mode case -CaseId FRMS-ST-0103 -Workers 1 -Retries 0 -SkipCatalogDiscovery`, then `0104`, `0194`, `0061`, `0093`, `0134`. Remaining Batch 14 cases require genuine matching financial/inspection fixtures; if missing, record `Blocked`, do not insert into shared Supabase. Preserve the four deferred fixture IDs `0067/0071/0109/0188` for future authorized fixture verification; none became PASS from Batch 14. Confirm TypeScript and Playwright runtime locally before Jira or merge claims.

## 2026-11-11 — Batch 14 targeted rerun evidence

Three new IDs were observed PASS in user console logs: `0103` (`20261111T114719268Z`), `0104` (`20261111T114821972Z`), `0194` (`20261111T114924460Z`). `0061` (`20261111T115025397Z`) **FAILED** due to Playwright strict-mode locator ambiguity between two valid offline-payment notices. Patched only the locator within `.handover-operation form > p.muted`; **ST-0061 remains PENDING_TARGETED_RERUN**, not PASS. Total mappings stay **153/275 (55.64%)**, 114 outstanding, 8 review. Shared DB was not modified; no Jira defect confirmed. See `test/handoff/BATCH14_TARGETED_RUNS_20261111.md`.

## 2026-11-11 — ST-0061 targeted PASS after locator fix

User CMD log confirmed `FRMS-ST-0061` run `20261111T120242294Z`: **1/1 PASS, 24.3s**, workers=1, retries=0, exit codes 0. The local branch was fast-forwarded to `fb5f216` before execution. Earlier `20261111T115025397Z` strict-locator FAIL remains historical; **0061 now has post-fix targeted PASS evidence**. Batch 14 thus has **4/15 distinct targeted PASS**, 11 without runtime, no newly confirmed Jira defect from this run; DB remains read-only. Since this run, remote branch also added ST-0276 as NOT_IMPLEMENTED, increasing registry to **276 cases / 153 IMPLEMENTED / 115 NOT_IMPLEMENTED / 8 REVIEW (55.43%)**. The old full run (275 cases, 95 PASS) and 0061's own 275-case runner snapshot remain unchanged. See `test/handoff/BATCH14_ST0061_PASS_20261111T120242294Z.md`.
