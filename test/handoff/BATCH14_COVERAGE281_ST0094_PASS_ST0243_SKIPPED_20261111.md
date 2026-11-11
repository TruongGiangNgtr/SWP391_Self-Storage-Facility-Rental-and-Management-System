# FRMS TEST_SWT — 281-case coverage PASS, ST-0094 PASS and ST-0243 skipped

**Date:** 2026-11-11. **Evidence source:** pasted Windows CMD transcript; no access to the local `results.json`, `summary.md`, `playwright-results.json` or per-run Git SHA. The user ran `git pull --ff-only origin TEST_SWT` immediately before the tests and saw `Already up to date`.

## Recorded results

| Action | Local run ID | Reported result | Runner exit / note |
| --- | --- | --- | --- |
| `-Mode coverage` | No report run ID | **PASS**: `281 cases / 281 unique / 153 implemented / 8 review / 120 outstanding`, `275 baseline / 6 supplemental` | **0**, mapping/registry validation only |
| `FRMS-ST-0094` | `20261111T135631438Z` | **1 selected, 1 executed, 1 PASS**, 19.0 s | **0**, 1 worker, 0 retries |
| `FRMS-ST-0243` | `20261111T140148408Z` | **1 selected, 1 executed, 1 SKIPPED** | **2** (runner mapped skip to Blocked), 1 worker, 0 retries |

Both targeted calls reported a healthy existing Testing API `http://127.0.0.1:5164`, Vite UI `http://127.0.0.1:4173`, confirmed demo actor roles, 19 applicable fixture environment keys / 0 missing, and **0 independently confirmed Jira defects**. The shared Supabase database remained configured read-only (`FRMS_TEST_DB_ISOLATED=false`, `FRMS_ALLOW_STATE_CHANGES=NO`; AI disabled).

### ST-0243 skip: evidence limits

The `FRMS-ST-0243` source first calls `matchingReturn(request, s => s.inspection?.status === 'COMPLETED' && s.settlement === null)`. That helper enumerates owner-visible Contracts for Customer 1 and Customer 2 using GET requests and checks each associated `/contracts/{id}/return-summary`. When none meets the predicate, it calls `precondition(false, 'owner-visible return/settlement record matching the required state')`, which uses `test.skip` with `FRMS_FIXTURE_MISSING:` in its message. Thus a missing matching real owner-visible record is a **plausible** skip reason, not yet proven because the provided console shows only `1 skipped` without the skip message. The local report must be inspected to establish exact cause; a transient API/permission failure could also lead to a skipped precondition in the current harness. **Do not convert this skip to PASS or a Jira product defect.**

Local evidence is in `tests/system/reports/20261111T140148408Z/`, principally `summary.md`, `results.json`, and `playwright-results.json`. No field-level fixture record was inferred or created. Future remediation, if needed, must use an already valid Customer-owned completed Inspection with no settlement, not a write to the shared DB.

**CLI typo:** the user ran `-SkipCatalogDiscover` (without the final `y`) for ST-0243 rather than `-SkipCatalogDiscovery`. The runner still started and skipped the Playwright case; correct the option in future commands. No causal conclusion is drawn from the typo.

### Accounting

**Batch 14:** **9 distinct targeted PASS** (`0103`, `0104`, `0194`, `0061`, `0093`, `0134`, `0091`, `0092`, `0094`), **1 targeted SKIPPED/Blocked awaiting exact cause** (`0243`), **5 without submitted runtime evidence** (`0116`, `0117`, `0118`, `0122`, `0244`). No full regression was run.

**Original repaired-case fixture backlog:** `0067`, `0071`, `0109`, `0188` stays open and separate. **Historical full regression:** run `20261111T085924993Z` still reports 95 PASS under its then-275-case registry; targeted PASS values are not added to that run.

**Oracle qualification:** ST-0094 verifies source-aligned overdue rental invoice and contract-status UI, not an independent backend fee or status calculation. All registry acceptance oracles remain `PENDING_SRS_REVIEW`.

## Next commands (existing healthy Testing backend, no DB writes)

```bat
git status --short
git pull --ff-only origin TEST_SWT
.\RUN_FRMS_TESTS.cmd -Mode coverage
type tests\system\reports\20261111T140148408Z\summary.md
findstr /i /c:"FRMS_FIXTURE_MISSING" tests\system\reports\20261111T140148408Z\playwright-results.json
.\RUN_FRMS_TESTS.cmd -Mode case -CaseId FRMS-ST-0117 -Workers 1 -Retries 0 -SkipCatalogDiscovery
.\RUN_FRMS_TESTS.cmd -Mode case -CaseId FRMS-ST-0118 -Workers 1 -Retries 0 -SkipCatalogDiscovery
```

`0117` and `0118` may legitimately SKIP if real owner-visible finalized settlement partitions with positive refund or additional amount due are unavailable. Do not seed or modify shared Supabase to force those states. Keep defect/coverage/pass provenance separate.
