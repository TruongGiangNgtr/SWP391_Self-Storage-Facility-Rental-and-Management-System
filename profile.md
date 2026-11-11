# FRMS Automated System Testing — Profile

**Project:** SWP391 Self-Storage Facility Rental and Management System  
**Branch:** `TEST_SWT`  
**Refreshed:** 2026-11-11  
**Latest attached DB snapshot (UTC):** `2026-11-11T06:30:28.186617Z`  
**Classification:** Internal test metadata. Never commit raw DB ZIPs, credentials, tokens or connection strings.

**Evidence language:** `REPO_VERIFIED` means checked in current committed source, `SNAPSHOT_VERIFIED` means extracted from the user-provided read-only sanitized PostgreSQL files, `USER_REPORTED_RUNTIME` is a user-supplied previous run, and `PENDING_RUNTIME` needs fresh authenticated API/Playwright confirmation. A database snapshot is **not** a live test run.

## 1. Repository, runtime and file map

| Item | Location / value |
| --- | --- |
| GitHub | `TruongGiangNgtr/SWP391_Self-Storage-Facility-Rental-and-Management-System` |
| Worktree | `C:\Users\buidi\OneDrive\Desktop\Git\SWP391\SWP391_Self-Storage-Facility-Rental-and-Management-System` |
| Branch | `TEST_SWT` (do not use `swt_test` or `main`) |
| API / test frontend | `http://127.0.0.1:5164` / `http://127.0.0.1:4173` |
| Test runtime | Windows CMD, PowerShell 5.1, Node/pnpm, Playwright Chromium, .NET 10 |
| Preferred entrypoint | `RUN_FRMS_TESTS.cmd` |
| Runner chain | `RUN_FRMS_TESTS.ps1` → `run-frms-tests.ps1` |
| Defaults | Testing, 4 Playwright workers, 1 retry, `CI=true` |
| Registry | `tests/system/manifest/cases.json` |
| Specs | `tests/e2e/playwright/tests/frms/` |
| Test result reports | `tests/system/reports/<run-id>/` |
| PostgreSQL profile exporter | `test/tools/FRMS_DB_PROFILE_EXPORT/RUN_EXPORT.cmd` |
| Profile export reports | `test/reports/db-profile/` (Git ignored) |
| Transfer to another AI | `test/handoff/README.md`, `test/handoff/context.json`, `test/handoff/fixtures.json` |

Note the difference: `test/` holds tools/handoff/DB metadata; `tests/` holds runnable test code and Playwright reports.

## 2. Automation coverage and historical runtime evidence

The committed registry contains **275 unique system testcases in 12 areas**:

| Functional area | Registered | Implemented | Not implemented | Review |
| --- | ---: | ---: | ---: | ---: |
| Authentication & Security | 51 | 39 | 12 | 0 |
| Storage Search & AI Size Guide | 19 | 15 | 4 | 0 |
| Reservation | 22 | 8 | 14 | 0 |
| Handover | 29 | 17 | 12 | 0 |
| Facility Operations | 10 | 6 | 4 | 0 |
| Access Visits | 25 | 13 | 9 | 3 |
| Billing & Payments | 23 | 10 | 13 | 0 |
| Renewal | 16 | 3 | 13 | 0 |
| Return & Settlement | 22 | 2 | 20 | 0 |
| Support Tickets | 25 | 8 | 12 | 5 |
| Administration | 19 | 7 | 12 | 0 |
| Pricing & Reference Data | 14 | 10 | 4 | 0 |
| **TOTAL** | **275** | **138** | **129** | **8** |

- **Implementation coverage:** 138/275 = **50.18%** (`REPO_VERIFIED`).
- **Historical user-reported result:** 51/51 implemented tests PASS, run `20261110T151721934Z`. This does **not** establish current success.
- **Batch 12:** 50 historical mappings (101 at that baseline), without a complete verified Batch 12 PASS. **Batch 13:** 37 new mappings (138 total), no runtime PASS yet.
- The manifest still has `oracleApproval=PENDING_SRS_REVIEW`. `IMPLEMENTED` does not mean oracle-approved or PASS.
- Missing valid fixtures → `Blocked/Skipped`; code/test issues → `UnconfirmedFailure`; others `Review` or `Not Run`. Only independently confirmed, reproducible product defects belong in Jira.

## 3. Database safety and access conventions

The last-known API configuration targets **remote/shared Supabase PostgreSQL**. `ASPNETCORE_ENVIRONMENT=Testing` is a process setting, **not** database isolation. `ConnectionStrings__Frms` may take precedence over `ConnectionStrings__FrmsDb`; both must be checked privately in `backend/Frms.Api/.env` without printing secrets.

- Limit runs on shared data to authorized read-only inspection, negative frontend validation and bounded authentication. Login may write `LoginHistory`.
- Billing, NoShow, Notifications job flags can be disabled, but Reservation Expiration is not controlled by those three flags. Do not launch an API process against shared data without reviewing that job.
- No real Reservation creation/check-in, handover assignment save/release, renewal, invoice/payment mutation, migrations, account modifications or seeding to manufacture test preconditions.
- The exporter uses `READ ONLY` transactions and allowlisted metadata fields. Its ZIP may still include linkable business IDs and should stay internal.
- The stored profile and handoff contain no live DB password, JWT, payOS token or raw user phone/email table.

## 4. New PostgreSQL snapshot from uploaded files

Files examined (not committed): `FRMS_PROFILE_SAFE_20261111T063028Z.zip` and `FRMS_PROFILE_SUMMARY_20261111T063028Z.json`. Their manifest and candidate JSON agree. Export timestamp: **2026-11-11 06:30:28 UTC**.

**Inventory:** 29 tables surveyed; 28 exported; 1 sensitive `AuditLog` table skipped; **1,552 metadata rows** from allowlisted columns. The ZIP passed integrity checking. No full database dump or personally identifiable credential fields were imported.

| Table | Rows | States (where available) |
| --- | ---: | --- |
| Facility | 63 | 63 ACTIVE |
| UserAccount | 132 | 131 ACTIVE, 1 INACTIVE |
| Employee / Customer | 128 / 4 | Safe reference identifiers only |
| StorageUnit | 386 | 378 AVAILABLE, 3 MAINTENANCE, 3 INSPECTION, 2 IN_USE |
| Reservation | 12 | 3 CONFIRMED, 6 COMPLETED, 2 PENDING_DEPOSIT, 1 CANCELLED |
| Visit | 21 | 6 CHECKED_IN, 10 CHECKED_OUT, 4 SCHEDULED, 1 CANCELLED |
| HandoverUnitAssignment | 8 | 2 ACTIVE, 6 CONSUMED |
| Contract | 6 | 3 ACTIVE, 3 TERMINATED, **0 COMPLETED** |
| Invoice | 22 | 19 PAID, 3 OVERDUE |
| Payment | 18 | 15 SUCCESS, 3 PENDING |
| Inspection | 4 | 1 IN_PROGRESS, 3 COMPLETED |
| SupportTicket | 2 | 2 COMPLETED |
| LoginHistory | 613 | 589 SUCCESS, 24 FAILED |
| NotificationLog | 73 | 73 PENDING |

**Important delta from the earlier 04:40:10 UTC snapshot:** the earlier claim that no matching Manager F01 Reservation–Visit–Unit triple exists is no longer correct. One candidate is present, but it is **already assigned**. Also, the known Staff handover Visit changed from SCHEDULED to CHECKED_IN.

## 5. Manager F01 assignment candidate — nuanced validity

The export reports one `assignable_manager_f01` triple. Cross-checking raw safe rows adds a **critical existing ACTIVE assignment**:

| Item | ID | Snapshot state |
| --- | --- | --- |
| Facility F01 | `f0000000-0000-0000-0000-000000000001` | ACTIVE |
| Reservation | `f1112fee-e99f-4094-879a-563ddd1f8f81` | CONFIRMED |
| Visit | `6855fba5-942c-40a4-bf22-5658ca3be00c` | RESERVATION / CHECKED_IN, 2026-12-05 |
| UnitType | `b1000000-0000-0000-0000-000000000001` | Same type as Reservation |
| Physical Unit | `fa72ca32-22d6-496c-aca2-20e64c040de7` | A-102 / AVAILABLE |
| HandoverUnitAssignment | `d9e3672e-9708-46de-96a9-72a860f55065` | **ACTIVE**, already links Reservation to Unit |

This **proves a matching tuple exists in the snapshot**, not that a *new* assignment can be performed. The frontend `HandoverUnitSelectionPage.tsx` filters `CONFIRMED`, `RESERVATION` Visit `CHECKED_IN`, and same-facility/same-type `AVAILABLE` units, and also reads the current Assignment. The committed `FRMS-ST-0065` test checks the read-only UnitType match and a visible heading, **not** assignment creation. After authenticated Manager F01 GET verification and SRS oracle review, the tuple may be useful for that read-only assertion, but **do not save or release assignments**.

**Launcher fix prepared:** `RUN_FRMS_TESTS.ps1` now includes `FRMS_TEST_ASSIGNABLE_RESERVATION_ID=f1112fee-e99f-4094-879a-563ddd1f8f81` as a snapshot default and removes this ID from the previous `knownIneligible` list. The existing `FRMS_TEST_AVAILABLE_PHYSICAL_UNIT_ID=fa72ca32-22d6-496c-aca2-20e64c040de7` and F01 Facility ID already match. **Latest targeted console run reports PASS; verify local execution SHA and report artifacts for stronger provenance (see final section).** `FRMS-ST-0065` now uses only GET requests and page navigation to verify the Reservation, RESERVATION/CHECKED_IN Visit, AVAILABLE matching Unit, and the pre-existing ACTIVE Assignment. Do not use this fixture for a NEW assignment or save/release.

Alternative Reservation `b370c765-adb7-4e16-aa19-54caf7d5b788` remains CONFIRMED + CHECKED_IN but its F01 UnitType `...0002` has 2 IN_USE + 1 INSPECTION and **0 AVAILABLE** physical units. It does not satisfy the matching-available-unit requirement.

## 6. Staff fixture drift and unresolved identity inputs

- **SCHEDULED visit blocker:** `FRMS_TEST_STAFF_HANDOVER_VISIT_ID=6855fba5-942c-40a4-bf22-5658ca3be00c` now has `VisitType=RESERVATION`, `Status=CHECKED_IN`. The snapshot has **0 RESERVATION + SCHEDULED visits**; the other four SCHEDULED Visits are ACCESS (3) or RETURN (1). Tests needing a pre-check-in Reservation Visit must remain Blocked until a genuine one exists, with no database mutation to create it.
- **Cross-facility scope:** `FRMS_TEST_OTHER_FACILITY_REFERENCE_ID=33a922ce-416f-4757-87f5-8f85aba047ef` is a CHECKED_IN Reservation Visit at F32, date `2026-12-05`. ST-0165 has a user-supplied targeted console PASS; local code revision and F32-positive-control execution have not been independently confirmed. The own F01 scope date `2026-12-01` differs, so a negative check against only that date could be misleading.
- **Negative login inputs (FRMS-ST-0199, FRMS-ST-0203):** `FRMS_TEST_UNREGISTERED_CUSTOMER_PHONE` and `FRMS_TEST_UNREGISTERED_EMPLOYEE_EMAIL` are now verified automatically by `tests/system/scripts/batch12-verify-unregistered.mjs` after the API preflight and before Playwright. The verifier authenticates Admin, checks ACTIVE role and **all** paginated account rows, rejects incomplete/duplicate directory pages, and sets both process-only values only if absent. The sanitized export cannot prove absence; if Admin verification is unavailable, both cases remain Blocked. No account is created. Runtime verification of this new process is pending.
- **Other missing preconditions:** no COMPLETED Contract in snapshot; no verified no-CCCD demo account. Optional fixture installer folders are not proof they are applied.
- **Discovery mismatch:** committed `tests/system/scripts/batch12-discover.mjs` defaults to Staff F02 for foreign discovery while wrapper knows Staff F32. Check code before applying optional `test/tools/FRMS_BATCH12_*/install.mjs`.

### Values already embedded as runner snapshot defaults

These **17** environment defaults were cross-referenced with the new export, but their presence is not a runtime PASS, permission check or current availability guarantee.

| Environment name | Current runner default | Snapshot observation |
| --- | --- | --- |
| `FRMS_TEST_ACTIVE_CONTRACT_ID` | `91000000-0000-0000-0000-000000000001` | ACTIVE Contract |
| `FRMS_TEST_AVAILABLE_PHYSICAL_UNIT_ID` | `fa72ca32-22d6-496c-aca2-20e64c040de7` | AVAILABLE; A-102, F01, UnitType ...0001; already linked to ACTIVE assignment |
| `FRMS_TEST_CUSTOMER_2_INVOICE_ID` | `83000000-0000-0000-0000-000000000003` | OVERDUE Invoice |
| `FRMS_TEST_CUSTOMER_2_RESERVATION_ID` | `81000000-0000-0000-0000-000000000002` | COMPLETED Reservation |
| `FRMS_TEST_CUSTOMER_2_SUPPORT_TICKET_ID` | `ffcfa55e-9512-4f62-9f83-34ba8c15b98d` | COMPLETED ticket |
| `FRMS_TEST_HANOI_FACILITY_ID` | `f0000000-0000-0000-0000-000000000001` | ACTIVE Facility 01 |
| `FRMS_TEST_HANOI_FACILITY_NAME` | `FRMS Facility - Hà Nội` | ACTIVE Facility 01 |
| `FRMS_TEST_OTHER_FACILITY_REFERENCE_ID` | `33a922ce-416f-4757-87f5-8f85aba047ef` | CHECKED_IN Reservation Visit at F32, date 2026-12-05; authorization not verified |
| `FRMS_TEST_OWN_DEPOSIT_INVOICE_ID` | `2383e8b2-2f51-43c7-88cb-def8a359fe2b` | PAID Invoice |
| `FRMS_TEST_OWN_PAID_INVOICE_ID` | `83000000-0000-0000-0000-000000000001` | PAID Invoice |
| `FRMS_TEST_OWN_RESERVATION_ID` | `b370c765-adb7-4e16-aa19-54caf7d5b788` | CONFIRMED, CHECKED_IN Visit, no matching AVAILABLE UnitType ...0002 at F01 |
| `FRMS_TEST_PRICING_UNIT_TYPE_NAME` | `Medium Private (5x10)` | Reference UnitType name; API authorization still pending |
| `FRMS_TEST_STAFF_HANDOVER_RESERVATION_ID` | `f1112fee-e99f-4094-879a-563ddd1f8f81` | CONFIRMED; associated Visit now CHECKED_IN, not SCHEDULED |
| `FRMS_TEST_STAFF_HANDOVER_VISIT_ID` | `6855fba5-942c-40a4-bf22-5658ca3be00c` | CHECKED_IN; INVALID for pre-check-in SCHEDULED test |
| `FRMS_TEST_STAFF_IN_PROGRESS_INSPECTION_ID` | `8284175e-729e-4070-931e-3d44deb21ec0` | IN_PROGRESS Inspection |
| `FRMS_TEST_STAFF_OWN_REFERENCE_ID` | `0c5eb20c-ec50-4e2f-a633-e0cea07eeadf` | CHECKED_IN Visit, Staff F01 |
| `FRMS_TEST_STAFF_SCOPE_DATE` | `2026-12-01` | 2026-12-01; needs current work-list confirmation |

Additional defaults: `FRMS_TEST_OTHER_FACILITY_SCOPE_DATE=2026-12-05`; `FRMS_TEST_TERMINATED_CONTRACT_ID=b2447e82-3493-44e7-8678-cd9255ea8d72` (TERMINATED). **The wrapper now additionally sets `FRMS_TEST_ASSIGNABLE_RESERVATION_ID` for the read-only ST-0065 scenario.** The two unregistered-identity keys are still not configured by snapshot defaults.

## 7. Unit Type reference data (2026-11-10 API reference)

The following is transcribed from the two user-supplied API responses dated 2026-11-10: **7 PRIVATE unit types and 1 PUBLIC unit type**. Values are exact API fields, not assumed inventory availability.

| Unit Type ID | Name | Mode | API size string | API `rentalPrice` |
| --- | --- | --- | --- | ---: |
| `b1000000-0000-0000-0000-000000000008` | Commercial Warehouse (10x25) | PRIVATE | `23 m² (10x25 ft)` | 36000 |
| `b1000000-0000-0000-0000-000000000006` | Extra Large Private (10x15) | PRIVATE | `14 m² (10x15 ft)` | 22000 |
| `b1000000-0000-0000-0000-000000000004` | Large Private (10x10) | PRIVATE | `10 m² (10x10 ft)` | 16000 |
| `b1000000-0000-0000-0000-000000000005` | Medium Plus Private (5x15) | PRIVATE | `7 m² (5x15 ft)` | 12000 |
| `b1000000-0000-0000-0000-000000000002` | Medium Private (5x10) | PRIVATE | `5 m² (5x10 ft)` | 9000 |
| `b1000000-0000-0000-0000-000000000001` | Small Private (5x5) | PRIVATE | `2 m² (5x5 ft)` | 2000 |
| `b1000000-0000-0000-0000-000000000007` | Warehouse Private (10x20) | PRIVATE | `18 m² (10x20 ft)` | 28000 |
| `b1000000-0000-0000-0000-000000000003` | Shared Public Locker (5x5) | PUBLIC | `3 m² equivalent (Locker)` | 3500 |

**Clarification:** `mode=PUBLIC` refers to shared storage, **not** public visibility of the catalog. A guest-facing catalog can show both `PRIVATE` and `PUBLIC` types. The listed unit types do not prove availability at any particular facility.

### Proposed catalog fixture: Shared Public Locker (5x5)

Only use it as an available-card fixture **after** verifying that an ACTIVE facility offers this type with positive capacity in the tested months.

```bat
rem CMD. Set UTF-8 before assigning the size value containing m2 superscript.
chcp 65001 >NUL
set "FRMS_TEST_AVAILABLE_UNIT_TYPE_ID=b1000000-0000-0000-0000-000000000003"
set "FRMS_TEST_AVAILABLE_UNIT_NAME=Shared Public Locker (5x5)"
set "FRMS_TEST_AVAILABLE_UNIT_MODE=PUBLIC"
set "FRMS_TEST_AVAILABLE_UNIT_SIZE=3 m² equivalent (Locker)"
set "FRMS_TEST_AVAILABLE_UNIT_RENT_VND=3500"
```

`FRMS_TEST_AVAILABLE_UNIT_TYPE_ID` is reference metadata; the current `catalog.spec.ts` does **not** read that variable. The values **read** by the test are unit name, size, mode, rent, and ACTIVE facility name (`FRMS_TEST_ACTIVE_FACILITY_NAME`). The optional `FRMS_TEST_AVAILABLE_UNIT_DESCRIPTION` can be supplied if needed.

Do not set `FRMS_TEST_ACTIVE_FACILITY_NAME` to a placeholder or invent one. Obtain the facility name from the facility-specific `/facilities` and `/facilities/{facilityId}/unit-types` responses and verify `availableCapacity > 0` for the period. `FRMS-ST-0027` uses one next-month period; `0028` uses a next-month to following-month period.


## 8. Actors and local environment

Customer authenticates by **PHONE**: `0901000001`, `0901000002`, `0901000003`. Staff / other employee roles authenticate by **EMAIL**: `staff.f01@example.com`, `staff.f32@example.com`, `manager.f01@example.com`, `bom@example.com`, `admin@example.com`. Every actor must be ACTIVE, correctly assigned and verified by the running backend before a role-sensitive test is considered valid.

```bat
rem Windows CMD at repository root. Never commit actual passwords:
set "FRMS_ENVIRONMENT=Testing"
set "FRMS_API_URL=http://127.0.0.1:5164"
set "FRMS_UI_URL=http://127.0.0.1:4173"
set "VITE_API_PROXY_TARGET=http://127.0.0.1:5164"
set "CI=true"
rem set "FRMS_TEST_CUSTOMER_1_PASSWORD=<local secret>"
rem set "FRMS_TEST_STAFF_PASSWORD=<local secret>"
```

The wrapper has demo defaults for local usage; override them using local process environment or CI secrets rather than writing credentials into this profile.

## 9. Running tests and refresh exports

```bat
cd /d "C:\Users\buidi\OneDrive\Desktop\Git\SWP391\SWP391_Self-Storage-Facility-Rental-and-Management-System"
git branch --show-current
git status --short
rem Confirm already-running authorized API:
curl http://127.0.0.1:5164/health

.\RUN_FRMS_TESTS.cmd -AuditOnly
.\RUN_FRMS_TESTS.cmd -Mode coverage
.\RUN_FRMS_TESTS.cmd -Mode case -CaseId FRMS-ST-0165 -Workers 1 -Retries 0
.\RUN_FRMS_TESTS.cmd -Mode all -Workers 4 -Retries 1

rem Read-only PostgreSQL metadata exporter with isolated Python venv:
.\test\tools\FRMS_DB_PROFILE_EXPORT\RUN_EXPORT.cmd
```

Exporter output: `test/reports/db-profile/FRMS_PROFILE_SAFE_<UTC>.zip` and `FRMS_PROFILE_SUMMARY_<UTC>.json`, excluded by `test/reports/.gitignore`. Playwright results: `tests/system/reports/<run-id>/`. The exporter is **not** a testcase run and does not establish PASS.

Do not start a backend against shared Supabase merely to run the wrapper: Reservation Expiration is not controlled by the other three disabled background-job flags. See `test/handoff/RUNBOOK.md`.

## 10. Next acceptance checks

1. **ST-0065 fixture prepared in runner and read-only test assertions updated.** Run the case with Manager F01 against the authorized API; verify current Reservation, Visit, Unit and ACTIVE Assignment via GET. The outdated `knownIneligible` guard was removed for the verified snapshot ID. No save/release PUT/DELETE.
2. Keep scheduled Reservation Visit tests Blocked; the new snapshot has none. Do not repurpose CHECKED_IN visits as SCHEDULED.
3. Verify F32 foreign Visit access and F01 denial using correct Visit date; avoid false positives from mismatched dates.
4. Run `FRMS-ST-0199` and `FRMS-ST-0203` after syncing this fix. The launcher now verifies unregistered identifiers from the full Admin directory without logging existing account details. `-AuditOnly` is a static inventory and intentionally cannot prove absence.
5. After authorization and fixture checks, run targeted tests, then the mapped suite and inspect `summary.md`/`results.json`. Never report 138 PASS from a registry count.

## 11. Evidence and source ownership

- DB snapshot inputs (user-uploaded; **not in Git**): `FRMS_PROFILE_SAFE_20261111T063028Z.zip`, `FRMS_PROFILE_SUMMARY_20261111T063028Z.json`.
- Requirements/oracles: `docs/FRMS_SRS_V11.md` and `tests/system/manifest/cases.json`.
- Actual UI rule: `frontend/src/features/handover-unit-selection/HandoverUnitSelectionPage.tsx`.
- Runner and fixture defaults: `RUN_FRMS_TESTS.ps1`, `run-frms-tests.ps1`, `tests/system/scripts/batch12-discover.mjs`; case source: `tests/e2e/playwright/tests/frms/batch12-staff.spec.ts`. Scenario instructions: `test/handoff/FRMS-ST-0065.md`.
- Automation manual and history: `tests/system/README.md`, `tests/system/PATCH_HISTORY.md`.
- Structured handoff: `test/handoff/context.json`, `fixtures.json`, `EVIDENCE.md`, `HANDOFF.md`, `NEXT_STEPS.md`.

This profile supersedes the previous stale **30 implemented / 20 PASS / 1 worker** snapshot narrative. Updates to `TEST_SWT` should be **one commit per coherent change batch**. No documentation update is proof of a runtime PASS.

### ST-0065 prepared command (CMD)

```bat
.\RUN_FRMS_TESTS.cmd -AuditOnly
.\RUN_FRMS_TESTS.cmd -Mode case -CaseId FRMS-ST-0065 -Workers 1 -Retries 0 -SkipCatalogDiscovery
```

The wrapper supplies the snapshot Reservation and matching AVAILABLE Unit automatically. API role/permissions and current state are **not** guaranteed. `-SkipCatalogDiscovery` skips unrelated catalogue fixture lookups, not the Batch 12 discovery pass. Runtime output must be checked in `tests/system/reports/<run-id>/summary.md` and `results.json`. The previous historical PASS count remains 51/51.

### Negative login fixture verification — prepared commands

Use the already-running authorized API, then run from the repository root:

```bat
.\RUN_FRMS_TESTS.cmd -Mode case -CaseId FRMS-ST-0199 -Workers 1 -Retries 0 -SkipCatalogDiscovery
.\RUN_FRMS_TESTS.cmd -Mode case -CaseId FRMS-ST-0203 -Workers 1 -Retries 0 -SkipCatalogDiscovery
```

Only the actual targeted runs invoke Admin verification. The verifier may generate normal LoginHistory records through authentication. Success log: `Verified negative login fixtures: phone and email absent from N complete Admin account records`. If verification fails, both variables are cleared from the test child process and the cases must remain Blocked. `-AuditOnly` still lists these keys as unconfigured because it does not contact the API. In a non-negative targeted case (e.g. FRMS-ST-0065) the two keys are excluded from its applicable fixture audit. Reports: `tests/system/reports/<run-id>/`.

## Static test-oracle hardening — 2026-11-11 (runtime pending)

On `TEST_SWT`, `FRMS-ST-0165` now has an actual F32 Staff positive control: ACTIVE F01/F32 accounts must belong to distinct Facilities, F32 must read the nominated RESERVATION Visit and see it in F32 work items on `FRMS_TEST_OTHER_FACILITY_SCOPE_DATE`, F01 must see its own work item, and only then F01 is asserted to exclude the F32 Visit on **that same foreign date** and reject foreign Visit detail (403/404). The discovery code uses `FRMS_TEST_FOREIGN_STAFF_EMAIL` (F32, not the outdated F02), searches own/foreign dates independently and does not replace explicit snapshot overrides. Missing/stale preconditions are `FRMS_FIXTURE_MISSING`, not PASS or Jira defects.

`FRMS-ST-0065` retains its GET-only checks for the pre-existing ACTIVE assignment and now also selects the matching Reservation **in the Manager UI** and confirms the saved Unit code and `Saved` state. No save/release click or data mutation is introduced.

**Evidence:** REPO_VERIFIED static source review only. Local Playwright, authenticated API, and database runtime are not accessible through the GitHub connector; `FRMS-ST-0065` and `FRMS-ST-0165` remain **PENDING_RUNTIME / Not Run** for this change. Historical 51/51 user-reported PASS and two separately reported negative-login PASS remain unchanged. No application, DB, or `main` changes.

## Targeted runtime results received — 2026-11-11

**Evidence level: USER_REPORTED_RUNTIME_CONSOLE** (user-pasted complete Windows CMD/Playwright console; not independently retrieved `results.json` or `summary.md`).

| Testcase | Run ID | Console result | Launcher / wrapper exit |
| --- | --- | --- | --- |
| `FRMS-ST-0065` | `20261111T080719965Z` | **1/1 PASS**, 15.2 seconds; 1 worker, 0 retries | 0 / 0 |
| `FRMS-ST-0165` | `20261111T081112172Z` | **1/1 PASS**, 5.7 seconds; 1 worker, 0 retries | 0 / 0 |

Both runs: backend preflight OK, six primary actor roles verified by discovery, no state-change flags, Jira CSV 0 independently confirmed defects. **Exact local commit SHA at execution is not supplied.** Therefore, the console confirms targeted runtime success, but does *not yet establish* that the patched ST-0065 saved Unit-code/Saved UI oracle and patched ST-0165 F32 positive-control/correct-date oracle were the versions executed. Compare `git rev-parse HEAD` from the original worktree with the known baseline code SHA `81babb4`; retrieve `tests/system/reports/<run-id>/{summary.md,results.json}` privately before promoting to `REPORT_VERIFIED`.

The warning `Staff cross-facility: no same-date other-facility work item pair confirmed` is **stale diagnostic wording** in GitHub TEST_SWT's discovery code, whose date-aware search no longer requires a same-day pair; the wrapper's snapshot defaults may have supplied the foreign Visit separately. `Batch 12 fixture discovery: 0 verified value(s) supplied` indicates no newly injected variables, not proof that all existing defaults are current. These warnings do not negate Playwright PASS, but source revision and real fixture control should be audited. `FRMS-ST-0067` remains Blocked (no RESERVATION/SCHEDULED Visit).

Keep historic **51/51 user-reported regression**, ST-0199/0203 reported PASS (without run IDs) and these two targeted run IDs as **separate** evidence. Do not merge their counts or imply whole Batch 12 / 101-case PASS; oracleApproval remains pending SRS review.

## Batch 13 — 50.18% implemented coverage (2026-11-11)

The `TEST_SWT` registry now contains **275 unique cases: 138 IMPLEMENTED (50.18%), 129 NOT_IMPLEMENTED, 8 REVIEW**. Batch 13 adds 37 distinct case IDs mapped to five new Playwright specs and a source-checked fixture helper. Read `test/handoff/BATCH13_COVERAGE.md` for exact cases, fixture gates and commands.

**Evidence: REPO_VERIFIED_SOURCE_MAPPING_ONLY; PENDING_RUNTIME**. No new Playwright runtime PASS, TypeScript compilation, approved SRS oracle or Jira defect was produced by this change. Historical 51/51 and individual user-reported ST-0199, ST-0203, ST-0065, ST-0165 remain separate evidence. Both new access Visit creation cases require an isolated disposable DB and `isolatedWrite()`; all new shared-safe cases are authentication/GET/browser-only. Foreign physical Unit tests require an authenticated real foreign Manager positive control; do not invent fixtures. Missing preconditions remain Blocked.

## Batch 13 coverage validation and first runtime failure (2026-11-11)

**Coverage audit:** Windows `.\\RUN_FRMS_TESTS.cmd -Mode coverage` returned `exit code 0`; `validate-coverage.mjs` printed **275 registered / 275 unique / 138 implemented / 8 review / 129 outstanding** and `Registry structure and executable mappings valid`. Evidence: `USER_REPORTED_RUNTIME_CONSOLE`; 50.18% means implementation mapping only, **not** runtime PASS.

**FRMS-ST-0183-B** — run `20261111T084456202Z`, 1 worker, 0 retries, **1 executed / 1 failed**, runner and wrapper exit code **1**. The first page-header assertion in `batch13-pricing.spec.ts:11` failed with Playwright **strict mode violation**: `getByRole('heading', {name:'Unit Type Pricing'})` matched both page `<h1>Unit Type Pricing</h1>` and `<h2>Global Unit Type Pricing Registry</h2>`. The frontend implementation was not shown to be defective; the test did not reach the numeric input validator. **Classify as `AUTOMATION_DEFECT` / `USER_REPORTED_RUNTIME_CONSOLE`, not confirmed product defect, and do not import to Jira.** The log reports Jira CSV **0 independently confirmed defects**.

**Fix in TEST_SWT:** select exactly the page heading with `page.getByRole('heading',{name:'Unit Type Pricing',exact:true,level:1})`; source reviewed against frontend heading markup. Similar Batch 13 heading checks were reviewed for this substring ambiguity; no other confirmed duplicate was identified from supplied source. This is a code-only oracle fix, not a new PASS. **Rerun required** after pulling the patch and inspect `tests/system/reports/<new-run-id>/summary.md` and `results.json`. Historical 51/51 plus earlier separately reported ST-0065, ST-0165, ST-0199 and ST-0203 remain unchanged. No app, DB, or `main` change.

## Full-run Batch 13 evidence — 2026-11-11

**Run `20261111T085924993Z`**, user-pasted `summary.md` content: 275 selected/registered, 138 implemented mappings executed, **95 Pass, 7 Flaky, 18 Blocked (14 PLAYWRIGHT_SKIPPED, 4 TEST_FIXTURE_OR_INFRA), 18 UnconfirmedFailure, 8 Review, 129 Not Run**. Stable PASS among implemented = **68.84%**; implementation coverage remains **50.18%**. Evidence level `USER_REPORTED_SUMMARY_TEXT`, not independently `REPORT_VERIFIED`; the actual `results.json` and local HEAD were not supplied. See `test/handoff/RUN_20261111T085924993Z.md` and the machine-readable JSON for complete per-ID classifications. **Zero confirmed Jira product defects from this evidence.** Do not merge historical 51/51 or individual earlier runs into these counts. Neither tests nor application/DB changed.

## Automation repairs from uploaded full-run results — 2026-11-11

Received and parsed **user-uploaded `results(1).json`** for `20261111T085924993Z`. It reports exact execution SHA `54905be56c96fcc9e6a148e32a678197c60f1447`, environment Testing and historical **95 Pass / 7 Flaky / 18 Blocked / 18 UnconfirmedFailure / 8 Review / 129 Not Run**. The source-only repair touches **17 testcase IDs**, adds Staff/Inspection positive fixture controls, fixes deterministic selectors/type assertions and reduces repeated in-scenario auth verification. Details: `test/handoff/FIX_RUN_20261111T085924993Z.md`. **New runtime: NOT RUN; all old statuses unchanged**. API /facilities 500, /health 503, login 500/timeouts and missing state fixtures remain unresolved. No Jira product defect confirmed; 138/275 IMPLEMENTED remains 50.18%.

## Targeted reruns after automation repair — 2026-11-11

**`USER_REPORTED_RUNTIME_CONSOLE`**: four independent single-case, 1-worker, 0-retry runs after fast-forward pull to `8826f5e` each reported **1 selected / 1 executed / 1 PASS / exit code 0**: `FRMS-ST-0081` (`20261111T094923472Z`), `FRMS-ST-0156` (`20261111T095025709Z`), `FRMS-ST-0235` (`20261111T095126418Z`), `FRMS-ST-0183-B` (`20261111T095244321Z`). Coverage validation PASS with **138/275 implemented (50.18%)**. Historical full-run 95 PASS stays unchanged; no independent report JSON, no new aggregate full-suite PASS, and no Jira defects confirmed. Staff SCHEDULED Visit fixture still missing. Details: `test/handoff/TARGETED_RERUNS_20261111_AFTER_8826f5e.md`.

## Targeted reruns after 1547d67 (2026-11-11)

USER_REPORTED_RUNTIME_CONSOLE: three targeted 1/1 PASS (ST-0160 run 20261111T095951885Z; ST-0085 run 20261111T100054156Z; ST-0184 run 20261111T100258194Z), and one automation locator FAIL (ST-0141 run 20261111T100152311Z). All were run with 1 worker, 0 retries after a reported fast-forward pull. ST-0141's compound Facility label caused getByLabel('Facility',{exact:true}) to find nothing; source now targets the dialog-scoped Facility combobox and verifies presence for Facility Manager / absence for BOM. Fix PENDING_RUNTIME; no ST-0141 PASS claimed. Details: test/handoff/TARGETED_RERUNS_20261111_AFTER_1547d67.md.

Historical full run remains 95 Pass / 7 Flaky / 18 Blocked / 18 UnconfirmedFailure / 8 Review / 129 Not Run. Registry unchanged: 138/275 IMPLEMENTED (50.18%). Across both post-repair targeted groups, seven distinct cases separately PASS; do not sum these into full-run counts. Zero confirmed Jira product defects.

## FRMS-ST-0141 targeted rerun PASS — 2026-11-11

**USER_REPORTED_RUNTIME_CONSOLE:** `FRMS-ST-0141` run `20261111T101339028Z` reports **1 selected, 1 executed, 1 PASS (7.6s)** with one worker, zero retries and launcher/wrapper exit code 0. This is separate from the prior `20261111T100152311Z` automation FAIL and from full-run `20261111T085924993Z`. The actual executing SHA and results.json are not supplied. **Eight distinct repaired case IDs now have individual targeted PASS evidence; nine of 17 still lack a fresh successful targeted rerun.** No Jira defects confirmed. Historical full run remains 95 PASS, and registry coverage remains 138/275 (50.18%). See `test/handoff/TARGETED_RERUN_FRMS-ST-0141_20261111T101339028Z.md`.

## Static re-verification audit — 2026-11-11

At `TEST_SWT` commit `d84eb3d`, all **275 manifest IDs are unique** and all **138 IMPLEMENTED** cases have exactly one matching executable Playwright title across 16 declared specs (no unmapped titles). Registered: 138 IMPLEMENTED / 129 NOT_IMPLEMENTED / 8 REVIEW; coverage still **50.18%**. **All 275 SRS oracles remain PENDING_SRS_REVIEW**; 53 sourceKey values contain trailing backticks (traceability cleanup pending). Of the 17 previously repaired cases, 8 have distinct user-console targeted PASS and 9 still lack successful new runs. Static source/original-workbook comparison flags **oracle/traceability gaps** in invoice snapshot/payment, invalid ticket category API, legacy price minimum, old access time-slot behavior, and several partial UI assertions. **No tests or registry expectations were modified and no new runtime PASS was claimed.** See `test/handoff/TESTCASE_REVERIFICATION_20261111.md` for the 17-case matrix and approved-SRS decisions needed. Historical full run remains 95 PASS.
