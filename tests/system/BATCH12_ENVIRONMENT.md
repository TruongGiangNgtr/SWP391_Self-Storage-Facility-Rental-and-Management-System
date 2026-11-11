# FRMS Batch 12: +50 automated cases on TEST_SWT

**Scope:** 275 registered test cases, previous baseline **51 implemented / 51 passed** in the user-reported run `20261110T151721934Z`. This patch adds **exactly 50 separate `test()` declarations**, taking the automation registry to **101 implemented (36.73%)**, 166 not implemented, 8 review. **No new runtime PASS is claimed.** New cases require execution on the user's Windows `TEST_SWT` working tree; the handoff archive does not include the current checked-out application or secrets.

## Sources and safety

- Reference: handoff `config/profile.md`, original `references/FRMS_Testcase_Final.md`, `tests/system/manifest/cases.json`, frontend/backend `main` as the implementation oracle where accessible.
- **Customer authenticates by PHONE**; Staff and other employee roles authenticate by EMAIL. Existing user-confirmed demo accounts: Customer 1 `0901000001` and Staff 01 `staff.f01@example.com`. Their demo password is sourced locally through environment variables. **Manager/BOM/System Admin account names were NOT confirmed by the profile and are never guessed by the new test helper.**
- The profile's `.env` points at **remote/shared Supabase**. `ASPNETCORE_ENVIRONMENT=Testing` does not make it disposable. **All 50 new cases are read-only, client-side negative validations, role-access checks, or an injected AI network-failure test.** No test sends an application-state-changing successful reservation, renewal, handover, ticket, price update, payment or admin action.
- API auth login calls and GET requests are used for session/precondition setup. Browser sign-in negatives hit the real login endpoint. The AI fallback test locally aborts only `**/api/v1/ai/**` using Playwright: this proves browser fallback rendering, not a real service outage.
- Do not run real mutation tests against shared Supabase. For **future** mutation batches only: separate disposable PostgreSQL Testing DB, migrated/seeded data, backup/snapshot, rollback/cleanup plan and, where relevant, payOS sandbox; `FRMS_TEST_DB_ISOLATED=true` and `FRMS_ALLOW_STATE_CHANGES=YES` are two **manual opt-ins**, not proof of DB isolation.

## Core environment: Windows CMD, set in the test terminal

```bat
set "FRMS_ENVIRONMENT=Testing"
set "FRMS_API_URL=http://127.0.0.1:5164"
set "FRMS_UI_URL=http://127.0.0.1:4173"
set "VITE_API_PROXY_TARGET=http://127.0.0.1:5164"
set "CI=true"
set "FRMS_TEST_CUSTOMER_1_PHONE=0901000001"
set "FRMS_TEST_CUSTOMER_1_PASSWORD=11111111"
set "FRMS_TEST_STAFF_01_EMAIL=staff.f01@example.com"
set "FRMS_TEST_STAFF_PASSWORD=11111111"
set "FRMS_TEST_DB_ISOLATED=false"
set "FRMS_ALLOW_STATE_CHANGES=NO"
node tests\system\scripts\batch12-env-audit.mjs
node tests\system\scripts\validate-coverage.mjs
```

The existing `run-frms-tests.ps1` launcher can set most process variables automatically and starts/stops the backend. Backend secrets remain only in ignored `backend/Frms.Api/.env`: check presence (not values) of `ConnectionStrings__FrmsDb`, `Jwt__SigningKey` and `backend/Frms.Api/prod-ca-2021.crt`. **If `ConnectionStrings__Frms` exists, it may override `ConnectionStrings__FrmsDb`.** Confirm the actual database target before any write tests. In the backend process set `ASPNETCORE_ENVIRONMENT=Testing`, `DOTNET_ENVIRONMENT=Testing`, and disable the Billing/NoShow/Notifications jobs per `config/profile.md`. Verify `http://127.0.0.1:5164/health` is healthy.

## Additional ACTIVE role accounts: configure locally, do not post passwords

The new test helpers require **both explicit identity and a password** for each unverified role; missing values yield Playwright SKIPPED / normalized Blocked, not PASS.

```bat
rem Use your actual ACTIVE seeded accounts, not these placeholder instructions.
rem set "FRMS_TEST_MANAGER_01_EMAIL=<verified manager email>"
rem set "FRMS_TEST_MANAGER_PASSWORD=<local secret>"
rem set "FRMS_TEST_BOM_EMAIL=<verified business operations email>"
rem set "FRMS_TEST_BOM_PASSWORD=<local secret>"
rem set "FRMS_TEST_ADMIN_EMAIL=<verified system administrator email>"
rem set "FRMS_TEST_ADMIN_PASSWORD=<local secret>"
```

Alternatively `FRMS_TEST_DEMO_PASSWORD` may be set locally if all configured roles deliberately share the test password. Nothing is committed to the patch. All these accounts must be ACTIVE and correctly assigned to their facilities in the actual Testing backend.

## Seeded fixture identifiers for the 50 new tests

Configure names/UUIDs from the real **Testing** data (never invent IDs). The exact missing names are reported by `node tests/system/scripts/batch12-env-audit.mjs`. Cases lacking fixture names are blocked by design. At minimum:

| Area | Additional variables | Preconditions |
| --- | --- | --- |
| Auth + AI + public catalog | `FRMS_TEST_UNREGISTERED_CUSTOMER_PHONE`, `FRMS_TEST_UNREGISTERED_EMPLOYEE_EMAIL`, `FRMS_TEST_HANOI_FACILITY_ID`, `FRMS_TEST_HANOI_FACILITY_NAME` | Unregistered identifiers must truly be absent; facility ACTIVE and publicly listed |
| Reservation | `FRMS_TEST_ACTIVE_FACILITY_ID`, `FRMS_TEST_AVAILABLE_UNIT_TYPE_ID`, `FRMS_TEST_NO_CCCD_PHONE`, `FRMS_TEST_NO_CCCD_PASSWORD`, `FRMS_TEST_OWN_RESERVATION_ID`, `FRMS_TEST_CUSTOMER_2_RESERVATION_ID` | Capacity available for next month; no-CCCD account active; own/other owner verified |
| Contracts + invoices | `FRMS_TEST_ACTIVE_CONTRACT_ID`, `FRMS_TEST_COMPLETED_CONTRACT_ID`, `FRMS_TEST_TERMINATED_CONTRACT_ID`, `FRMS_TEST_OWN_DEPOSIT_INVOICE_ID`, `FRMS_TEST_OWN_PAID_INVOICE_ID`, `FRMS_TEST_CUSTOMER_2_INVOICE_ID` | Statuses and ownership checked against seeded data; don't mutate them |
| Customer support | `FRMS_TEST_CUSTOMER_2_SUPPORT_TICKET_ID` | Belongs to another customer, must be inaccessible to Customer 1; Customer 1 has at least one own Contract |
| Staff + Manager | `FRMS_TEST_STAFF_OWN_REFERENCE_ID`, `FRMS_TEST_OTHER_FACILITY_REFERENCE_ID`, `FRMS_TEST_STAFF_SCOPE_DATE`, `FRMS_TEST_STAFF_HANDOVER_RESERVATION_ID`, `FRMS_TEST_STAFF_HANDOVER_VISIT_ID`, `FRMS_TEST_ASSIGNABLE_RESERVATION_ID`, `FRMS_TEST_AVAILABLE_PHYSICAL_UNIT_ID`, `FRMS_TEST_STAFF_IN_PROGRESS_INSPECTION_ID` | Own/cross-facility work items on the *same* date, SCHEDULED visit for check-in gating, AVAILABLE unit, IN_PROGRESS inspection owned by Staff |
| Pricing | `FRMS_TEST_PRICING_UNIT_TYPE_NAME` | Unique searchable global UnitType that Business Operations Manager can read; no price mutation |

Some inherited launcher fixtures (active facility, available type, etc.) may already be resolved by Patch 08. Do not overwrite them with stale hardcoded UUIDs.

## Static verification and run commands

From the existing repo root, first inspect local changes and **do not re-apply patches 01–11**:

```bat
git branch --show-current
git status --short
git apply --check FRMS_BATCH12_PLUS50.patch
git apply FRMS_BATCH12_PLUS50.patch
node tests\system\scripts\batch12-env-audit.mjs
node tests\system\scripts\validate-coverage.mjs
powershell.exe -NoProfile -ExecutionPolicy Bypass -File ".\run-frms-tests.ps1" -Mode all -Workers 4 -Retries 1
```

Replace the patch filename above with the location to which you saved it. For a single test:

```bat
powershell.exe -NoProfile -ExecutionPolicy Bypass -File ".\run-frms-tests.ps1" -Mode case -CaseId FRMS-ST-0031 -Workers 1 -Retries 0
```

Results: `tests/system/reports/<run-id>/{summary.md,results.json,playwright-results.json,jira_defects.csv}`. A nonzero launcher exit can reflect unimplemented/blocked cases; read `summary.md` rather than treating it as a Playwright assertion failure. **No failed/blocked test is automatically a Jira defect.**

## Acceptance requirements

Before declaring 101/101 PASS: verify full Playwright discovery (101 unique titles), new-case runtime execution, `summary.md` for Pass/Blocked/UnconfirmedFailure, SRS oracle review and any product defect independently reproducible on valid fixtures. The patch was statically parsed and its 275-ID registry mapping checked, but runtime evidence cannot be generated from the handoff-only archive here.
