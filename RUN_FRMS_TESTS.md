# FRMS TEST_SWT - One-command Windows automated test launcher

## 1. Install into repository

Copy `run-frms-tests.ps1` and `07_FRMS_CONFIGURABLE_WORKERS.patch` to the repository **root**. Apply the patch once on `TEST_SWT`:

```powershell
git switch TEST_SWT
git apply --check .\07_FRMS_CONFIGURABLE_WORKERS.patch
git apply .\07_FRMS_CONFIGURABLE_WORKERS.patch
```

Do not re-apply the patch if it has already been applied. This patch changes only `tests/system/scripts/run.mjs` to respect worker/retry environment variables. It defaults to four workers and one retry.

## 2. Run from Windows PowerShell 5.1 (not CMD)

```powershell
Set-Location 'C:\Users\buidi\OneDrive\Desktop\Git\SWP391\SWP391_Self-Storage-Facility-Rental-and-Management-System'
powershell.exe -NoProfile -ExecutionPolicy Bypass -File .\run-frms-tests.ps1 -Mode all -Workers 4 -Retries 1
```

The launcher loads `backend/Frms.Api/.env` into its child-process environment without printing values, starts the backend automatically when needed, waits for `/health`, runs preflight, starts Vite through the Playwright configuration and executes implemented tests. It stops only the backend process that **it** started.

Other modes:

```powershell
powershell.exe -NoProfile -ExecutionPolicy Bypass -File .\run-frms-tests.ps1 -Mode case -CaseId FRMS-ST-0005
powershell.exe -NoProfile -ExecutionPolicy Bypass -File .\run-frms-tests.ps1 -Mode area -Area 'Authentication & Security'
powershell.exe -NoProfile -ExecutionPolicy Bypass -File .\run-frms-tests.ps1 -Mode coverage
powershell.exe -NoProfile -ExecutionPolicy Bypass -File .\run-frms-tests.ps1 -Mode preflight
powershell.exe -NoProfile -ExecutionPolicy Bypass -File .\run-frms-tests.ps1 -Mode catalog-probe
```

Use `-SkipBackend` to require/reuse an already-running healthy local API; use `-SkipCatalogDiscovery` to bypass the best-effort catalog fixture lookup; use `-InstallBrowser` on first run if Chromium has not been installed yet.

## 3. Environment inventory

| Variable | Origin or assigned value | Why |
| --- | --- | --- |
| `ConnectionStrings__FrmsDb` | Local backend `.env` | PostgreSQL connection (secret; not printed) |
| `Jwt__SigningKey` | Local backend `.env` | JWT (secret; not printed) |
| `Payment__PayOS__*`, `Ai__*`, `Email__*` | Local backend `.env` | External integrations; inherited, not overridden |
| `ASPNETCORE_ENVIRONMENT`, `DOTNET_ENVIRONMENT`, `FRMS_ENVIRONMENT` | `Testing` | Testing-only |
| `ASPNETCORE_URLS`, `FRMS_API_URL` | `http://127.0.0.1:5164` | API |
| `FRMS_UI_URL` | `http://127.0.0.1:4173` | Vite Playwright UI |
| `VITE_API_PROXY_TARGET` | `http://127.0.0.1:5164` | Vite `/api` proxy |
| `CI` | `true` | No Vite reuse |
| `FRMS_PLAYWRIGHT_WORKERS` | `4` (override with `-Workers`) | Parallel workers |
| `FRMS_PLAYWRIGHT_RETRIES` | `1` (override with `-Retries`) | Retry policy |
| `BackgroundJobs__Billing__Enabled` | `false` | Disable billing job |
| `BackgroundJobs__NoShow__Enabled` | `false` | Disable NoShow job |
| `BackgroundJobs__Notifications__Enabled` | `false` | Disable notification job |
| `FRMS_TEST_CUSTOMER_1_PHONE` | `0901000001` | Customer login by phone |
| `FRMS_TEST_CUSTOMER_1_PASSWORD` | `11111111` | Customer demo login password |
| `FRMS_TEST_STAFF_01_EMAIL` | `staff.f01@example.com` | Staff login by email |
| `FRMS_TEST_STAFF_PASSWORD` | `11111111` | Staff demo login password |
| `FRMS_TEST_AVAILABLE_UNIT_*` | Locker reference, **overridden if verified by API** | Catalog test fixture |
| `FRMS_TEST_ACTIVE_FACILITY_NAME` | **API-derived if first result offers an available unit**; otherwise not invented | Catalog test fixture |
| `FRMS_TEST_DB_ID` | Not set by launcher | Never claim an unverified remote database is isolated |

The `.env` variable loader also preserves all additional configured keys, including payment/AI/email integration values, without embedding them in the launcher. Local PostgreSQL TLS certificate must match the connection string.

**Important:** `mode=PUBLIC` for unit types refers to shared storage, not whether the catalogue is publicly visible. Both PRIVATE and PUBLIC types can appear in public search results.

## 4. Operational caveats

- The current `run.mjs` ignores `FRMS_PLAYWRIGHT_WORKERS` until Patch 07 is applied.
- The catalog fixture auto-discovery checks the **first facility** in the public search results and available unit types for the following two months. This matches the currently expanded first facility card. If that facility is sold out or the API times out, the fixture remains unconfirmed and those cases may be Blocked.
- `11111111` must match the actual database passwords. Environment settings alone do not create/reset accounts.
- The backend `.env` previously pointed to a remote/shared Supabase database. `Testing` is an application environment, **not database isolation**. The reservation expiration job may still run. Do not run mutating tests against shared data without isolation.
- `tests/system/reports/<run-id>/` contains run reports, traces and Jira CSV. Only independently confirmed product defects should be exported to Jira. A nonzero exit can signal a Blocked/UnconfirmedFailure case, not necessarily a product bug.
- The repository currently implements about 30 of 275 registered system cases. The launcher runs only implemented cases; it does not implement the remainder.
- Browser installation is not repeated on every run. If missing, run the launcher with `-InstallBrowser`.
