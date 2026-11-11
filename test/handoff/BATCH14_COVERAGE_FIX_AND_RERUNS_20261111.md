# FRMS TEST_SWT — Coverage validator repair and targeted reruns

**Date:** 2026-11-11  
**Evidence:** User-provided Windows CMD log, **not** independent per-run `results.json` or executing local HEAD. The log includes `git pull --ff-only origin TEST_SWT` fast-forwarding to `f69dedde8ee659112d63368bcb04398503076eab`.

## Runner evidence

| Action | Run ID | Result | Duration / exit |
| --- | --- | --- | --- |
| `-Mode coverage` | No run ID | **FAIL**: validator rejected the extra `FRMS-ST-0276` | Exit 3 |
| `FRMS-ST-0093` | `20261111T122312270Z` | **PASS**: 1 selected, 1 executed | 11.9s / exit 0 |
| `FRMS-ST-0134` | `20261111T122412231Z` | **PASS**: 1 selected, 1 executed | 13.5s / exit 0 |

All targeted cases ran `-Workers 1 -Retries 0 -SkipCatalogDiscovery`. Existing Testing API `http://127.0.0.1:5164` was healthy and Vite UI ran on `http://127.0.0.1:4173`. Environment `FRMS_TEST_DB_ISOLATED=false`, `FRMS_ALLOW_STATE_CHANGES=NO`, AI calls disabled; role/fixture preflight reported 19 applicable keys, zero missing for each targeted case. Each targeted run printed zero independently confirmed Jira defects. Shared Supabase was not written by the tests.

**Interpretation:** `0093` passed the implemented existing-record PAID invoice UI checks, **not** a payment transaction; `0134` passed existing Customer/contract ownership and dropdown checks, **not** a forged invalid cross-customer Support Ticket POST. Those separate passes increase Batch 14's unique successful targeted case IDs to **6/15**: `0103`, `0104`, `0194`, `0061`, `0093`, `0134`. Nine have no post-implementation targeted-run evidence.

## Coverage error and repair

The reported 276-case run of `tests/system/scripts/validate-coverage.mjs` failed with:

```text
COVERAGE ERROR: Expected 275 IDs, received 276.
COVERAGE ERROR: Unknown primary area for FRMS-ST-0276: Return Inspection
```

**Root cause:** The validator hardcoded exactly 275 cases and accepted only the 12 original primary areas. The first supplemental defect test `FRMS-ST-0276` uses `Return Inspection` and was correctly recorded as `NOT_IMPLEMENTED`. The registry was not malformed.

**Source-only fix:** Preserve strict per-area counts for the canonical **275 original cases**; separately validate any case whose `sourceKey` begins `Supplemental:`. Supplemental IDs must be after the baseline (numeric ID greater than 0275); their area must be an original area or `Return Inspection`. Total must equal 275 baseline cases plus the actual number of supplemental records. The original executable `test()` mapping, unique-ID, channel, automation-state and `REVIEW` validation remains in place. Extra source cases do not silently count toward original-area quotas. A second informational line now reports baseline/supplemental partition sizes.

While review/repair was underway, additional **source-only** commits added `FRMS-ST-0277` (file type), `FRMS-ST-0278` (oversized evidence) and `FRMS-ST-0279` (Staff Access Visit CCCD UI). They remain **NOT_IMPLEMENTED**, with oracle approval pending. Thus current HEAD when this report was prepared has:

- **279 registered**, 279 unique;
- **153 IMPLEMENTED**, **118 NOT_IMPLEMENTED**, **8 REVIEW**;
- **Implementation coverage: 153/279 = 54.84%**;
- **275 baseline** plus **4 supplemental** (`0276`, `0277`, `0278`, `0279`).
- These 4 supplemental cases are **not automatically product defects**; defect status and SRS/oracle approval remain separately documented.

Static structure check of current manifest against the new validator partition logic **passed**; local Windows `node tests/system/scripts/validate-coverage.mjs` and Playwright were **not run by the assistant**. The user's failed coverage run and two successful Playwright runs remain separate historical evidence; do not retroactively mark coverage PASS.

## Historical results, fixture backlog and next commands

The original full run `20261111T085924993Z` still has **95 PASS among its 275 selected**, and the previous repaired-case targeted passes remain separate. Four earlier fixture blockers — `FRMS-ST-0067`, `0071`, `0109`, `0188` — remain deferred; no test DB seeding is authorized.

After confirming a clean worktree and pulling `TEST_SWT`:

```bat
git status --short
git pull --ff-only origin TEST_SWT
.\RUN_FRMS_TESTS.cmd -Mode coverage
.\RUN_FRMS_TESTS.cmd -Mode case -CaseId FRMS-ST-0091 -Workers 1 -Retries 0 -SkipCatalogDiscovery
.\RUN_FRMS_TESTS.cmd -Mode case -CaseId FRMS-ST-0092 -Workers 1 -Retries 0 -SkipCatalogDiscovery
```

Inspect the new coverage exit code and independent reports. If the current data has no suitable rental invoice/fee partition, `0091/0092` should be Blocked rather than manufactured into PASS. Do not run `-StartBackend`, enable writes, or execute negative uploads in the shared database. No changes to frontend, backend, manifest status, database or `main` were made for this validator repair.
