# FRMS Batch 12 — Fix two remaining fixtures (no data writes)

**Scope:** `FRMS_TEST_OTHER_FACILITY_REFERENCE_ID` and `FRMS_TEST_ASSIGNABLE_RESERVATION_ID` only.

## Cause and fix

The previous cross-facility lookup required an own Staff work item and another-facility Staff work item **on the same day**. `FRMS-ST-0165` doesn't require that: an own-work positive observation and a foreign-work negative observation can be performed on different dates.

This version retains the own date/reference for `FRMS-ST-0057`, adds `FRMS_TEST_OTHER_FACILITY_SCOPE_DATE`, and updates `FRMS-ST-0165` to assert the foreign Visit is not in the signed-in Staff's work list on the foreign Visit date and that GET `/api/v1/visits/{visitId}` does not disclose it (403/404 accepted).

Discovery first looks for a foreign Visit in another Staff's work list. If not found, it uses the verified BOM account's cross-facility `/visits` listing, checks the reservation's real `facilityId`, and selects an eligible `SCHEDULED` or `CHECKED_IN` reservation Visit from a different facility. **No arbitrary UUIDs are generated.** The new date fixture is emitted only if a valid foreign Visit is discovered.

`FRMS_TEST_ASSIGNABLE_RESERVATION_ID` still requires an actual Manager-facility `CONFIRMED` reservation with a `CHECKED_IN` reservation visit and an `AVAILABLE` physical unit of the same `unitTypeId`. Since your current discovery didn't find one, the script now prints diagnostic counts (total reservations, confirmed, checked-in, available units, matching types) to distinguish missing data from failing API pagination. It does **not** seed/check in/reserve/assign data on shared Supabase; `FRMS-ST-0065` remains SKIPPED/Blocked until a real matching fixture is available.

## Installation: Windows CMD at TEST_SWT repository root

Unzip `FRMS_BATCH12_FINAL2_FIX` into repo root (folder name should match exactly). Then:

```bat
node .\FRMS_BATCH12_FINAL2_FIX\install.mjs
node --check tests\system\scripts\batch12-discover.mjs
powershell.exe -NoProfile -ExecutionPolicy Bypass -File ".\run-frms-tests.ps1" -Mode all -Workers 4 -Retries 1
```

Installer is idempotent, checks the exact original/revised file SHA256 before modifying, requires TEST_SWT, and creates two `.before-final2.bak` backups. If you manually edited either test file, installer **refuses to overwrite**.

## What to copy from the new run log

- `Batch 12 fixture discovery: ...`
- `WARNING: Batch 12 fixture: Manager assignable: ...`
- `WARNING: Batch 12 fixture: Staff cross-facility: ...`
- final `Missing fixture names only` list

**Do not send access tokens, real passwords or secret .env contents.**

Validation performed here: `node --check`, TypeScript syntax/transpilation check. Actual FRMS backend state and runtime test results remain unverified until your Windows run.
