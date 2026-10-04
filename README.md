# FRMS — Self-Storage Facility Rental and Management System

FRMS is a React + TypeScript and ASP.NET Core system for the five approved roles and seven approved rental-management flows. The governing implementation source is the SRS V10 final; no Phase 1 flow behavior is implemented in this foundation.

## Phase 0 status

The technical-foundation exit gate is implemented: solution and workspace scaffolding, the four backend assemblies, DTO-to-Command/Result mapping, architecture-boundary tests, the 27-entity EF Core schema migration, authoritative reference-data seeds, authentication foundations, error contract, OpenAPI, and NUnit/Postman/Playwright skeletons.

Phase 1 is deliberately out of scope. In particular, there are no reservation, payment, handover, rental, inspection, return, facility-operation, or support endpoints; the API does not silently implement any business flow.

Two inputs required for a fully executable demonstration environment are intentionally not invented:

- The SRS/Data Dictionary does not provide approved employee/customer seed identities, credentials, or BCrypt hashes. No user account is seeded.
- The ExtraFeeType names are authoritative but their default monetary amounts are not. They remain an explicit deployment-data decision.

See [Phase 0 foundation notes](docs/PHASE0_FOUNDATION.md) for the exact boundary and follow-up decisions.

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
dotnet restore Frms.slnx
pnpm install
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

The Phase 0 API exposes only `/api/v1/auth/customer/login`, `/api/v1/auth/employee/login`, `/api/v1/auth/me`, and `/openapi/v1.json`. Login needs an account provisioned through an approved operational path; Phase 0 supplies no fabricated account-provisioning endpoint.

## Verification

```powershell
dotnet build Frms.slnx --no-restore --configuration Release
dotnet test Frms.slnx --no-restore --configuration Release
pnpm build:frontend
pnpm test:postman
pnpm test:e2e
```

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
