# FRMS — Next actions and acceptance gates

Actions are prioritized. **Do not mark an action done until new source or runtime evidence supports it.**

## P0 — Safe baseline and discovery

1. **Verify local branch state.** Confirm `TEST_SWT`, check `git status`, avoid rewriting local uncommitted work, ensure one coherent commit per change batch.
2. **Validate the case registry.** Run `.\RUN_FRMS_TESTS.cmd -Mode coverage`; expected current manifest mapping: 275 registered, 101 implemented, 166 not implemented, 8 review. If counts change, regenerate `context.json` and this handoff from the actual manifest.
3. **Audit the active discovery scripts.** Compare `tests/system/scripts/batch12-discover.mjs` and `tests/e2e/playwright/tests/frms/batch12-staff.spec.ts` against optional patch directories. Identify required F32 behavior, do not blindly overwrite.
4. **Run the integrated negative-login preflight.** It now lives under `tests/system/scripts/batch12-verify-unregistered.mjs` and is invoked by the main launcher for FRMS-ST-0199, FRMS-ST-0203 and full/area runs after backend preflight. Confirm Admin ACTIVE role, complete pagination and successful Playwright results. If verification fails, fixtures remain Blocked; do not print account PII.
5. **Review the newly matching F01 read-only tuple.** Snapshot 06:30:28 UTC identifies `f1112fee-e99f-4094-879a-563ddd1f8f81` CONFIRMED + CHECKED_IN Visit + AVAILABLE matching Unit, but an ACTIVE assignment already links it. The wrapper guard is fixed and fixture default added; next run `.\RUN_FRMS_TESTS.cmd -Mode case -CaseId FRMS-ST-0065 -Workers 1 -Retries 0 -SkipCatalogDiscovery` against authorized API. Confirm Manager GET results, UI heading and report before PASS. Do not save/release assignments on shared Supabase; stale data must classify as Blocked.
6. **Find a genuine Reservation/SCHEDULED Visit.** Snapshot has zero and the old Staff handover Visit is now CHECKED_IN; keep pre-check-in gating tests Blocked. Do not modify DB to create a fixture.

## P1 — Runtime evidence for Batch 12

7. Confirm API `/health` and exact DB target before tests. Do not use `-StartBackend` on shared DB by default; login itself can log LoginHistory.
8. Run isolated relevant cases `FRMS-ST-0165`, `FRMS-ST-0199`, `FRMS-ST-0203` after authenticated fixture verification; inspect assertions and actual results.
9. Run `.\RUN_FRMS_TESTS.cmd -Mode all -Workers 4 -Retries 1` only with accepted read-only scope. Archive the new `tests/system/reports/<run-id>/summary.md` and `results.json` privately.
10. Reconcile Pass/Blocked/Not Run/Review/UnconfirmedFailure. Independently reproduce any suspected product defects before Jira CSV export. Update runtime counts **only with a real new run ID**.

## P2 — Remaining testcase implementation

11. Plan 166 not-implemented cases by functional area and 8 REVIEW cases by SRS oracle decision.
12. For mutation, payment, concurrency and data lifecycle coverage, require an isolated disposable PostgreSQL test DB, deterministic factories, cleanup/rollback, controlled background jobs and payOS sandbox where needed.
13. Extend automated suites and coverage only after source-aligned UI expected outputs and business-state assertions are known. Never change production business rules just to satisfy tests.

## Definition of complete

- All intended `TEST_SWT` code/document changes in **one** commit for the batch.
- No modifications to `main`, database, secret files or user local uncommitted changes.
- Manifest updated only for real executable testcase implementations.
- Current runner/fixture preflight and results are recorded with evidence levels.
- Missing fixtures remain Blocked, not silently assigned.
- User can hand this directory to another AI and reproduce the run commands safely.
