# FRMS TEST_SWT — Coverage PASS and two Batch 14 targeted PASS runs

**Date:** 2026-11-11
**Evidence provenance:** User-provided Windows CMD console (not independently retrieved report JSON or a per-run Git SHA).
**Executed local revision:** `git pull --ff-only origin TEST_SWT` reported already up to date, but did not print a new SHA; the console's registry output is the authoritative snapshot for this execution (279 cases).

## Structural coverage validation (post-fix)

```text
.\RUN_FRMS_TESTS.cmd -Mode coverage
Registry: 279 cases / 279 unique / 153 implemented / 8 review / 118 outstanding
Registry partitions: 275 baseline / 4 supplemental
Registry structure and executable mappings valid. This is NOT a claim of runtime passes or SRS approval.
FRMS wrapper exit code: 0
```

This is the **first user-console confirmation that the repaired `validate-coverage.mjs` accepted the 275-case baseline plus four supplemental cases**. Coverage mode checks registry/mapping structure, **not** application runtime or SRS oracle approval.

## Targeted Playwright results

| Case | Run ID | Selected / executed / PASS | Duration | Workers | Retries | Wrapper exit | Interpretation |
| --- | --- | --- | --- | --- | --- | --- | --- |
| `FRMS-ST-0091` | `20261111T125511933Z` | **1/1/1** | 14.0s | 1 | 0 | 0 | Verified implemented owner-visible rental-invoice / no-recorded-late-fee display, **not** a computed Day-7 boundary |
| `FRMS-ST-0092` | `20261111T125616085Z` | **1/1/1** | 19.2s | 1 | 0 | 0 | Verified implemented greater-than-seven-overdue-day fee display against actual service record, **not** independent Day-8 fee formula |

The runner's `Jira CSV` line reported zero independently confirmed defects for both of these runs. Backend Testing API was already healthy at `http://127.0.0.1:5164`, Vite UI at `http://127.0.0.1:4173`. For each targeted case the launcher reported 19 applicable environment keys, 0 missing, verified actors Customer 1/2, Staff, Manager, BOM and Admin. The recurring warning for no Staff-accessible **SCHEDULED Reservation Visit** concerns deferred `0067`, not these billing tests. Shared Supabase remained read-only via `FRMS_TEST_DB_ISOLATED=false`, `FRMS_ALLOW_STATE_CHANGES=NO`, AI disabled. No state-changing step is part of these cases.

## Accounting and concurrent changes

**Batch 14 (15 new implemented IDs): 8 distinct user-console targeted PASS**, 7 without a new targeted execution. Previous six: `0103`, `0104`, `0194`, `0061`, `0093`, `0134`. New passes: `0091`, `0092`. Remaining seven: `0094`, `0116`, `0117`, `0118`, `0122`, `0243`, `0244`. Missing positive data partitions must SKIP/Blocked; do not create fictional fee, settlement or Inspection records.

**Historical full run** `20261111T085924993Z` is immutable at **95 PASS** (275 cases at that time). Separate targeted PASS results are not aggregated into it.

**Concurrent registry changes:** While evidence was being documented, `TEST_SWT` added `FRMS-ST-0280`, in addition to `0276`–`0279`. The latest separately verified source registry at note creation has **280 total, 153 IMPLEMENTED, 119 NOT_IMPLEMENTED, 8 REVIEW**, with **275 baseline + 5 supplemental**; mapping coverage is **153/280 = 54.64%**. The successful 279-case coverage log remains accurate for its earlier execution. `0280` remains NOT_IMPLEMENTED and is **not** a runtime failure or PASS. Do not silently rewrite the older snapshot.

**Four earlier fixture blockers** remain tracked separately: `FRMS-ST-0067`, `0071`, `0109`, `0188`.

## Suggested next safe commands

```bat
git status --short
git pull --ff-only origin TEST_SWT
.\RUN_FRMS_TESTS.cmd -Mode coverage
.\RUN_FRMS_TESTS.cmd -Mode case -CaseId FRMS-ST-0094 -Workers 1 -Retries 0 -SkipCatalogDiscovery
.\RUN_FRMS_TESTS.cmd -Mode case -CaseId FRMS-ST-0243 -Workers 1 -Retries 0 -SkipCatalogDiscovery
```

The remaining return cases require genuine completed Inspection, pending Damage or settlement states and may Block. Never use `-StartBackend` or enable shared-database writes merely to fill them. Source oracles remain `PENDING_SRS_REVIEW`; no new full-run report was executed.
