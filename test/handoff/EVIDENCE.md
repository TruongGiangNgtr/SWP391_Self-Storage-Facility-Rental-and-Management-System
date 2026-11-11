# FRMS — Evidence ledger (latest sanitized DB snapshot)

**Source:** User-uploaded `FRMS_PROFILE_SAFE_20261111T063028Z.zip` and `FRMS_PROFILE_SUMMARY_20261111T063028Z.json`. **Generated:** `2026-11-11T06:30:28.186617Z`. The ZIP passed integrity checking and contains matching manifest/candidates in both files.

Evidence levels: `REPO_VERIFIED` (committed TEST_SWT source), `SNAPSHOT_VERIFIED` (safe exported rows), `USER_REPORTED_RUNTIME` (historical test result), `PENDING_RUNTIME` (no recent authorized API/Playwright outcome).

| Item | Value | Evidence and limitation |
| --- | --- | --- |
| Registry | 275 / 101 IMPLEMENTED / 166 NOT_IMPLEMENTED / 8 REVIEW | REPO_VERIFIED; implemented is not passed |
| Historic regression | 51/51 PASS, run `20261110T151721934Z` | USER_REPORTED_RUNTIME; 50 Batch 12 still lack whole-batch PASS |
| FRMS-ST-0199 | PASS (unknown customer phone login) | USER_REPORTED_RUNTIME, 2026-11-11; run ID/report not provided |
| FRMS-ST-0203 | PASS (unknown employee email login) | USER_REPORTED_RUNTIME, 2026-11-11; run ID/report not provided |
| Snapshot | 29 tables; 28 exported; 1 skipped; **1,552 rows** | SNAPSHOT_VERIFIED; AuditLog excluded |
| Facility | 63 ACTIVE | SNAPSHOT_VERIFIED |
| UserAccount | 132 (131 ACTIVE, 1 INACTIVE) | SNAPSHOT_VERIFIED; email/phone withheld |
| StorageUnit | 386: 378 AVAILABLE, 3 MAINTENANCE, 3 INSPECTION, 2 IN_USE | SNAPSHOT_VERIFIED; availability may change |
| Reservation | 12: 3 CONFIRMED, 6 COMPLETED, 2 PENDING_DEPOSIT, 1 CANCELLED | SNAPSHOT_VERIFIED |
| Visit | 21: 6 CHECKED_IN, 10 CHECKED_OUT, 4 SCHEDULED, 1 CANCELLED | SNAPSHOT_VERIFIED; **zero RESERVATION/SCHEDULED** |
| Contract | 6: 3 ACTIVE, 3 TERMINATED | SNAPSHOT_VERIFIED; zero COMPLETED |
| Invoice / Payment | 22 (19 PAID, 3 OVERDUE) / 18 (15 SUCCESS, 3 PENDING) | SNAPSHOT_VERIFIED |
| Handover assignment | 8: 2 ACTIVE, 6 CONSUMED | SNAPSHOT_VERIFIED; includes F01 and F32 ACTIVE assignments |
| Inspection | 4: 1 IN_PROGRESS, 3 COMPLETED | SNAPSHOT_VERIFIED |
| LoginHistory | 613: 589 SUCCESS, 24 FAILED | SNAPSHOT_VERIFIED; login may append history |
| Candidate F01 Reservation | `f1112fee-e99f-4094-879a-563ddd1f8f81` | CONFIRMED / CHECKED_IN / AVAILABLE matching Unit, but **already ACTIVE assignment** |
| Staff scheduled handover Visit | `6855fba5-942c-40a4-bf22-5658ca3be00c` | CHECKED_IN now; **not valid** for SCHEDULED scenario |
| Foreign Facility 32 Visit | `33a922ce-416f-4757-87f5-8f85aba047ef` | CHECKED_IN, 2026-12-05; F32/F01 permissions PENDING_RUNTIME |

## F01 handover evidence, including missing condition

The export's `assignable_manager_f01.candidateCount=1` matches Reservation `f1112fee-e99f-4094-879a-563ddd1f8f81`, Visit `6855fba5-942c-40a4-bf22-5658ca3be00c`, physical Unit `fa72ca32-22d6-496c-aca2-20e64c040de7` in Facility 01 and UnitType `b1000000-0000-0000-0000-000000000001`. **Crucially**, `HandoverUnitAssignment` row `d9e3672e-9708-46de-96a9-72a860f55065` has Status ACTIVE for the same Reservation and Unit.

This triple can support a *read-only matching-UnitType investigation* after Manager F01 access and oracle approval. It cannot be assumed to allow a *new* assignment; never call save/release to test eligibility on shared Supabase. The wrapper has now been corrected to provide this ID as a read-only ST-0065 snapshot default and not reject it. The Playwright case verifies the existing ACTIVE assignment via GET. Live Manager F01 access and test PASS are still pending.

Earlier Reservation `b370c765-adb7-4e16-aa19-54caf7d5b788` still has no matching AVAILABLE UnitType ...0002 in Facility 01 (2 IN_USE, 1 INSPECTION).

## Scheduled Visit and other fixture blockers

The old `FRMS_TEST_STAFF_HANDOVER_VISIT_ID` is now CHECKED_IN. Remaining four scheduled Visits are three ACCESS and one RETURN, not RESERVATION. Therefore, the recorded snapshot cannot justify a `RESERVATION/SCHEDULED` test input. Other missing inputs include unregistered phone/email (PII excluded), COMPLETED Contract (none), and no-CCCD account (not verified).

## Source discrepancies and validation rules

- `frontend/src/features/handover-unit-selection/HandoverUnitSelectionPage.tsx` filters Reservation CONFIRMED, Visit RESERVATION/CHECKED_IN, matching AVAILABLE Unit; it also reads existing assignments.
- `tests/e2e/playwright/tests/frms/batch12-staff.spec.ts` implements `FRMS-ST-0065` with GET Reservation, Unit, ACTIVE Assignment, and Manager UI heading; it does not create/release assignments.
- The canonical discovery still defaults Staff F02 for cross-facility search, while wrapper supplies F32.
- A snapshot ID is not evidence of current API role authorization or application truth; live tests may alter data or be blocked by the active launcher.
- No new runtime Playwright PASS was produced during profile maintenance. Export ZIP and summary are **not committed** to public Git; only minimal test metadata is recorded.

## User-confirmed targeted test results — 2026-11-11

The user explicitly reported **FRMS-ST-0199 PASS** and **FRMS-ST-0203 PASS** after the negative-account discovery integration and launcher parser fix. Evidence category: `USER_REPORTED_RUNTIME`, not `REPORT_VERIFIED`. No run ID, `summary.md`, `results.json`, screenshot, or Playwright trace has been supplied for either run. This does not change the separate historical `51/51` regression result or prove all 50 newly mapped Batch 12 cases passed.
