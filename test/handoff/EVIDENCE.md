# FRMS — Evidence ledger and snapshot interpretation

## Evidence classification

- `REPO_VERIFIED`: read from the committed GitHub `TEST_SWT` source.
- `SNAPSHOT_VERIFIED`: read from the user-supplied sanitized DB export at `2026-11-11T04:40:10Z`.
- `USER_REPORTED_RUNTIME`: based on the user's console/report output, not a new run here.
- `PENDING_RUNTIME`: requires an authorized live API/browser run.
- `BLOCKED`: precondition definitively absent from the cited snapshot or currently not verified.

## Evidence table

| Claim | Value | Evidence | Meaning |
| --- | --- | --- | --- |
| Total registry | 275 IDs | REPO_VERIFIED: `tests/system/manifest/cases.json` | Indexed testcases |
| Executable mapping | 101 implemented | REPO_VERIFIED: manifest | Not a PASS count |
| Not implemented | 166 | REPO_VERIFIED: manifest | Must remain Not Run |
| Review | 8 | REPO_VERIFIED: manifest | Requires requirement/oracle decision |
| Historical regression | 51 Pass / 51 executed | USER_REPORTED_RUNTIME: run `20261110T151721934Z` | Historic only |
| Batch 12 +50 | No verified whole-batch runtime PASS | PENDING_RUNTIME | Do not claim 101 Pass |
| Last missing fixture names | 3 | USER_REPORTED_RUNTIME: environment audit | May be prior to discovery |
| DB snapshot | 29 tables, 28 exported, 1,548 safe-field rows | SNAPSHOT_VERIFIED | Metadata-only export, AuditLog skipped |
| Facilities | 63 ACTIVE | SNAPSHOT_VERIFIED | Does not imply all capacity/time ranges ready |
| User accounts | 132 (131 ACTIVE, 1 INACTIVE) | SNAPSHOT_VERIFIED | Phone/email redacted |
| Reservations | 12 | SNAPSHOT_VERIFIED | 3 CONFIRMED, 6 COMPLETED, 2 PENDING_DEPOSIT, 1 CANCELLED |
| Visits | 21 | SNAPSHOT_VERIFIED | 5 CHECKED_IN, 10 CHECKED_OUT, 5 SCHEDULED, 1 CANCELLED |
| Physical units | 386 (378 AVAILABLE) | SNAPSHOT_VERIFIED | Total across all facilities, not necessarily eligible for F01 |
| Contracts | 6 (3 ACTIVE, 3 TERMINATED) | SNAPSHOT_VERIFIED | No COMPLETED contract established |
| Invoices | 22 | SNAPSHOT_VERIFIED | 19 PAID, 3 OVERDUE |
| Inspections | 4 | SNAPSHOT_VERIFIED | 1 IN_PROGRESS |
| Handover assignments | 7 | SNAPSHOT_VERIFIED | 1 ACTIVE, 6 CONSUMED |
| Foreign F32 Visit | `33a922ce-416f-4757-87f5-8f85aba047ef` | SNAPSHOT_VERIFIED | Runtime staff permissions untested |
| F01 assignable Reservation | No qualifying tuple | SNAPSHOT_VERIFIED | `FRMS-ST-0065` Blocked |

## Why F01 handover fixture cannot be set

The UI's selection predicate requires a `CONFIRMED` Reservation, `RESERVATION` Visit `CHECKED_IN`, and physical unit with `status=AVAILABLE`, the **same `facilityId`** and **same `unitTypeId`**.

- Candidate A `b370c765-adb7-4e16-aa19-54caf7d5b788`: Reservation and Visit satisfy the first two conditions. Facility 01 UnitType 0002 physical units in snapshot: **2 IN_USE, 1 INSPECTION, 0 AVAILABLE**.
- Candidate B `f1112fee-e99f-4094-879a-563ddd1f8f81`: Unit availability exists for the type, but Visit `SCHEDULED`, not `CHECKED_IN`.
- Foreign Facility 32 candidate `bc9b2c4c-9d74-4f16-b0d6-73156a42fe85` cannot substitute for Manager F01 assignment.

Never send an assignment request to "fix" the fixture on shared Supabase. If the DB state changes, regenerate the read-only snapshot and revalidate both IDs.

## Runtime gaps and version skew

- The repository `profile.md` contains old patch-state claims; current counts come from the manifest.
- Root optional script `batch12-verify-unregistered.mjs` is present but the committed wrapper lacks the dedicated hook injected by its installer.
- The canonical `batch12-discover.mjs` uses Staff F02 for automatic foreign discovery and scans same-day pairs; a later F32 patch was not shown applied to canonical code.
- A snapshot-confirmed Visit ID is **not** evidence of a 403/404 facility authorization outcome.
- The source `FRMS-ST-0065` implementation checks unit-type matching and page labels; the business precondition must be validated before running, even if the script itself does not assert every lifecycle state.

## Materials not to put in Git

Do **not** add `FRMS_PROFILE_SAFE_*.zip`, raw JSONL exports, complete Admin user lists, `.env`, JWT, payOS secrets, passwords or database dump. Source material was inspected as evidence but is intentionally not reproduced in this handoff.
