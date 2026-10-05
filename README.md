# FRMS — Self-Storage Facility Rental and Management System

FRMS is a React + TypeScript and ASP.NET Core system for the five approved roles and seven approved rental-management flows. The governing implementation source is the SRS V10 final; no Phase 1 flow behavior is implemented in this foundation.

## Phase 0 status

**In Progress.** The technical foundation is implemented: solution and workspace scaffolding, the four backend assemblies, DTO-to-Command/Result mapping, architecture-boundary tests, the 27-entity EF Core schema migrations, authentication, error contract, OpenAPI, and NUnit/Postman/Playwright harnesses. Empty-database migration and real SQL Server authentication/constraint tests pass. The seed exit gate remains open until the five approved ExtraFeeType default amounts and currency are supplied and the seed is implemented and verified.

Phase 1 is deliberately out of scope. The preserved SRS route catalogue is contract-only: those controllers return `501 ENDPOINT_NOT_IMPLEMENTED` and do not contain reservation, payment, handover, rental, inspection, return, facility-operation, or support behavior.

Production credentials and monetary defaults remain outside source control:

- Production accounts are not seeded. The Testing fixture creates disposable accounts with BCrypt hashes and generates its own JWT key.
- The ExtraFeeType names are authoritative but their default monetary amounts are not. They remain an explicit deployment-data decision.

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
$env:ConnectionStrings__Frms = 'Server=localhost;Database=Frms_Local;Trusted_Connection=True;TrustServerCertificate=True'
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
$env:FRMS_TEST_CONNECTION_STRING = 'Server=localhost;Database=Frms_Test_Phase0;Integrated Security=true;Encrypt=false;TrustServerCertificate=true'
dotnet build Frms.slnx --no-restore --configuration Release
dotnet test Frms.slnx --no-restore --configuration Release
pnpm build:frontend
pnpm test:postman
pnpm test:e2e
```

The SQL integration tests require a disposable database named `Frms_Test_*` and permission to create/migrate it. The example disables encryption for local development only; use the connection's approved TLS settings on a hosted SQL Server. SQL tests fail when this input is absent rather than silently skipping the gate. Schema fixtures roll back; authentication fixtures remain in the disposable test database. Start the API at `http://localhost:5164` before `pnpm test:postman`. When `pnpm` is not on PATH, use Corepack to enable the repository-pinned version.

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
