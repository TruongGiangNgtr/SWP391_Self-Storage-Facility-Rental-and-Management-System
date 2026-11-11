# FRMS / TEST_SWT — Testcase re-verification audit

**Review date:** 2026-11-11  
**Repository:** TruongGiangNgtr/SWP391_Self-Storage-Facility-Rental-and-Management-System  
**Reviewed HEAD:** `d84eb3db2e8259f58e6b0da1763bfa804baff797`  
**Scope:** static registry integrity for all 275 cases; executable title/mapping reconciliation for all 138 implemented cases in 16 spec files; detailed source/oracle review of the 17 cases modified by the previous repair. This is **not** a new runtime test run.

**Basis:** current `tests/system/manifest/cases.json`, the 16 mapped Playwright spec files, corresponding frontend/backend source, user-provided 2026-11-11 targeted Windows CMD logs, the user's earlier `Test case requirement_v2(2).md` (v2.0, 2026-11-10) and original-source export `FRMS_Consolidated_System_Test_Cases_AI_Ready(1).md` (246 original source rows). The latter two are reference artifacts in the user's Library, not independently approved SRS documents. **A mismatch with an original source row is a requirements decision, not automatically a product defect.**

## Integrity checks

- **275** registry cases with **275 unique IDs**, nonempty Expected Output and `sourceKey`.
- Exactly **138 IMPLEMENTED**, **129 NOT_IMPLEMENTED**, **8 REVIEW** = **50.18% implementation mapping coverage**.
- **138 of 138** implemented IDs have one and only one corresponding executable Playwright `test('FRMS-ST-…')` title in their declared implementation file; no mapped title missing, no extra unmapped test, no duplicate executable ID across the mapped 16 files.
- **All 275 `oracleApproval` values are `PENDING_SRS_REVIEW`**; no claim of approved acceptance criteria.
- **53** registry entries have a spurious trailing backtick in `sourceKey` (data-cleanliness issue; no testcase IDs changed). Multiple distinct IDs intentionally refer to the same original source key where scenario partitions were split. Do not silently merge or delete these cases.
- Local Node/TypeScript compilation and execution against the user's current API/DB were **not** performed during this static review.

## Repaired-case verification matrix (17)

| Testcase | Latest post-repair targeted evidence | Source/oracle assessment | Finding |
| --- | --- | --- | --- |
| `FRMS-ST-0072` | PENDING_RUNTIME | PARTIAL | Selected Contract/date/submit checked; not the absence of time slot or complete contract details |
| `FRMS-ST-0081` | PASS (targeted) | PARTIAL | After-end calendar date disabled; direct invalid-input submission/no POST not verified |
| `FRMS-ST-0156` | PASS (targeted) | SOURCE_ALIGNED_UI | Date-only layout, rental details and My Visits link checked |
| `FRMS-ST-0160` | PASS (targeted) | SOURCE_CONFLICT + PARTIAL | Checks min date, not rejected yesterday submission; original test required time-slot acceptance but frontend is date-only |
| `FRMS-ST-0085` | PASS (targeted) | PARTIAL | Table filters/columns verified; invoice detail action/link not directly asserted |
| `FRMS-ST-0235` | PASS (targeted) | ORACLE_GAP_HIGH | Invoice text is compared before/after refresh, without changed catalog price or captured contract/extension terms |
| `FRMS-ST-0236` | PENDING_RUNTIME | ORACLE_GAP_HIGH | Staff denied Customer route; original requires attempted paid-state mutation and unchanged invoice/payment state |
| `FRMS-ST-0141` | PASS (targeted) | PARTIAL | Customer option excluded, manager/global Facility UI checked; no forced invalid-role payload submitted |
| `FRMS-ST-0183-B` | PASS (targeted) | SOURCE_TRACE_CONFLICT | 99,999 nonnegative check mapped to PRC-TC-03, originally about invalid numeric/negative/empty inputs |
| `FRMS-ST-0184` | PASS (targeted) | SOURCE_CONFLICT_HIGH | Original PRC-TC-04 rejects 99,999 and accepts 100,000; current code permits nonnegative values; SRS decision needed |
| `FRMS-ST-0184-B` | PENDING_RUNTIME | SOURCE_CONFLICT_HIGH | New zero-is-valid case conflicts with originating 100,000 minimum / original invalid-zero note; needs approved SRS |
| `FRMS-ST-0067` | PENDING_RUNTIME | FIXTURE_BLOCKER | Requires real Staff-accessible SCHEDULED Reservation Visit, not CHECKED_IN demo fixture |
| `FRMS-ST-0071` | PENDING_RUNTIME | FIXTURE_BLOCKER | Requires genuine CHECKED_IN confirmed reservation with no ACTIVE unit assignment; backend returns 200/null when absent |
| `FRMS-ST-0220` | PENDING_RUNTIME | SOURCE_ALIGNED_UI | Positive Staff GET validates CHECKED_IN Visit and confirms repeat check-in button absent |
| `FRMS-ST-0109` | PENDING_RUNTIME | PARTIAL + FIXTURE | Needs Staff-owned IN_PROGRESS inspection; checks editing fields but not Finalize Return step |
| `FRMS-ST-0188` | PENDING_RUNTIME | ORACLE_GAP_HIGH + FIXTURE | Checks dynamic ACTIVE DamageType options; source specifies exactly six seeded types and non-admin modification restriction |
| `FRMS-ST-0176` | PENDING_RUNTIME | ORACLE_GAP_HIGH | UI category whitelist and no-submit checked; original requires invalid API category rejected HTTP 400 and no ticket persisted |

**Evidence distinction:** The eight `PASS (targeted)` values above are from separate user-supplied CMD transcripts, not directly retrieved local `results.json` files. A Playwright PASS means that the implemented assertions passed; it does **not** resolve PARTIAL or SOURCE_CONFLICT findings. The nine PENDING_RUNTIME cases have no new successful post-repair targeted evidence. The prior full-run `20261111T085924993Z` remains **95 Pass / 7 Flaky / 18 Blocked / 18 UnconfirmedFailure / 8 Review / 129 Not Run**. No new full-run baseline or independently confirmed Jira product defect is asserted.

## Critical SRS/oracle decisions

1. **Pricing (0184, 0184-B, 0183-B):** the original source `Khoa:MOD05_Pricing:PRC-TC-04` states **99,999 rejected / 100,000 accepted**, while `UnitTypePricingPage.tsx` uses a nonnegative validation rule (`parsedPrice < 0`) and numeric input `min=0`. `0183-B` is attributed to original `PRC-TC-03` despite now testing 99,999. The current registry silently reflects implementation rather than original expected behavior. Determine the approved SRS threshold, then preserve both the normative expected behavior and an implementation-mismatch record where necessary; do not rewrite expectation solely to make a test pass.
2. **Access visit (0160):** original `Khoa:MOD02_Visit:VIS-TC-05` demanded yesterday rejection **and** today-with-a-later-time-slot acceptance. The current UI is intentionally **date-only**, and the implemented case checks minimum-date metadata only. Resolve the obsolete time-slot specification and add an observable invalid-date no-POST assertion, or explicitly supersede the original with approval.
3. **Ticket API (0176):** original `SUP-TC-04` requires a forged out-of-enum category request to return **HTTP 400** without creating a ticket; current test only verifies the UI select and empty-category validation. A direct negative API test belongs in an **isolated disposable DB** or a separately approved non-mutating validation harness; never assume an unvalidated request cannot write to shared Supabase.
4. **Billing (0235, 0236):** `BIL-TC-09` requires captured base and discount terms and protection against later price changes; `BIL-TC-10` requires a forbidden manual PAID-state mutation plus unchanged persisted state. Current tests only verify a same-invoice refresh or a role route boundary. Use a reproducible isolated fixture plus before/after state comparisons to prove the original claim.
5. **Damage types (0188):** original `PRC-TC-08` describes exactly six fixed types and non-admin mutation denial. Current test checks the actual dynamic ACTIVE catalogue only. Do not impose the original fixed count unless the approved SRS confirms it, but do not claim the original fixed-six/mutation rule has been tested.
6. **Remaining partial assertions:** `0072` does not assert the absence of time-slot input, `0081` does not force the after-max submission, `0085` does not assert an Invoice Details link, `0141` does not force an invalid role outside the select, and `0109` does not inspect the Finalize Return control.

## Fixture and safe rerun plan

Prioritize safe, read-only targeted reruns for `FRMS-ST-0220`, `FRMS-ST-0184-B`, `FRMS-ST-0236`, `FRMS-ST-0072`, and `FRMS-ST-0176`, with one worker and zero retries; do not interpret PASS as approval for the incomplete normative oracle. `FRMS-ST-0067` needs an actual SCHEDULED Staff Reservation Visit; `0071` needs an actually unassigned CHECKED_IN Visit; `0109` and `0188` need a Staff-owned IN_PROGRESS inspection. Treat absent fixtures as Blocked, not PASS or software defects. Do not seed or modify shared Supabase to satisfy them. Full state-mutating verification of billing prices, invoice payment and forced-invalid API payloads requires a genuinely isolated database and approved test data.

**No testcase code, manifest Expected Output, sourceKey, application code, main branch or DB was changed by this review.** Follow the v2.0 principle: preserve the original requirement claim and disclose implementation mismatch until an authoritative SRS decision exists.


## Post-audit evidence addendum — five targeted runs (2026-11-11)

Subsequent user-supplied Windows console logs report **1/1 PASS** for `FRMS-ST-0220` (`20261111T103359756Z`), `FRMS-ST-0184-B` (`20261111T103506500Z`), `FRMS-ST-0236` (`20261111T103603990Z`), `FRMS-ST-0072` (`20261111T103705001Z`) and `FRMS-ST-0176` (`20261111T103912045Z`). This supplements, but does not retroactively change, the 8/17 snapshot above: the newest cumulative evidence is **13/17 distinct targeted PASS**, with `0067/0071/0109/0188` awaiting successful new execution. The partial/contradictory normative oracles identified above remain **UNRESOLVED**, including for the newly passing `0184-B`, `0236`, and `0176`. Historical full-run Pass remains 95. Source: `test/handoff/TARGETED_RERUNS_20261111_FIVE_PASS.md`.
