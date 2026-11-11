# FRMS / TEST_SWT — Batch 13 coverage handoff

**Date:** 2026-11-11  
**Branch:** `TEST_SWT` only  
**Baseline:** 275 registered, 101 IMPLEMENTED, 166 NOT_IMPLEMENTED, 8 REVIEW  
**After this batch:** **138 IMPLEMENTED / 129 NOT_IMPLEMENTED / 8 REVIEW** = **50.18% implementation coverage**.

## Added Playwright testcase mappings (37)

### Handover (12)

- `FRMS-ST-0064`
- `FRMS-ST-0064-B`
- `FRMS-ST-0066`
- `FRMS-ST-0071`
- `FRMS-ST-0220`
- `FRMS-ST-0223`
- `FRMS-ST-0223-B`
- `FRMS-ST-0223-C`
- `FRMS-ST-0224`
- `FRMS-ST-0224-B`
- `FRMS-ST-0225`
- `FRMS-ST-0226`

### Facility Operations (4)

- `FRMS-ST-0168`
- `FRMS-ST-0170`
- `FRMS-ST-0172`
- `FRMS-ST-0172-B`

### Access Visits and Billing (10)

- `FRMS-ST-0077`
- `FRMS-ST-0080`
- `FRMS-ST-0084`
- `FRMS-ST-0162-B`
- `FRMS-ST-0163`
- `FRMS-ST-0086`
- `FRMS-ST-0095`
- `FRMS-ST-0096`
- `FRMS-ST-0235`
- `FRMS-ST-0236`

### Support Tickets and Administration (6)

- `FRMS-ST-0127`
- `FRMS-ST-0173`
- `FRMS-ST-0176`
- `FRMS-ST-0180-B`
- `FRMS-ST-0145`
- `FRMS-ST-0192`

### Pricing and Reference Data (5)

- `FRMS-ST-0183-B`
- `FRMS-ST-0184`
- `FRMS-ST-0184-B`
- `FRMS-ST-0187`
- `FRMS-ST-0188`

The tests reside under `tests/e2e/playwright/tests/frms/batch13-*.spec.ts`, share `batch13-helpers.ts`, and map to `tests/system/manifest/cases.json`. Each case ID appears as an executable Playwright test title, not just a documentation placeholder.

## Evidence, safety and status

All 37 are **IMPLEMENTED** and `PENDING_RUNTIME` with `oracleApproval=PENDING_SRS_REVIEW`. There is **no new PASS**, TypeScript compilation result, Playwright execution report or independently confirmed Jira defect from this source-only implementation. Historic 51/51 and the separately user-reported ST-0199, ST-0203, ST-0065 and ST-0165 must not be counted as Batch 13 PASS or summed without inspecting run IDs.

The ordinary runner disables business-state writes using `FRMS_TEST_DB_ISOLATED=false` and `FRMS_ALLOW_STATE_CHANGES=NO`. Only **ST-0077** and **ST-0080** contain a successful access-Visit creation POST, and they call `isolatedWrite()` before it; on the shared Supabase default they are skipped. Run them only against an independently verified disposable PostgreSQL database with authorized fixtures and cleanup. Other new scenarios use GET, page navigation, negative-form validation and login (which may append LoginHistory).

Cross-facility physical-unit scenarios **ST-0223-C** and **ST-0172-B** explicitly need a real ACTIVE foreign Facility Manager: `FRMS_TEST_FOREIGN_MANAGER_EMAIL` and `FRMS_TEST_FOREIGN_MANAGER_PASSWORD`. They verify authenticated manager role, distinct Facility assignment, a real foreign Unit from an authorized list, and a positive detail GET *before* checking F01 denial. Do not invent UUIDs or demo credentials.

Additional requirements such as an actual IN_USE/MAINTENANCE unit of the matching type, an unassigned checked-in Reservation, completed handover, CHECKED_IN ACCESS Visit, overdue Late Fee or completed Support Ticket are checked at runtime; absent/stale cases are Blocked, not product defects. Price editor boundary tests check nonnegative frontend validation and never submit price changes on shared data. ST-0235 checks recorded invoice values across refresh, not an isolated historical price-change experiment; record that limit in SRS oracle review.

## Reproduction (Windows CMD)

Check a clean local worktree before pulling:

```bat
git branch --show-current
git status --short
git pull --ff-only origin TEST_SWT

curl.exe http://127.0.0.1:5164/health
.\RUN_FRMS_TESTS.cmd -AuditOnly
.\RUN_FRMS_TESTS.cmd -Mode coverage
.\RUN_FRMS_TESTS.cmd -Mode case -CaseId FRMS-ST-0183-B -Workers 1 -Retries 0 -SkipCatalogDiscovery
.\RUN_FRMS_TESTS.cmd -Mode all -Workers 1 -Retries 0
```

Do **not** use `-StartBackend` against an unapproved shared DB, and do not automatically run migrations, create fixtures or make price/payment/hand-over mutations. Run TypeScript/typecheck and Playwright discovery from the repository's own toolchain first. Collect the new `tests/system/reports/<run-id>/summary.md` and `results.json` for real per-case classification (PASS, Blocked, Review, Not Run, UnconfirmedFailure). Mark Jira defects only after accepted oracle and independent reproduction. Update `profile.md` and `test/handoff/` in one commit when new evidence is available.

**Coverage definition:** 138/275 describes **test automation implementation mappings**, not test execution success or approved business requirement coverage.
