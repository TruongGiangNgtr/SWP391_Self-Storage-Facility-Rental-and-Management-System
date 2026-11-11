# FRMS Batch 12 — Fix for five missing fixtures (read-only refinement)

## Current result

- 15/20 fixture names were discovered at user runtime.
- The five remaining names require business-state-specific records, not merely IDs of the right shape.
- This patch **only** improves discovery and diagnostics. It **cannot promise 20/20** if appropriate records do not exist in the current database.

## Changes

1. Scans each of the known Staff work-item dates for an actual **same-day** pair of distinct facility references. The previous script checked Staff02 only on Staff01's first available work date.
2. Searches two additional dates (+90, -90 days) and, when Staff and Manager are assigned the same facility, queries the Manager's authorized GET `/visits` list to find a scheduled reservation visit outside the date window. Confirms Staff can read the visit and reservation, the reservation is `CONFIRMED`, and the scheduled work item is present in Staff's date-scoped list before selecting IDs.
3. Reports when no valid Staff handover, cross-facility reference, or owned in-progress inspection was found.
4. Keeps Manager assignable reservation rule intact: reservation `CONFIRMED`, visit `CHECKED_IN`, matching available unit in Manager's assigned facility.
5. No POST except normal demo login. No fixture values invented or stored on disk.

## Installation — Windows CMD at repository root

Extract `FRMS_BATCH12_FIXTURE_REFINEMENT` at the repository root; do not run from Downloads. With `TEST_SWT` checked out:

```bat
node .\FRMS_BATCH12_FIXTURE_REFINEMENT\install.mjs
node --check tests\system\scripts\batch12-discover.mjs
powershell.exe -NoProfile -ExecutionPolicy Bypass -File ".\run-frms-tests.ps1" -Mode all -Workers 4 -Retries 1
```

The installer refuses to overwrite any modified discovery script and keeps an original copy at `tests/system/scripts/batch12-discover.mjs.before-refinement.bak`. It does not reapply Batch 12 test patches or change the launcher hook. Do not commit the backup file.

## How to interpret unresolved items

| Fixture | Required preconditions | If still absent |
| --- | --- | --- |
| `FRMS_TEST_ASSIGNABLE_RESERVATION_ID` | `CONFIRMED` reservation, `CHECKED_IN` reservation visit and matching `AVAILABLE` physical unit within Manager facility | Requires a stateful seeded dataset or explicitly selected matching Manager account. Do not change shared data. |
| `FRMS_TEST_OTHER_FACILITY_REFERENCE_ID` | Staff01 and Staff02 each have real work items on one same date, with distinct facility assignments | Missing cross-facility work-item fixture. Do not substitute a UUID from an unrelated date. |
| `FRMS_TEST_STAFF_HANDOVER_RESERVATION_ID`, `FRMS_TEST_STAFF_HANDOVER_VISIT_ID` | Pair of `CONFIRMED` reservation and `SCHEDULED` reservation visit accessible to Staff01, represented in its work list | Requires seeded scheduled visit or existing valid pair; never use an arbitrary confirmed reservation. |
| `FRMS_TEST_STAFF_IN_PROGRESS_INSPECTION_ID` | `IN_PROGRESS` inspection claimed by the signed-in Staff01 | Requires seeded inspection with ownership, not any pending or someone else's inspection. |

## Validation performed

- `node --check` for the updated script and installer.
- Mocked API simulation demonstrated discovery of same-date cross-facility pair on a later date, a scheduled reservation visit outside normal search window via Manager `/visits`, and explicit missing-owned-inspection diagnosis.
- **Not tested against your live local backend**. Results remain unverified until you run the launcher.

If you still see missing IDs, please copy the lines beginning with `WARNING: Batch 12 fixture:` and the last missing fixture list (do not send credentials, bearer tokens, or full private API responses).
