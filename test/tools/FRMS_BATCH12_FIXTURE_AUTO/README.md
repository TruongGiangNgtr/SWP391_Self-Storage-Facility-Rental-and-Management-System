# FRMS Batch 12 — Automatic fixture discovery

Last updated: 2026-11-11. Branch: `TEST_SWT`. Existing 51 PASS remain historical evidence; 50 Batch 12 tests are not yet runtime-verified.

## Why the audit still shows 20 missing keys

`batch12-env-audit.mjs` reads only `process.env`; it does not infer fixture IDs or log in to the API. The two Hanoi fields are known from the supplied ACTIVE Facility API snapshot. The other fields require current, authorized API reads for the correct account and facility. Arbitrary UUIDs cause invalid test failures.

## Installation (Windows CMD)

1. This *historical installer version* is stored at `test/tools/FRMS_BATCH12_FIXTURE_AUTO/`, containing `install.mjs` and its bundled `tests/system/scripts/batch12-discover.mjs`. Do not re-install on a branch where later discovery changes have already been applied.
2. From that same root, while on `TEST_SWT`, run:

```bat
node .\test\tools\FRMS_BATCH12_FIXTURE_AUTO\install.mjs
git diff -- run-frms-tests.ps1
powershell.exe -NoProfile -ExecutionPolicy Bypass -File ".\run-frms-tests.ps1" -Mode all -Workers 4 -Retries 1
```

The installer makes an idempotent local edit to `run-frms-tests.ps1`, preserves the existing launcher logic, creates a `run-frms-tests.ps1.batch12.bak` backup, and copies one Node discovery helper. It does **not** apply old patches, modify the database, add secrets, or turn on state-changing test flags.

The launcher now (1) sets documented demo role defaults without overriding variables already set, (2) starts or connects to a healthy backend, (3) runs preflight and the existing catalog discovery, (4) makes authenticated **GET** calls for Batch 12 fixtures and authenticates via the existing login endpoints, (5) sets only verified and previously unset fixture variables **in its own process**, (6) runs the audit and Playwright tests.

**The CMD parent process is not modified by a PowerShell child process.** If you run `node tests\system\scripts\batch12-env-audit.mjs` separately in the original CMD after the launcher finishes, it can again show missing variables. The audit inside the launcher is the relevant post-discovery check.

## Authentication and fixture rules

- Customer 1: phone `0901000001`; Customer 2: phone `0901000002`.
- Staff 01: `staff.f01@example.com`; Staff 02: `staff.f02@example.com`, used only to discover a cross-facility work item.
- Facility Manager: `manager.f01@example.com`; Business Operations Manager: `bom@example.com`; System Administrator: `admin@example.com`.
- Demo password defaults to `11111111` only as documented for the local Testing demo baseline. Provide `FRMS_TEST_*_PASSWORD` overrides in your own environment if credentials have changed. Never share passwords/tokens or commit `.env`.
- A role counts as verified only after a successful login **and** `/api/v1/auth/me` reporting the expected ACTIVE role.
- Customer 1 and 2 must be distinct authenticated accounts. Other-customer invoices/reservations/tickets must come from Customer 2's authorized listing, not by arbitrary guess.
- Staff references must be date-scoped and facility-scoped. Cross-facility references are obtained by signing in as another Staff member and comparing authenticated work items for the same date.
- Manager assignment fixture requires a matching `CONFIRMED` reservation with a `CHECKED_IN` reservation visit and AVAILABLE physical unit of the same unit type at the Manager's facility.
- In-progress inspection requires an `IN_PROGRESS` record owned by the signed-in Staff.
- Unknown phone and email candidates are selected only after the Admin successfully enumerates the **complete** authorized user directory; values are not blindly assumed unregistered.
- Public Hanoi fixture is exposed only when live public API confirms an ACTIVE facility with positive availability for next month. The prior snapshot alone does not imply future inventory.
- If no suitable record exists, the corresponding key remains unset; affected Playwright cases must be SKIPPED/Blocked, not PASS. Do not seed, mutate, delete or change data on shared Supabase.

## Expected output

Look for:

```text
Batch 12 fixture discovery: N verified value(s) supplied to this launcher process.
  CUSTOMER: verified
  CUSTOMER_2: verified
  FACILITY_STAFF: verified
  FACILITY_MANAGER: verified
  BUSINESS_OPERATIONS_MANAGER: verified
  SYSTEM_ADMINISTRATOR: verified
FRMS Batch 12: environment audit ...
20 explicit fixture keys referenced; M missing ...
```

`N` and `M` depend on live data and need not equal 20 and 0. The launcher still returns exit code 3 when the full 275-case registry contains unimplemented/review/blocked cases even if executed tests pass.

## Existing verified Facility reference (not a runtime capacity promise)

```bat
chcp 65001 >NUL
set "FRMS_TEST_HANOI_FACILITY_ID=f0000000-0000-0000-0000-000000000001"
set "FRMS_TEST_HANOI_FACILITY_NAME=FRMS Facility - Hà Nội"
```

Setting these manually resolves only the two **missing-variable** warnings, not eligibility for the particular search test; the script prefers live positive-capacity verification.

## Recovery

To undo the local launcher edit (only if no subsequent intentional launcher changes were made), copy back `run-frms-tests.ps1.batch12.bak` to `run-frms-tests.ps1` and delete `tests/system/scripts/batch12-discover.mjs` if it was not customized. Review `git diff` first. Do not commit the `.bak` file.

## Validation evidence

- `node --check` passed for installer and discovery script.
- Installer run twice against the reference Patch 08 launcher snapshot: one hook installed, second invocation idempotent.
- Against an in-memory mock API with six ACTIVE test roles and realistic fixture responses, discovery filled 19 of 20 advertised fixture fields and left the absent Staff inspection unset.
- Offline error path tested: when API `/health` is unavailable, discovery outputs no fixtures without attempting repeated logins.
- **No live local Windows backend or Supabase reads were performed from the assistant environment; user runtime results remain pending.**
