# FRMS System Test Automation — TEST_SWT

**Last documentation consolidation:** 2026-11-11. This is the canonical guide for automated system tests on branch `TEST_SWT`. The repository-root `README.md` describes the application, not the test patch history.

## Current scope and evidence

- Registry: **275 unique test cases**, across 12 functional areas; source of truth: [manifest/cases.json](manifest/cases.json).
- Registry mapping: **138 IMPLEMENTED**, **129 NOT_IMPLEMENTED**, **8 REVIEW** (verified against the committed manifest).
- Historical runtime evidence: **51 implemented cases reported PASS** in run `20261110T151721934Z`, after Patch 11. The additional **50 Batch 12 cases have not been shown to pass as a complete run**.
- Registry `IMPLEMENTED` means executable code is mapped, **not** that a test passed. Each test oracle still requires SRS review.
- An unresolved fixture, missing API or wrong permission is **Blocked/Skipped**, not PASS or a confirmed software defect. An unconfirmed assertion failure is **not** automatically a Jira defect.

## Repository layout

| Path | Purpose |
| --- | --- |
| `RUN_FRMS_TESTS.cmd`, `RUN_FRMS_TESTS.ps1` | Preferred Windows entrypoint; configures test process and snapshot-based fixture defaults |
| `run-frms-tests.ps1` | Underlying launcher, preflight, catalogue/fixture discovery and report execution |

The two root PowerShell scripts are **both required**: `RUN_FRMS_TESTS.cmd` invokes `RUN_FRMS_TESTS.ps1`, which configures the safe Testing environment and snapshot candidates, then invokes `run-frms-tests.ps1` for API preflight, discovery, Playwright execution and reports. Deleting either breaks the current one-command runner.
| `tests/e2e/playwright/tests/frms/` | Playwright test specifications and helpers |
| `tests/system/manifest/cases.json` | Testcase registry and implementation mapping |
| `tests/system/scripts/` | Coverage, preflight, discovery, execution and Jira report scripts |
| `tests/system/reports/` | Generated evidence; excluded from Git |
| `profile.md` | Historical environment and test-data notes; **some earlier coverage/run claims are outdated** |
| `test/tools/FRMS_DB_PROFILE_EXPORT/README.md` | Separate, read-only PostgreSQL profile export procedure |
| [PATCH_HISTORY.md](PATCH_HISTORY.md) | Historical patch details and resolved launch/locator bugs |

The versioned patch installers in `test/tools/FRMS_BATCH12_*/` retain their own READMEs because they document optional versioned changes. **Do not re-run old installers just because the directory exists.**

## Windows CMD — first checks

Run at the repository root:

```bat
git branch --show-current
git status --short
pnpm install --frozen-lockfile
pnpm --dir tests/e2e/playwright exec playwright install chromium
```

The branch must be `TEST_SWT`. Do **not** switch, reset, reapply old patches or pull automatically if the worktree has uncommitted changes.

The preferred runner uses the local testing endpoints:

```text
FRMS_ENVIRONMENT=Testing
FRMS_API_URL=http://127.0.0.1:5164
FRMS_UI_URL=http://127.0.0.1:4173
VITE_API_PROXY_TARGET=http://127.0.0.1:5164
```

Demo logins use **phone for CUSTOMER** (`0901000001`, `0901000002`, `0901000003`) and **email for employees** (`staff.f01@example.com`, `staff.f32@example.com`, `manager.f01@example.com`, `bom@example.com`, `admin@example.com`). Store passwords in the local process/secret manager; do not commit real credentials. Verify account ACTIVE status, ownership, role and facility before relying on any fixture.

## Commands

Use the *existing* test backend on port 5164 by default. Check `http://127.0.0.1:5164/health`; health does not prove DB isolation.

```bat
rem Local process-variable and fixture inventory only (not API health):
.\RUN_FRMS_TESTS.cmd -AuditOnly

rem Registry and implementation validation:
.\RUN_FRMS_TESTS.cmd -Mode coverage

rem All implemented automation:
.\RUN_FRMS_TESTS.cmd -Mode all -Workers 4 -Retries 1

rem A selected testcase:
.\RUN_FRMS_TESTS.cmd -Mode case -CaseId FRMS-ST-0165 -Workers 1 -Retries 0

rem A selected functional area:
.\RUN_FRMS_TESTS.cmd -Mode area -Area "Authentication & Security" -Workers 2 -Retries 0
```

If `RUN_FRMS_TESTS.cmd` is unavailable, use the underlying `run-frms-tests.ps1` **only after checking that the environment and safety switches are equivalent**. Wrapper `-StartBackend` is an explicit opt-in and should **not** be used with a shared Supabase database unless the impact of scheduled jobs is approved.

The wrapper supplies the Testing process variables, read-only test flags, worker/retry defaults and snapshot-based fixture candidates. The underlying launcher performs API preflight and further discovery. **An environment-audit warning before discovery is not final runtime fixture evidence.**

## Fixtures and current known blockers

- Snapshot baseline: sanitized PostgreSQL export at `2026-11-11T06:30:28.186617Z` (UTC); identifiers and statuses can change after the snapshot. Validate them against the **current** API and the authenticated role.
- `FRMS_TEST_OTHER_FACILITY_REFERENCE_ID`: cross-facility Visit ID `33a922ce-416f-4757-87f5-8f85aba047ef`, associated with Facility 32 and Staff f32. For `FRMS-ST-0165`, independently verify F32 authorized access and F01 denied access. `referenceId` is a Visit ID for a `RESERVATION_VISIT` work item, **not** a Facility ID.
- `FRMS_TEST_ASSIGNABLE_RESERVATION_ID`: the new snapshot has a matching F01 CONFIRMED + CHECKED_IN + AVAILABLE Unit tuple (`f1112fee-e99f-4094-879a-563ddd1f8f81`), **but it already has an ACTIVE assignment**. The wrapper now configures this ID by default for read-only `FRMS-ST-0065`, and the outdated rejection has been removed. The Playwright test verifies Reservation, CHECKED_IN Visit, AVAILABLE Unit, matching Facility/UnitType, and existing ACTIVE assignment via GET, then checks Manager UI heading. **A successful runtime PASS is still pending**; never create/release an assignment on shared Supabase.
- `FRMS_TEST_STAFF_HANDOVER_VISIT_ID`: changed to `CHECKED_IN` in the new snapshot; **zero RESERVATION/SCHEDULED** Visits remain. Before-check-in gating tests have no valid fixture and must be Blocked.
- `FRMS_TEST_UNREGISTERED_CUSTOMER_PHONE` / `FRMS_TEST_UNREGISTERED_EMPLOYEE_EMAIL`: the current launcher verifies both using `tests/system/scripts/batch12-verify-unregistered.mjs` after API preflight, with an ACTIVE Admin account and complete paginated user directory. It sets the process-only values if absent and otherwise clears/blocks them; **runtime success is pending**. The sanitized DB export excludes phone/email and cannot establish absence. `-AuditOnly` reports static keys without Admin verification; non-negative single-case runs omit these unrelated missing keys in the final audit.
- Other IDs (contracts, invoices, reservations, inspections, catalogue unit types) are **candidate** values and must be revalidated for status, facility and ownership. Never insert invented UUIDs to force an audit to show zero missing.

See the `test/tools/FRMS_BATCH12_FINAL2_FIX/README.md` and `test/tools/FRMS_DB_PROFILE_EXPORT/README.md` for optional diagnostic workflows. A patch directory being tracked does not prove its installer was applied to the active launcher.

## Safety: shared database

The previous backend environment used a remote/shared Supabase PostgreSQL database. `ASPNETCORE_ENVIRONMENT=Testing` and `FRMS_TEST_DB_ISOLATED=false` do **not** make it isolated. The current batch is intended for read-only queries, authentication and negative UI validation. Login can still write `LoginHistory`.

`BackgroundJobs__Billing__Enabled=false`, `BackgroundJobs__NoShow__Enabled=false` and `BackgroundJobs__Notifications__Enabled=false` do **not** disable the Reservation Expiration job. Prefer an already-running, explicitly authorized backend. Never run state-changing reservation, handover, renewal, payment, inspection, user-admin or database migration tests against shared data without an isolated disposable DB, authorized fixtures, and cleanup/rollback.

## Reports and Jira

The system runner writes `tests/system/reports/<run-id>/`, including `summary.md`, `results.json`, and `jira_defects.csv` when generated. Review the per-case result, the console's Playwright counts and fixture warnings; overall exit code `3` can result from unimplemented/Review/Blocked registry entries even when executed assertions pass.

Only independently confirmed, reproducible product defects with an accepted expected result belong in the Jira CSV. Never import Blocked, fixture failures or unconfirmed Playwright failures as defects.

## References

- Root [README.md](../../README.md): application overview and developer setup.
- [SRS](../../docs/FRMS_SRS_V11.md), [API](../../docs/FRMS_API_V11.md), [Data Dictionary](../../docs/FRMS_Data_Dictionary_V11.md): normative requirements and implementation contracts.
- [PATCH_HISTORY.md](PATCH_HISTORY.md): collapsed history of Patch 01–12 and launcher refinements.
- `profile.md`: older environment notes; the registry and latest independently observed runtime logs take precedence for coverage and PASS counts.

## PowerShell cleanup inventory (2026-11-11)

The obsolete root `APPLY_FRMS_PATCHES_TEST_SWT.ps1` was deleted: it required three `01–03_*.patch` files, but **no `.patch` file remains** on the tracked branch, and those old patch stages are already integrated. Do not use or re-create this installer just to run tests.

Other PowerShell files intentionally retained:

- `RUN_FRMS_TESTS.ps1`: **ACTIVE**, invoked by `RUN_FRMS_TESTS.cmd`; sets safety flags, demo identities and snapshot candidate fixtures.
- `run-frms-tests.ps1`: **ACTIVE**, invoked by the wrapper; runs preflight, fixture discovery and tests and produces reports.
- `backend/scripts/PostgreSql-Local.ps1`: independent local PostgreSQL/Docker administration helper, unrelated to the FRMS test launcher.
- `backend/tests/Frms.IntegrationTests/Verify-CreateReservation.ps1`: **legacy SQL Server** test verification script with hardcoded Windows paths and write operations against a legacy test database. Not part of the current PostgreSQL Playwright test run. Do **not** run against shared Supabase. Kept to preserve existing historical test artifacts/references.

No test code, launcher code or backend code was modified by this cleanup. Do not interpret static cleanup as a runtime test PASS.

## Tool location after cleanup

Versioned fixture installers and the read-only database profile exporter are now under [`test/tools/`](../../test/tools/README.md). Do not use the old root-level `FRMS_BATCH12_*` and `FRMS_DB_PROFILE_EXPORT` paths. Installer commands must still be run **from the repository root**, because they intentionally resolve active test files using the current working directory. Moving the archived packages did **not** install or upgrade the canonical discovery code.

### Run FRMS-ST-0065 (prepared fixture, read-only)

```bat
.\RUN_FRMS_TESTS.cmd -AuditOnly
.\RUN_FRMS_TESTS.cmd -Mode case -CaseId FRMS-ST-0065 -Workers 1 -Retries 0 -SkipCatalogDiscovery
```

The wrapper supplies the snapshot ID pair by default. Verify actual `/health` separately and inspect the new `summary.md` and `results.json`. Full fixture notes are in [test/handoff/FRMS-ST-0065.md](../../test/handoff/FRMS-ST-0065.md). The test uses GET requests and browser navigation only; login can still write LoginHistory.

## Batch 13 (2026-11-11): 50.18% implementation coverage

Manifest now contains **138 IMPLEMENTED / 129 NOT_IMPLEMENTED / 8 REVIEW (275 total)**, after 37 new uniquely mapped Playwright cases. See `test/handoff/BATCH13_COVERAGE.md`. All new cases are **PENDING_RUNTIME**, with SRS oracle review pending. No new PASS, Jira defect, app edit or DB mutation is claimed. Access Visit creation ST-0077 and ST-0080 are isolated-write gated and cannot run on shared Supabase under normal wrapper defaults. Foreign physical Unit cases require a positively verified real foreign Facility Manager.

## Batch 14: 15 read-only Playwright mappings — 2026-11-11

Added **15** source-backed system tests across Billing (0091-0094), Renewal preview (0103-0104), Return/Settlement (0116-0118, 0122, 0243-0244), Handover offline acknowledgment (0061), cross-customer Support (0134), and Admin Facility selection (0194). Manifest is now **275 registered, 153 IMPLEMENTED, 114 NOT_IMPLEMENTED, 8 REVIEW** (**55.64% implementation mapping**). All 15 new cases are **PENDING_RUNTIME**, not PASS, and all 275 SRS oracles remain pending approval. Specs use authenticated GET-backed positive fixtures and no business-state write operations; missing real state must SKIP/Blocked. Historical full run 20261111T085924993Z remains 95 PASS and 13 distinct repaired-case targeted PASS remain separate. Environment uses existing Testing API/UI, Customer1/2, Staff F01, Manager F01 and Admin credentials + existing profile fixture IDs; **no new required environment keys**. Shared Supabase remains nonisolated; no write enables or startup flags. See `test/handoff/BATCH14_IMPLEMENTATION_20261111.md`, `BATCH14_ENVIRONMENT_20261111.md`, and `BLOCKED_FIXTURES_0067_0071_0109_0188.md`.

## Batch 14 targeted evidence and coverage repair — 2026-11-11

User CMD logs show `FRMS-ST-0093` (`20261111T122312270Z`, 11.9s) and `FRMS-ST-0134` (`20261111T122412231Z`, 13.5s) each **1/1 PASS**, one worker, zero retries and exit 0. Batch 14 therefore has **6/15 distinct targeted PASS**, nine pending runtime. The same log shows **coverage exit 3** under the previous validator (`Expected 275 IDs, received 276`, unknown primary `Return Inspection` on ST-0276); it is a registry-validator defect, not a failing Playwright testcase. The validator has been patched to enforce the 275-case original SRS baseline separately from `Supplemental:` cases, retaining duplicate/executable-mapping checks. Additional commits independently registered ST-0277, ST-0278, ST-0279 as NOT_IMPLEMENTED. Current source snapshot: **279 registered, 153 IMPLEMENTED, 118 NOT_IMPLEMENTED, 8 REVIEW, 54.84% implementation coverage**; **275 baseline + 4 supplemental**. This patch was statically audited only; **coverage has not yet been rerun after the patch**. Historical full run remains 95 PASS from the earlier 275-case run; zero new confirmed Jira defects from the two targeted runs. See `test/handoff/BATCH14_COVERAGE_FIX_AND_RERUNS_20261111.md`.
