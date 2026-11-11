# FRMS / TEST_SWT — Batch 14 implementation and static review

**Date:** 2026-11-11 • **Branch:** `TEST_SWT` • **Scope:** 15 new source-backed Playwright system-test mappings. **Environment:** existing ASP.NET Core Testing API; existing read-only/shared Supabase connection; no database writes.

## Coverage and evidence

| Measure | Before Batch 14 | With Batch 14 source |
| --- | ---: | ---: |
| Registered testcase IDs | 275 | 275 |
| IMPLEMENTED (source mappings) | 138 | **153** |
| NOT_IMPLEMENTED | 129 | **114** |
| REVIEW | 8 | 8 |
| Implementation mapping coverage | 50.18% | **55.64%** |
| Batch 14 runtime PASS | — | **NOT RUN** |

All **275** `oracleApproval` flags remain `PENDING_SRS_REVIEW`. Implementation mapping is **not** a PASS claim or approved SRS coverage. The historical full run `20261111T085924993Z` remains **95 Pass, 7 Flaky, 18 Blocked, 18 UnconfirmedFailure, 8 Review, 129 Not Run**. Older 13/17 targeted post-repair PASS evidence is separate and not additive.

### Batch 14 — 15 IDs (all status PENDING_RUNTIME)

| Group | Case IDs | Implementation | Data oracle and safety |
| --- | --- | --- | --- |
| Recorded late fees and invoice state | `0091`, `0092`, `0093`, `0094` | `batch14-billing-renewal.spec.ts` | Owner-scoped GET of invoices and billing; reflect actual fee/status; **no checkout or payment attempt** |
| Renewal preview | `0103`, `0104` | `batch14-billing-renewal.spec.ts` | Five- and six-month MonthPicker selection; no `Confirm Extension` POST |
| Pending Damage / settlement | `0116`, `0117`, `0118`, `0122`, `0243`, `0244` | `batch14-return.spec.ts` | GET actual Customer Return Summary and, for pending decision cases, Staff Inspection; **never Finalize Return** |
| First-month handover payment | `0061` | `batch14-handover-support-admin.spec.ts` | Real Manager F01 CONFIRMED + CHECKED_IN Reservation with ACTIVE unit assignment, positive Staff Visit GET. Checkbox is local form state; **never submit Complete Handover** |
| Support authorization | `0134` | `batch14-handover-support-admin.spec.ts` | Customer 1 positive Contract list, Customer 2 negative GET 403/404 and selector excludes foreign Contract; **never create ticket** |
| Employee facility selection | `0194` | `batch14-handover-support-admin.spec.ts` | Admin lookup provides all Facility IDs; combobox rejects invented ID; valid selection is local and dialog is cancelled; **never create Employee** |

**Dynamic fixtures:** Batch 14 discovers owner-visible records via existing API GETs. A case requiring a particular recorded state (e.g. seven-day late-fee boundary, amount due, pending Damage, completed Inspection without settlement) uses a verified positive state. **If none exists, the case must SKIP/Blocked rather than fabricate one or pass vacuously.** HTTP 5xx remain infrastructure/unconfirmed failures, not missing fixture success.

**Important source-vs-SRS limitations (not hidden):**
- `0091`: the frontend does not calculate a seven-day grace fee; it only displays an *absent* LateFee record from the API. This source-aligned test **does not prove** the original day-seven backend calculation.
- `0103/0104`: preview displays the selected duration; Monthly Price is `Confirmed after submission`, not a numeric discount/estimate. An end-to-end renewal confirmation requires isolated data.
- `0117/0118`: assert recorded settlement values, not actual bank refund/collection.
- `0134`: read-only cross-owner GET and UI select controls; a direct forged ticket POST remains for isolated negative API tests.
- `0194`: validates an *existing* Facility ID list, not backend rejection of a forged nonexistent Facility on create.
- `0243/0244`: require real matching Inspection / settlement states, which may be absent in the currently shared DB. Do not upgrade without fixture evidence.

## Static verification

Verified all 15 new `test('FRMS-ST-…')` declarations correspond to the 15 changed manifest records and all three new `.spec.ts` paths. ID uniqueness and manifest counts reconciled (275 / 153 / 114 / 8). No `request.post/put/patch/delete` appears in Batch 14 specs. Source selectors and API paths checked against current React frontend and ASP.NET Core routes; this is not a locally compiled TypeScript/Playwright run.

**Environment audit:** existing `FRMS_ENVIRONMENT=Testing`, API `http://127.0.0.1:5164`, UI `http://127.0.0.1:4173`, role-specific credentials and profile fixture IDs suffice for this batch. No new environment key is mandatory; see [BATCH14_ENVIRONMENT_20261111.md](BATCH14_ENVIRONMENT_20261111.md). The existing shared DB flags `FRMS_TEST_DB_ISOLATED=false`, `FRMS_ALLOW_STATE_CHANGES=NO` must remain unchanged. Do not use the connection-string password previously pasted in chat, do not print it, and do not commit it.

## Safe validation commands (Windows CMD)

First ensure the local `TEST_SWT` worktree is clean, then pull. Run all commands with an **already healthy** Testing backend. Do not pass `-StartBackend` while the remote/shared DB is connected.

```bat
git branch --show-current
git status --short
git pull --ff-only origin TEST_SWT
curl.exe http://127.0.0.1:5164/health
.\RUN_FRMS_TESTS.cmd -AuditOnly
.\RUN_FRMS_TESTS.cmd -Mode coverage
.\RUN_FRMS_TESTS.cmd -Mode case -CaseId FRMS-ST-0103 -Workers 1 -Retries 0 -SkipCatalogDiscovery
.\RUN_FRMS_TESTS.cmd -Mode case -CaseId FRMS-ST-0104 -Workers 1 -Retries 0 -SkipCatalogDiscovery
.\RUN_FRMS_TESTS.cmd -Mode case -CaseId FRMS-ST-0194 -Workers 1 -Retries 0 -SkipCatalogDiscovery
.\RUN_FRMS_TESTS.cmd -Mode case -CaseId FRMS-ST-0061 -Workers 1 -Retries 0 -SkipCatalogDiscovery
.\RUN_FRMS_TESTS.cmd -Mode case -CaseId FRMS-ST-0093 -Workers 1 -Retries 0 -SkipCatalogDiscovery
.\RUN_FRMS_TESTS.cmd -Mode case -CaseId FRMS-ST-0134 -Workers 1 -Retries 0 -SkipCatalogDiscovery
```

After these, individually test all other new IDs only where the real fixture partition exists. If API 500/503 or repeated timeouts reappear, stop broad regression, collect the run `results.json` and investigate the backend. Do not export Jira defects without an approved oracle and independent reproduction.

## Deferred four pre-existing fixture blockers

See [BLOCKED_FIXTURES_0067_0071_0109_0188.md](BLOCKED_FIXTURES_0067_0071_0109_0188.md). These IDs were **already IMPLEMENTED** before Batch 14 and remain unresolved, not new Batch 14 cases. No snapshot status is silently promoted to current authenticated DB state.

## 2026-11-11 — Batch 14 targeted rerun evidence

Three new IDs were observed PASS in user console logs: `0103` (`20261111T114719268Z`), `0104` (`20261111T114821972Z`), `0194` (`20261111T114924460Z`). `0061` (`20261111T115025397Z`) **FAILED** due to Playwright strict-mode locator ambiguity between two valid offline-payment notices. Patched only the locator within `.handover-operation form > p.muted`; **ST-0061 remains PENDING_TARGETED_RERUN**, not PASS. Total mappings stay **153/275 (55.64%)**, 114 outstanding, 8 review. Shared DB was not modified; no Jira defect confirmed. See `test/handoff/BATCH14_TARGETED_RUNS_20261111.md`.
