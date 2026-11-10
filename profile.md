# FRMS Automated System Testing - Profile

**Project:** SWP391 Self-Storage Facility Rental and Management System  
**Branch:** `TEST_SWT`  
**Last updated:** 2026-11-10 (Asia/Ho_Chi_Minh)  
**Purpose:** Record verified settings, intended settings, test fixtures, and the latest observed automated test run.

> Status words matter: **Verified** means confirmed by a command output or source file; **Configured** means a value to set in the local terminal; **Pending** means not yet confirmed to work. This profile does not claim that all 275 cases passed.

## 1. Repository and tools

| Item | Value | Status |
| --- | --- | --- |
| Repository | `https://github.com/TruongGiangNgtr/SWP391_Self-Storage-Facility-Rental-and-Management-System` | Verified |
| Branch | `TEST_SWT` | Verified |
| Local root | `C:\Users\buidi\OneDrive\Desktop\Git\SWP391\SWP391_Self-Storage-Facility-Rental-and-Management-System` | Verified |
| Operating system | Windows, build `10.0.26200.9711` | Verified |
| Test command shell | **CMD** (use `set "KEY=value"`) | Previously used |
| Backend command shell | **PowerShell** (use `$env:KEY = "value"`) | Verified by latest user log |
| Alternative test shell | PowerShell (use `$env:KEY = "value"`) | Supported |
| Package manager | `pnpm 11.19.0` | Verified |
| Backend | ASP.NET Core (.NET), PostgreSQL | Verified |
| UI automation | Playwright with Chromium Headless Shell | Verified |
| API URL | `http://127.0.0.1:5164` | Verified by preflight |
| Frontend URL | `http://127.0.0.1:4173` | Playwright starts Vite |
| Backend environment | `Testing` | Verified |
| Automation environment | `Testing` | Verified |

## 2. CMD variables for automated tests

Set these in the **same CMD window** in which `pnpm run test:frms:*` will run. They are process-local settings, not permanent Windows environment variables.

```bat
set "FRMS_ENVIRONMENT=Testing"
set "FRMS_API_URL=http://127.0.0.1:5164"
set "FRMS_UI_URL=http://127.0.0.1:4173"
set "VITE_API_PROXY_TARGET=http://127.0.0.1:5164"
set "CI=true"

rem Customer logs in using PHONE, not email.
set "FRMS_TEST_CUSTOMER_1_PHONE=0901000001"
set "FRMS_TEST_CUSTOMER_1_PASSWORD=11111111"

rem Employee logs in using EMAIL, not phone.
set "FRMS_TEST_STAFF_01_EMAIL=staff.f01@example.com"
set "FRMS_TEST_STAFF_PASSWORD=11111111"
```

`11111111` is the chosen demo/test password. **Pending:** authentication with these credentials has not yet been confirmed against the running database. Setting the variables alone does not change database passwords.

Other known customer demo accounts:

| Account | Login phone | Reference email |
| --- | --- | --- |
| Customer 1 | `0901000001` | `customer1@example.com` |
| Customer 2 | `0901000002` | `customer2@example.com` |
| Customer 3 | `0901000003` | `customer3@example.com` |

Staff accounts follow `staff.fNN@example.com` for `NN=01..62`; their facility memberships and ACTIVE status must be checked in the actual Testing database.

## 3. Backend configuration and complete PowerShell startup script

### 3.1 Confirmed backend configuration

Run the API in a **separate PowerShell terminal**, leaving it open during automated tests. The local environment file is located at `backend\Frms.Api\.env`. The file contains PostgreSQL, JWT, and service integration settings; the actual secrets are **not copied into this profile**.

The backend does **not** automatically load `.env` when `ASPNETCORE_ENVIRONMENT=Testing`. The working setup is to load its variables into the PowerShell process **before** calling `dotnet run --no-launch-profile`.

**Observed from the user's terminal:** the `.env` file exists, the PostgreSQL connection variable is set, the JWT signing key is set, and `backend\Frms.Api\prod-ca-2021.crt` exists. The last provided log ends at the `dotnet run` command; it does not itself show a new startup success message. Earlier logs independently confirmed `/health` and `PREFLIGHT OK`.

### 3.2 Full backend startup (Windows PowerShell)

Copy and execute this block in **PowerShell**, not CMD. It uses the user's confirmed local repository path and preserves the exact startup sequence.

```powershell
# 1. Go to repository root.
$REPO = "C:\Users\buidi\OneDrive\Desktop\Git\SWP391\SWP391_Self-Storage-Facility-Rental-and-Management-System"
Set-Location $REPO

# 2. Locate the local backend environment file.
$ENV_FILE = "$REPO\backend\Frms.Api\.env"
if (-not (Test-Path $ENV_FILE)) {
    throw "Khong tim thay file .env tai $ENV_FILE"
}

# 3. Load key=value entries from .env into THIS PowerShell process.
#    Preserve characters such as &, ?, spaces, and = inside quoted values.
foreach ($line in Get-Content -LiteralPath $ENV_FILE -Encoding UTF8) {
    if ($line -match '^\s*([A-Za-z_][A-Za-z0-9_]*)\s*=\s*(.*)$') {
        $key = $Matches[1]
        $value = $Matches[2].Trim()

        if ($value.Length -ge 2 -and
            $value.StartsWith('"') -and
            $value.EndsWith('"')) {
            $value = $value.Substring(1, $value.Length - 2)
        }

        [Environment]::SetEnvironmentVariable($key, $value, "Process")
    }
}

# 4. Configure isolated Testing process mode and API listener.
$env:ASPNETCORE_ENVIRONMENT = "Testing"
$env:DOTNET_ENVIRONMENT = "Testing"
$env:ASPNETCORE_URLS = "http://127.0.0.1:5164"

# 5. Disable configurable scheduled jobs for this test session.
$env:BackgroundJobs__Billing__Enabled = "false"
$env:BackgroundJobs__NoShow__Enabled = "false"
$env:BackgroundJobs__Notifications__Enabled = "false"

# 6. Verify that credentials are loaded WITHOUT printing their values.
Write-Host "Connection configured:" ([bool]$env:ConnectionStrings__FrmsDb)
Write-Host "JWT configured:" ([bool]$env:Jwt__SigningKey)

# 7. Verify TLS certificate file used by the connection string.
Test-Path "$REPO\backend\Frms.Api\prod-ca-2021.crt"

# 8. Start the backend. Keep this terminal open.
dotnet run --project .\backend\Frms.Api --no-launch-profile
```

The user's last confirmed checks produced:

```text
Connection configured: True
JWT configured: True
True
```

`True` in the third line refers to the certificate file path. These checks confirm the variables/file exist, **not** that the database is isolated or safe for destructive tests.

### 3.3 Verify the running API from another terminal

Use a **second PowerShell terminal** (the backend terminal stays occupied by `dotnet run`):

```powershell
$REPO = "C:\Users\buidi\OneDrive\Desktop\Git\SWP391\SWP391_Self-Storage-Facility-Rental-and-Management-System"
Set-Location $REPO

$env:FRMS_ENVIRONMENT = "Testing"
$env:FRMS_API_URL = "http://127.0.0.1:5164"
$env:FRMS_UI_URL = "http://127.0.0.1:4173"
$env:VITE_API_PROXY_TARGET = "http://127.0.0.1:5164"
$env:CI = "true"

Invoke-WebRequest -Uri "http://127.0.0.1:5164/health" -UseBasicParsing
pnpm run test:frms:preflight
```

For Windows PowerShell 5.1, `-UseBasicParsing` prevents the HTML/script parsing security prompt. Expected response: HTTP `200` with body `Healthy`, followed by `PREFLIGHT OK` when preflight succeeds. The prior run confirmed preflight success; rerun these commands to verify this particular backend session.

### 3.4 Database and process boundaries

- `.env` currently targets a **remote Supabase PostgreSQL database**. Setting ASP.NET Core to `Testing` does not create a dedicated database. Do not perform destructive/reset/migration tests against the remote database without confirming authorization and data isolation.
- If `ConnectionStrings__Frms` is also present, it overrides `ConnectionStrings__FrmsDb`. Check for an unintended override before starting the backend.
- The three job flags above disable Billing, NoShow, and Notifications jobs, but **reservation expiration** may still execute on startup and periodically. This matters when connected to shared data.
- The PowerShell backend process and CMD/PowerShell test process have **separate** environment variables; the test variables in section 2 must be set in the terminal used to run `pnpm`.
- Keep PostgreSQL credentials, JWT signing key, payment keys, AI keys, and SMTP passwords in local ignored `.env`; do not commit them to this profile or repository.

## 4. Playwright settings

| Item | Value | Status |
| --- | --- | --- |
| Tests | `tests/e2e/playwright/tests/frms/` | Verified |
| Registry | `tests/system/manifest/cases.json` | Verified |
| Runner | `tests/system/scripts/run.mjs` | Verified |
| Reports | `tests/system/reports/<run-id>/` | Verified |
| Browser | Chromium Headless Shell `v1243` | Installed / observed |
| Retries | **1** | Verified in runner |
| Workers requested | **4** | **Pending application** |
| Workers in last inspected runner | **1** (`--workers=1`) | Verified in Patch 06 runner source |
| Jira export | Only independently confirmed product defects | Verified policy |

**Important correction:** The previous profile described four workers as a preference, but that setting was not shown as applied. To activate it, change the Playwright argument in `tests/system/scripts/run.mjs` from `--workers=1` to `--workers=4`. Verify the next output contains `Running 30 tests using 4 workers`.

Parallel workers are suitable for independent, read-only UI cases. Before enabling concurrent Reservation, Billing, Renewal, or account-state tests, provide independent fixtures and deterministic cleanup; otherwise simultaneous tests may corrupt results or shared data.

## 5. Applied patches and test status

| Patch | Function | Status |
| --- | --- | --- |
| `01_FRMS_CORE_REGISTRY_REPORTER.patch` | Registry, reports and Jira exporter | Applied |
| `02_FRMS_AUTH_UI_TESTS.patch` | Authentication UI checks | Applied |
| `03_FRMS_CATALOG_AND_API_TESTS.patch` | Catalog and API checks | Applied |
| `04_FRMS_WINDOWS_PLAYWRIGHT_RUNNER_FIX.patch` | Fix Windows `C:\Program` child-process error | Applied |
| `05_FRMS_RESULT_LOCATORS_AND_CATALOG_DIAGNOSTICS.patch` | Fix ambiguous locators; improve catalog assertions | Applied |
| `06_FRMS_CATALOG_API_DIAGNOSTICS.patch` | Direct API probe, response timing, timeout diagnostics | **Pending confirmation** |

**Latest observed full run:** `20261110T130856341Z` (after Patch 05)

| Status | Count |
| --- | ---: |
| Registry cases / unique IDs | 275 / 275 |
| Implemented cases attempted | 30 |
| Pass | **20** |
| Blocked by fixture or test environment | **7** |
| Unconfirmed failure / catalog timeout | **3** |
| Not Run / not implemented | 237 |
| Review | 8 |
| Independently confirmed product defects for Jira | **0** |

Blocked login cases: `FRMS-ST-0005`, `0024`, `0197` (Customer password variable); `0008`, `0201` (Staff password variable). The CMD configuration in section 2 supplies the missing variables, but the actual login result is still pending.

Blocked catalog cases: `FRMS-ST-0027`, `0028` (fixture and facility availability). Unconfirmed timeouts: `FRMS-ST-0032`, `0035`, `0036` (wait for public catalog results). Do not classify fixture failures or timeout symptoms as product defects without independent reproduction.

## 6. Unit type reference data

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

## 7. Commands to run (CMD)

```bat
rem Validate the registry: 275 cases, 30 automated mappings.
pnpm run test:frms:coverage

rem API readiness (requires backend already running).
pnpm run test:frms:preflight

rem Install the browser only if missing.
pnpm --dir tests/e2e/playwright exec playwright install chromium

rem Run an individual implemented case.
pnpm run test:frms:case -- --id FRMS-ST-0002

rem Run all implemented cases and report against the entire registry.
pnpm run test:frms:all

rem Only after Patch 06 has been applied.
pnpm run test:frms:catalog-probe
```

**Case status guidance:** `Pass` proves the implemented assertion passed for that execution; `Blocked` means missing fixture/infrastructure; `UnconfirmedFailure` is not automatically a product defect; `Review` requires requirements or oracle decisions; `Not Run` is not tested. Jira CSV must include only independently confirmed software defects.

## 8. Next actions

- [ ] Change the Playwright runner from one worker to four and verify a four-worker run.
- [ ] Re-run customer/staff login tests using the configured demo password `11111111`; confirm accounts are ACTIVE.
- [ ] Identify an ACTIVE facility offering the chosen unit type with positive availability during the required months.
- [ ] Confirm whether Patch 06 was applied; run `test:frms:catalog-probe` if available.
- [ ] Re-run the suite and inspect the new `results.json` and trace evidence before reporting any confirmed defects.
- [ ] Continue implementing the outstanding cases with fixture isolation and requirement-aligned assertions.

## 9. Automated one-command test launcher (new)

Use `run-frms-tests.ps1` in repository root and apply `07_FRMS_CONFIGURABLE_WORKERS.patch` once on `TEST_SWT`. This script loads backend `.env` (all configured DB/JWT/integration keys), sets Testing environment, backend/API/UI/proxy URLs, customer/staff demo credentials (`11111111`), unit type reference values, 4 Playwright workers, 1 retry, and disabled configurable background jobs. It can start/stop backend automatically, run preflight, discover a catalog fixture when verifiable, and launch registered test cases with Jira/report generation. See `RUN_FRMS_TESTS.md` for complete usage and limitations.

```powershell
powershell.exe -NoProfile -ExecutionPolicy Bypass -File .\run-frms-tests.ps1 -Mode all -Workers 4 -Retries 1
```

**Status:** Files generated and statically validated; full Windows/DB run not executed here. The latest confirmed run remains 20 Pass, 7 Blocked, 3 UnconfirmedFailure.
