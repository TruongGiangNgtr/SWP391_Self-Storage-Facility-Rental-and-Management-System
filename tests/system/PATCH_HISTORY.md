# FRMS TEST_SWT — Automation patch history

**Consolidated:** 2026-11-11. This document replaces the root-level patch READMEs and the older launcher-version notes. It records **historical changes**, not instructions to reapply patches. For current commands, see [README.md](README.md).

| Stage | Change | Important detail |
| --- | --- | --- |
| 01–03 | Registry, result normalization/Jira exporter, authentication UI, catalogue and API security tests | Initial **30** implemented mappings; previous README install paths are obsolete after integration |
| 04 | Windows Playwright process launch | Use Node `spawn` without shell parsing to avoid spaces in `C:\Program Files` paths and `DEP0190` |
| 05 | Playwright locators and catalogue state diagnostics | Narrow customer registration, search and month picker selectors; distinguish loading from empty catalogue |
| 06 | Catalogue API probe and timing diagnostics | Add read-only probe for relevant month windows, report API status/elapsed/collection and guard slow responses |
| 07 + V8 | Configurable workers/retries and PowerShell `Run-Pnpm` fix | Redirect native stdout to console; otherwise `$preflight = Run-Pnpm ...` captures strings plus exit code and can report a false preflight failure |
| 08 | Capacity-aware catalogue fixture selection | Filter ACTIVE facilities by the *tested month range*, then validate available unit type before using card assertions |
| 09 | 21 additional UI/security cases | Registry increased **30 → 51** implemented; initial static checks were not runtime PASS evidence |
| 10 | Authenticated route-boundary correction | Earlier run `20261110T145651711Z`: 49 Pass, 2 Fail in `FRMS-ST-0147` and `FRMS-ST-0153`; route/session handling changed |
| 11 | Playwright trace configuration placement | `test.use({trace:'off'})` must be at test-file top level, not inside `test.describe`; later run `20261110T151721934Z` reported **51 PASS** |
| 12 | Additional 50 read-only and negative-validation mappings | Registry increased **51 → 101** implemented, with **166 NOT_IMPLEMENTED** and **8 REVIEW** out of 275; the new 50 do **not** have independently established whole-batch PASS |
| Batch 12 fixture refinements | API role/fixture discovery, cross-facility F32 reference, negative-account preflight proposals | Some installers are retained as versioned tool directories. Only the source at `tests/system/scripts/` and the launcher actually control execution; do not assume later patch folders were installed |
| DB snapshot | Sanitized read-only PostgreSQL export for profile / fixture discovery | Snapshot `2026-11-11T04:40:10Z`; no assignable Reservation in Facility 01; snapshot status is not runtime validation |

## Why the old root READMEs were removed

Their commands referred to patches that were already applied, external ZIP files, and stale **30/51-case** totals. Applying those historical patches to the current branch can conflict with the installed **101-case** test suite. Only one operational guide, [README.md](README.md), now describes the supported runner and current limitations.

Removed source documents after consolidation:

- `README_APPLY_FRMS_TEST_SWT.md`
- `README_BATCH_09.md`
- `README_PATCH_04.md`, `README_PATCH_05.md`, `README_PATCH_06.md`, `README_PATCH_08.md`, `README_PATCH_10.md`, `README_PATCH_11.md`
- `RUN_FRMS_TESTS.md`, `RUN_FRMS_TESTS_V8.md`
- `PROFILE_BATCH_09_APPEND.md`, `PROFILE_PATCH_10_APPEND.md`
- `tests/system/BATCH12_ENVIRONMENT.md`

The project-root `README.md`, active `profile.md`, source code, test manifest, Playwright tests and tool-specific README files were **not deleted**.

## Historical evidence safeguards

- The 51-PASS result was reported by the user for a particular run; it does not validate newer code or data.
- Old patch instructions might mention 30/51 mappings; the committed registry currently maps **101**.
- An unavailable or ineligible fixture must remain Blocked/Skipped.
- `jira_defects.csv` is for independently confirmed product defects only.
- A remote/shared Supabase database is not isolated by the `Testing` environment name; avoid state changes and account for background jobs.
