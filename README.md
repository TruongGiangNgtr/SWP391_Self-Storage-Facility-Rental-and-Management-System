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

Restore dependencies:

```powershell
dotnet tool restore
dotnet restore Frms.slnx --disable-parallel
pnpm install --frozen-lockfile
```

Keep database and JWT secrets outside source control. For a local session, set the following environment variables (replace placeholders):

```powershell
$env:ConnectionStrings__Frms = '<your-local-sql-server-connection-string>'
$env:Jwt__SigningKey = '<at-least-32-byte-secret>'
```

Apply the baseline migration and run the API:

```powershell
dotnet ef database update --project backend/Frms.DataAccess --startup-project backend/Frms.Api
dotnet run --project backend/Frms.Api
```

The design-time factory uses `FRMS_CONNECTION_STRING`, then `ConnectionStrings__Frms`; an unset connection falls back to `Frms_DesignTime`. Always configure the intended database explicitly. After a Release build, generate a reviewable idempotent deployment script with:

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

There is no CI workflow in this checkout; run these gates locally from the repository root. The SQL integration tests require an actual SQL Server connection to a disposable database named `Frms_Test_*` and permission to create/migrate it. Supply the connection through the environment using the instance's approved authentication/TLS settings; do not put connection strings or credentials in committed files. SQL tests fail when this input is absent rather than skipping the gate. Schema fixtures roll back; authentication fixtures remain in the disposable test database. When `pnpm` is not on PATH, use Corepack to enable the repository-pinned version.

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
