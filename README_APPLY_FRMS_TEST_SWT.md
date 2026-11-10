# FRMS automation patches for `TEST_SWT` — 2026-11-10

**Target:** Existing remote branch `TEST_SWT`, baseline commit `45e94710477f9a9085ca22dbc69523c914de4d62` (verified 2026-11-10). Original patches were authored against `main` commit `c89b417c50a985bad29cc82f7d4b46f3e9c6d549`; relevant existing files and patch target paths were compared and found compatible. `TEST_SWT` is 22 commits behind that main baseline; this bundle does not merge or cherry-pick those commits.

This bundle contains patches 01–03 (apply in order). No patch modifies FRMS business logic, migrations or production API code. The tests use the existing Playwright 1.63 dependency and browser configuration. Apply directly to the existing `TEST_SWT` branch; do not create a new branch or push changes to `main`.

## Install patches (Windows PowerShell)

```powershell
cd 'D:\path\to\SWP391_Self-Storage-Facility-Rental-and-Management-System'
git fetch origin
# Checkout the existing TEST_SWT branch, do not create a new branch.
if (git show-ref --verify --quiet refs/heads/TEST_SWT) {
    git switch TEST_SWT
} else {
    git switch --track origin/TEST_SWT
}
git pull --ff-only origin TEST_SWT
git status --short
# Branch check: must show TEST_SWT
git branch --show-current

# Unzip FRMS_AUTOMATION_PATCHES_01_03.zip to a separate directory, then:
$patchRoot = 'D:\path\to\FRMS_AUTOMATION_PATCHES_01_03'
& "$patchRoot\APPLY_FRMS_PATCHES.ps1" -Repo (Get-Location).Path

# Alternatively, in repository root:
# git apply --check "$patchRoot\01_FRMS_CORE_REGISTRY_REPORTER.patch"
# git apply "$patchRoot\01_FRMS_CORE_REGISTRY_REPORTER.patch"
# ... repeat in order for patches 02 and 03.
```

The script **requires `TEST_SWT`**, refuses a dirty working tree, checks that all three patches are compatible **before applying any**, and warns if HEAD differs from the inspected `TEST_SWT` commit. If a later apply unexpectedly fails, inspect `git status --short` before retrying. Do not use `git reset --hard` as an automatic recovery strategy. Keep work in `TEST_SWT` and push to `origin/TEST_SWT` only when validated.

## Run the implemented tests

```powershell
pnpm install --frozen-lockfile
pnpm --dir tests/e2e/playwright run install:browsers

# Start the .NET API and a separate PostgreSQL test DB.
$env:FRMS_ENVIRONMENT = 'Testing'
$env:FRMS_API_URL = 'http://127.0.0.1:5164'
$env:FRMS_UI_URL = 'http://127.0.0.1:4173'
$env:VITE_API_PROXY_TARGET = $env:FRMS_API_URL
$env:CI = 'true' # Prevent Playwright reusing unrelated frontend servers.

# Credentials must correspond to verified ACTIVE fixtures.
# Configure secrets via environment/CI secrets, not inside test files.
# FRMS_TEST_CUSTOMER_1_PASSWORD (Customer 1 login uses PHONE 0901000001)
# FRMS_TEST_STAFF_PASSWORD (Staff 01 login uses EMAIL staff.f01@example.com)
# For catalog card tests, seed an actual matching ACTIVE facility and available unit:
# FRMS_TEST_ACTIVE_FACILITY_NAME
# FRMS_TEST_AVAILABLE_UNIT_NAME
# FRMS_TEST_AVAILABLE_UNIT_SIZE
# FRMS_TEST_AVAILABLE_UNIT_MODE (PRIVATE or PUBLIC)
# FRMS_TEST_AVAILABLE_UNIT_RENT_VND

pnpm run test:frms:coverage
pnpm run test:frms:preflight
pnpm run test:frms:case -- --id FRMS-ST-0002
pnpm run test:frms:case -- --area 'Authentication & Security'
pnpm run test:frms:all
```

Read `tests/system/README.md` after applying. If no backend is available, the runner exits 2 and marks selected cases Blocked without creating Jira defects. Reports are written to `tests/system/reports/<run-id>/`. CSV contains only the header until separately confirmed actual product defects satisfy the exporter gates. **This is not yet a complete 275-test suite.**

## Current coverage and remaining implementation

| Area | Registry | Executable code | Review | Not implemented |
|---|---:|---:|---:|---:|
| Authentication & Security | 51 | 22 | 0 | 29 |
| Storage Search & AI Size Guide | 19 | 8 | 0 | 11 |
| Reservation | 22 | 0 | 0 | 22 |
| Handover | 29 | 0 | 0 | 29 |
| Facility Operations | 10 | 0 | 0 | 10 |
| Access Visits | 25 | 0 | 3 | 22 |
| Billing & Payments | 23 | 0 | 0 | 23 |
| Renewal | 16 | 0 | 0 | 16 |
| Return & Settlement | 22 | 0 | 0 | 22 |
| Support Tickets | 25 | 0 | 5 | 20 |
| Administration | 19 | 0 | 0 | 19 |
| Pricing & Reference Data | 14 | 0 | 0 | 14 |
| **Total** | **275** | **30** | **8** | **237** |

**Proposed follow-on implementation batches:**

- Patch 04: complete authorization fixture validation, customer registration isolation, auth tests, catalog matching and deterministic backend seed factory.
- Patch 05: reservation and deposit payments including capacity race, signed callback, and duplicate delivery.
- Patch 06: handover, facility operations and access visits, with role/facility isolation.
- Patch 07: billing, renewal and return/settlement, with fixed clock and transaction invariants.
- Patch 08: support tickets, administration, pricing and reference data; connect NUnit and CI reporting.

No API/DB state assertions, time manipulation or payment callbacks are claimed implemented in patches 01–03 beyond the three independent API checks. The 8 unresolved test-spec/UI gaps must remain Review until requirement approval.

## Verification performed for this bundle

- Parsed all 275 source records, preserving `-B`, `-C`, `-D` and `-E` suffix IDs.
- Verified 12 functional-area totals and unique ID mapping.
- Checked JavaScript syntax of all system runner modules and TypeScript transpilation syntax of all 3 Playwright spec files.
- Replayed `git apply --check` and `git apply` sequentially on a clean reconstructed baseline containing the exact inspected root `package.json`.
- Simulated a missing-backend run: selected test becomes Blocked, and `jira_defects.csv` has header only.
- Simulated normalization of Pass, Flaky, Blocked and UnconfirmedFailure; unconfirmed failures produced zero Jira defect rows.

**Not verified:** Browser/runtime execution against a running FRMS API, populated database, actual login session, Playwright dependency installation on a full checkout, and final acceptance against independently approved SRS. Those require an isolated test environment and valid fixtures.

## Commit & push on TEST_SWT (only after checking reports)

```powershell
git status --short
git add package.json tests/system tests/e2e/playwright/tests/frms
git commit -m "test: add staged FRMS automation to TEST_SWT"
git push origin TEST_SWT
```

Do not interpret the 30 scripts as 30 runtime passes; additional fixtures/server startup are needed.
