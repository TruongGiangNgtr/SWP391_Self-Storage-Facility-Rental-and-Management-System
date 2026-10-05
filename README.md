# FRMS — Self-Storage Facility Rental and Management System

FRMS is a React + TypeScript and ASP.NET Core system for the five approved roles and seven approved rental-management flows. The governing implementation source is the SRS V10 final; no Phase 1 flow behavior is implemented in this foundation.

## Phase 0 status

**Completed — verified 2026-10-05.** All required build/test/SQL/Postman/Playwright and committed-diff whitespace checks passed. The Phase 0 seed scope is five UserRoles, Policy v1 and six fixed DamageTypes, per the owner-approved SRS clarification. ExtraFeeType schema/checks remain without seeded rows; amounts and currency belong to later approved deployment data.

Phase 1 is deliberately out of scope. The preserved SRS route catalogue is contract-only: those controllers return `501 ENDPOINT_NOT_IMPLEMENTED` and do not contain reservation, payment, handover, rental, inspection, return, facility-operation, or support behavior.

Production credentials and monetary defaults remain outside source control:

- Production accounts are not seeded. The Testing fixture creates disposable accounts with BCrypt hashes and generates its own JWT key.
- The ExtraFeeType names are authoritative; its data is deferred beyond Phase 0 until amounts and currency are approved.

See [Phase 0 foundation notes](docs/PHASE0_FOUNDATION.md) for the exact boundary and follow-up decisions.

See [merge validation report](docs/PHASE0_MERGE_VALIDATION.md) for actual command results, the full exit-gate checklist, and the exact changed-file manifest.

## Repository layout

```text
backend/Frms.Api                 HTTP composition root and API DTOs
backend/Frms.Business            commands/results, lifecycle services, IClock/provider contracts
backend/Frms.DataAccess          EF Core model, migration, repositories
backend/Frms.Infrastructure      provider-registration boundary
backend/tests                    NUnit unit/API/integration/architecture suites
frontend                         Vite + React + TypeScript foundation
tests/postman                    Phase 0 OpenAPI/auth-contract collection
tests/e2e/playwright             Phase 0 browser-shell smoke test
```

## Prerequisites

- .NET SDK `10.0.401` (pinned in `global.json`)
- Node.js compatible with pnpm `11.19.0`
- SQL Server for migration verification/runtime

## Local setup

Run these commands from the repository root (`C:\SWP391`). The solution on this branch is `Frms.slnx` at the root, not `backend/Frms.slnx`.

Restore dependencies:

```powershell
dotnet tool restore
dotnet restore Frms.slnx --disable-parallel
pnpm install --frozen-lockfile
```

SQL Server must be running and accessible before applying the existing migration. Replace the placeholders locally; keep the connection string and JWT key only in the current backend terminal session:

```powershell
# Backend terminal: session-only configuration, never commit these values
$env:ConnectionStrings__FrmsDb = "Server=<SERVER>;Database=<DATABASE>;Trusted_Connection=True;TrustServerCertificate=True"
$env:Jwt__SigningKey = [Convert]::ToBase64String([System.Security.Cryptography.RandomNumberGenerator]::GetBytes(32))

dotnet tool restore
dotnet ef database update --project backend/Frms.DataAccess --startup-project backend/Frms.Api
dotnet run --project backend/Frms.Api
```

The existing connection-string name `ConnectionStrings:Frms` remains supported through `ConnectionStrings__Frms` and takes precedence over the `FrmsDb` alias. Set only one name in a local session. Runtime configuration uses standard .NET configuration, including environment variables. The EF design-time factory also supports legacy `FRMS_CONNECTION_STRING` first, then `ConnectionStrings__Frms`, then `ConnectionStrings__FrmsDb`; it fails clearly if none is configured. There is no default database or automatic startup migration.

`backend/Frms.Api/appsettings.Example.json` contains placeholders only and is a reference, not an automatically loaded local configuration. Do not commit local settings, passwords, database files, or signing keys. `TrustServerCertificate=True` in the example is for local development; use the deployment's approved authentication and certificate settings in production.

In a separate frontend terminal at the repository root:

```powershell
Copy-Item frontend/.env.example frontend/.env.local
pnpm dev
```

The default launch profile uses `http://localhost:5164`, which is the sample `VITE_API_PROXY_TARGET`. The HTTPS profile uses `https://localhost:7235` (and HTTP 5164). To use it, start the API with `dotnet run --project backend/Frms.Api --launch-profile https` and set the target in the ignored `.env.local` to that HTTPS URL. Only for its self-signed ASP.NET Core development certificate, set `VITE_API_PROXY_ALLOW_SELF_SIGNED=true`; other targets keep certificate verification enabled. Restart Vite after changing the environment.

The shared frontend API client calls relative `/api/v1` paths, for example `fetch("/api/v1/auth/me")`. Vite forwards all `/api` requests without rewriting the path and with `changeOrigin: true`. Production builds and `vite preview` do not use the dev proxy: configure the production web server to route same-origin `/api` requests to the API. Never place a SQL connection string or any secret in a `VITE_*` variable.

The existing anonymous `/health` endpoint now probes the configured SQL Server through `FrmsDbContext`: HTTP 200 when reachable, HTTP 503 when unavailable, with no connection details in its response. This checks connectivity, not migration/schema completeness.

After a Release build, generate a reviewable idempotent deployment script with:

```powershell
dotnet tool run dotnet-ef migrations script --project backend/Frms.DataAccess --startup-project backend/Frms.Api --no-build --configuration Release --idempotent
```

The implemented Phase 0 API endpoints are `/api/v1/auth/customer/login`, `/api/v1/auth/employee/login`, `/api/v1/auth/me`, `/health`, and `/openapi/v1.json`. The remaining documented routes are 501 contract scaffolds. Login needs an account provisioned through an approved operational path; the real SQL integration fixture provisions disposable test accounts only when `FRMS_TEST_CONNECTION_STRING` is supplied.

## Verification

```powershell
$env:FRMS_TEST_CONNECTION_STRING = '<connection-string-for-a-disposable-Frms_Test_*-database>'
dotnet tool restore
dotnet restore Frms.slnx --disable-parallel
dotnet build Frms.slnx --no-restore --configuration Release
dotnet test Frms.slnx --no-restore --configuration Release
pnpm install --frozen-lockfile
pnpm build:frontend
pnpm test:postman
pnpm --dir tests/e2e/playwright run install:browsers
pnpm test:e2e
git diff --check origin/main...HEAD
```

There is no CI workflow in this checkout; run these gates locally from the repository root. The SQL integration tests require an actual SQL Server connection to a disposable database named `Frms_Test_*` and permission to create/migrate it. Supply the connection through the environment using the instance's approved authentication/TLS settings; do not put connection strings or credentials in committed files. SQL tests fail when this input is absent rather than skipping the gate. Schema fixtures roll back; authentication fixtures remain in the disposable test database.

If `pnpm` is not on PATH, create Corepack shims only in a temporary directory and update the current terminal's PATH; Corepack uses the version pinned in `package.json`:

```powershell
$pnpmShimDir = Join-Path $env:TEMP 'frms-corepack-shims'
New-Item -ItemType Directory -Path $pnpmShimDir -Force | Out-Null
corepack enable --install-directory $pnpmShimDir
$env:PATH = $pnpmShimDir + [IO.Path]::PathSeparator + $env:PATH
```

After building the frontend, run the configuration regression checks (Node.js 24, as used for validation):

```powershell
node --test backend/tests/frontend-configuration.test.mjs
```

These check relative API calls, the dev proxy and local certificate opt-in, actual path forwarding, and exclusion of backend configuration/local data files from the bundle and tracked configuration.

After the Release build, start a temporary API in another terminal before `pnpm test:postman`. Provide the same disposable test connection in that terminal and generate a session-only signing key:

```powershell
$env:ConnectionStrings__Frms = $env:FRMS_TEST_CONNECTION_STRING
$env:ASPNETCORE_ENVIRONMENT = 'Testing'
$env:Jwt__SigningKey = [Convert]::ToBase64String([System.Security.Cryptography.RandomNumberGenerator]::GetBytes(32))
dotnet run --project backend/Frms.Api --no-build --configuration Release --no-launch-profile --urls http://localhost:5164
```

Stop the temporary API with Ctrl+C after Postman; do not persist its key or fixture accounts as production configuration.

Install the pinned browser once before the E2E command:

```powershell
pnpm --dir tests/e2e/playwright run install:browsers
```

## Core conventions

- API base path: `/api/v1`; JSON is camelCase and enums are strings.
- Timestamps are UTC ISO-8601; business-calendar/display conversion is `Asia/Ho_Chi_Minh` (GMT+7).
- Money is `decimal`; month values remain `YYYY-MM`.
- Errors have a stable `code` and `traceId`; clients must not branch on prose.
- Customer sign-in is by phone number; employee sign-in is by email. BCrypt work factor defaults to 12 and is configuration-bound.
- The allowed path is Controller → Business service → Repository → EF Core/SQL Server. API DTOs and EF entities never cross the Business service contract boundary.

## Authoritative documents

| File | Purpose |
|---|---|
| `AGENTS.md` | Mandatory repository operating rules |
| `TECH_BASELINE.md` | Locked technical baseline |
| `docs/FRMS_SRS_V10.md` | Governing FRMS SRS V10 FINAL supplied to this checkout |
| `docs/03-FRMS_Data_Dictionary_V2_1.md` | Entity, seed, constraint, procedure, and job specification supplied to this checkout |

The initial documents were supplied under `docs/` in this checkout rather than the legacy `project_sources/` paths described in earlier repository orientation text. Their content, not that legacy path, was used as the implementation authority.
