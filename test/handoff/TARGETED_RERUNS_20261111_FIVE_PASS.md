# FRMS TEST_SWT — Five post-repair targeted PASS runs

**Recorded:** 2026-11-11  
**Evidence:** `USER_REPORTED_RUNTIME_CONSOLE`. The user pasted complete CMD test execution logs. The local `git rev-parse HEAD` and each run's `results.json` / `summary.md` have **not** been independently retrieved. Therefore the exact executing source SHA is **not established**, even though the latest remote TEST_SWT source before documentation was `3ef27b06ed2a078a0fe13c9ca2478f6a66f0929e`.

## Individual run results

| Case | Run ID | Selected | Executed | Passed | Workers | Retries | Duration | Runner / wrapper exit |
| --- | --- | ---: | ---: | ---: | ---: | ---: | ---: | --- |
| `FRMS-ST-0220` | `20261111T103359756Z` | 1 | 1 | **1** | 1 | 0 | 17.9s | 0 / 0 |
| `FRMS-ST-0184-B` | `20261111T103506500Z` | 1 | 1 | **1** | 1 | 0 | 8.7s | 0 / 0 |
| `FRMS-ST-0236` | `20261111T103603990Z` | 1 | 1 | **1** | 1 | 0 | 9.2s | 0 / 0 |
| `FRMS-ST-0072` | `20261111T103705001Z` | 1 | 1 | **1** | 1 | 0 | 11.8s | 0 / 0 |
| `FRMS-ST-0176` | `20261111T103912045Z` | 1 | 1 | **1** | 1 | 0 | 11.5s | 0 / 0 |

All runs passed Testing preflight against the existing healthy API `http://127.0.0.1:5164`; Vite served UI at `http://127.0.0.1:4173`. The wrapper reported `FRMS_TEST_DB_ISOLATED=false`, `FRMS_ALLOW_STATE_CHANGES=NO`, AI disabled and remote/shared database warning. Each completed run printed **Jira CSV: 0 independently confirmed defect(s)**. Fixture discovery repeatedly warned that no authorized **SCHEDULED Reservation Visit** was available for the signed-in Staff; this specific missing fixture does not contradict the successful `FRMS-ST-0220` **CHECKED_IN** test.

## Scope-aware interpretation

- `FRMS-ST-0220`: the test's existing assertions for CHECKED_IN Visit and absence of duplicate check-in passed. This does **not** establish the availability of a SCHEDULED Visit required by `FRMS-ST-0067`.
- `FRMS-ST-0184-B`: the current editor's nonnegative/zero price frontend assertion passed. **Source-oracle conflict remains**: original `PRC-TC-04` uses a VND 100,000 minimum and requires saving; current implementation permits zero frontend input. Requires an approved SRS decision.
- `FRMS-ST-0236`: Customer invoice route/payment-action boundary check for Staff passed. It **does not** attempt the original manual PAID-state mutation or prove persisted Invoice/Payment state remains unchanged.
- `FRMS-ST-0072`: existing date-only booking screen assertions passed. The testcase still does **not** explicitly assert the absence of time-slot control or all contract fields.
- `FRMS-ST-0176`: UI category whitelist/required-category no-POST check passed. It **does not** submit a forged invalid category to the API or assert the original HTTP 400 and unchanged ticket count.

These five PASS statuses apply to their **implemented Playwright assertions**, not full original-source requirement approval. Do not alter normative Expected Output to make tests pass, and do not create data on shared Supabase to fill fixture gaps.

## Accounting

- Previously documented: **8 distinct repaired cases** had post-repair targeted PASS from separate runs.
- This batch: **5 additional distinct repaired cases**, all 1/1 PASS.
- **Total targeted PASS among the 17 repaired case IDs: 13 distinct.**
- **Four still lack a post-repair targeted PASS:** `FRMS-ST-0067`, `FRMS-ST-0071`, `FRMS-ST-0109`, `FRMS-ST-0188`. These require genuine appropriate reservation/inspection fixtures and/or a stabilized backend.
- **Historical full run `20261111T085924993Z` remains unchanged** at 95 Pass, 7 Flaky, 18 Blocked, 18 UnconfirmedFailure, 8 Review, 129 Not Run. Do not add distinct targeted PASS results to its 95 Pass.
- Registry remains 275 total, **138 IMPLEMENTED, 129 NOT_IMPLEMENTED, 8 REVIEW**, implementation mapping coverage **50.18%**. All registry oracles still require SRS approval. No new full regression baseline.
- **No independently confirmed Jira defect.**

## Next action

Safely verify the remaining four targeted cases only if there are authentic fixtures. `0067` needs a Staff-readable SCHEDULED Reservation Visit; `0071` needs a CHECKED_IN Visit and genuinely no active Manager assignment; `0109` / `0188` need an IN_PROGRESS Inspection **owned by the signed-in Staff**. Missing fixtures must be classified Blocked, not passed or fabricated. Separate backend HTTP 500/503 incidents from Playwright defects. To verify the original price/payment/API-invalid oracles, first obtain approved SRS semantics and a genuinely isolated test database. Only then obtain a new full-run `summary.md` and `results.json`; do not overwrite the historical run.

**Change scope:** evidence-only documentation commit; no test source, manifest, application code, database or main branch changes.
