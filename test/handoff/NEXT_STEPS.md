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
