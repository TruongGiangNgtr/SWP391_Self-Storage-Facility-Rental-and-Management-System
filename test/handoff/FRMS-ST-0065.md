# FRMS-ST-0065 — Ready-to-run read-only fixture

**Prepared:** 2026-11-11, branch `TEST_SWT`  
**Testcase:** `FRMS-ST-0065` — Check Unit Type Match: valid partition (assigned unit matches reserved type)  
**Source:** user-supplied sanitized PostgreSQL export generated `2026-11-11T06:30:28.186617Z`  
**Runtime verification:** PENDING; no Playwright pass claimed.

## 1. Snapshot-backed test data

| Field / key | Value | Snapshot state |
| --- | --- | --- |
| Manager login | `manager.f01@example.com` | Demo identifier from launcher; login and role must be verified at runtime |
| `FRMS_TEST_HANOI_FACILITY_ID` | `f0000000-0000-0000-0000-000000000001` | Facility 01 ACTIVE |
| `FRMS_TEST_ASSIGNABLE_RESERVATION_ID` | `f1112fee-e99f-4094-879a-563ddd1f8f81` | Reservation CONFIRMED |
| Reservation Visit ID | `6855fba5-942c-40a4-bf22-5658ca3be00c` | RESERVATION, CHECKED_IN |
| Reservation UnitType ID | `b1000000-0000-0000-0000-000000000001` | Matches Unit Type |
| `FRMS_TEST_AVAILABLE_PHYSICAL_UNIT_ID` | `fa72ca32-22d6-496c-aca2-20e64c040de7` | AVAILABLE, Unit Code A-102 |
| Existing Handover Assignment | `d9e3672e-9708-46de-96a9-72a860f55065` | ACTIVE; links Reservation and Unit |

**Important distinction:** This is a **read-only matching/assigned-unit fixture**, not a blank Reservation ready for new assignment. The database already has an ACTIVE assignment. Do not PUT/DELETE assignment, perform check-in, or create fixtures in shared Supabase to make this testcase pass.

## 2. Code changes made for this fixture

- `RUN_FRMS_TESTS.ps1` supplies the Reservation ID as a snapshot default, preserving an explicit local override, and no longer treats it as known-ineligible.
- Existing Unit ID and Facility ID defaults are unchanged and match this Reservation.
- `tests/e2e/playwright/tests/frms/batch12-staff.spec.ts` verifies only GET operations:
  - `GET /api/v1/reservations/{reservationId}`: must be authorized to Manager, same Facility, CONFIRMED with RESERVATION/CHECKED_IN Visit.
  - `GET /api/v1/storage-units/{unitId}`: AVAILABLE, same Facility and UnitType.
  - `GET /api/v1/reservations/{reservationId}/handover-unit-assignment`: ACTIVE, same Reservation ID and Unit ID.
  - Navigate to Manager `/manager/handover-unit-selection` and verify the `Available Matching Units` heading.

Data that changed since the snapshot is treated as `FRMS_FIXTURE_MISSING` (Blocked in the system report), not automatically a product defect. Credentials are process-local/ignored; never commit passwords or tokens.

## 3. CMD: exact execution sequence

Open CMD in the repository root:

```bat
cd /d "C:\Users\buidi\OneDrive\Desktop\Git\SWP391\SWP391_Self-Storage-Facility-Rental-and-Management-System"
git status --short
git fetch origin TEST_SWT
git pull --ff-only origin TEST_SWT

rem The backend must already be running with approved shared-DB safeguards.
curl http://127.0.0.1:5164/health

rem This audit confirms process variable presence, not current DB eligibility.
.\RUN_FRMS_TESTS.cmd -AuditOnly

rem One testcase; no other catalogue discovery.
.\RUN_FRMS_TESTS.cmd -Mode case -CaseId FRMS-ST-0065 -Workers 1 -Retries 0 -SkipCatalogDiscovery
```

If the branch diverged or there are uncommitted local changes, **do not force-reset or blindly pull**. Preserve local work and reconcile first.

Do not pass `-StartBackend` by default: an API started against shared Supabase may run Reservation Expiration even if other background job flags are disabled. The read-only test still authenticates using POST and may append LoginHistory.

## 4. Expected result and failure handling

- **PASS (only after actual execution):** Manager can GET the current Reservation, Unit and ACTIVE assignment with matching IDs/states, and Manager page has the correct heading. No assignment PUT/DELETE is sent.
- **Blocked:** API unavailable, invalid Manager credentials/role, Reservation no longer CONFIRMED, Visit no longer CHECKED_IN, physical Unit no longer AVAILABLE, or ACTIVE assignment changed. Investigate fixture drift and record the latest status; no production defect inferred.
- **UnconfirmedFailure:** Assertions fail despite verified valid preconditions; independently investigate the implementation and approved oracle before Jira.
- **Not Run:** No runtime command/report was supplied.

Look in `tests/system/reports/<run-id>/summary.md` and `results.json`; the full registry can produce nonzero coverage exit codes even if the targeted browser case passes.

## 5. Explicit non-goals

This patch does **not** unlock a new-assignment workflow, fix scheduled Reservation Visit fixtures for `FRMS-ST-0067`, verify cross-facility F32 permissions, resolve the two unregistered-identity fixtures, or declare all 101 implemented cases PASS.
