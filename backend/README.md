# FRMS backend — Phase 0

The backend is split into four assemblies:

- `Frms.Api`: HTTP, request/response DTOs, mapping, authentication middleware, and DI composition root.
- `Frms.Business`: command/result service contracts, authentication orchestration, `IClock`, and external provider interfaces.
- `Frms.DataAccess`: EF Core persistence entities, the authoritative initial migration, reference-data seeds, and repositories.
- `Frms.Infrastructure`: external-provider registration boundary; no provider behavior is fabricated in Phase 0.

The HTTP path is API → Business → repository → SQL Server. Infrastructure implements Business-owned interfaces. `Program.cs` composes DataAccess/Infrastructure registrations. Controllers and background-job types cannot access repositories or `DbContext` directly; `Frms.ArchitectureTests` verifies this.

## Configure and run

Run the following from the repository root (`C:\SWP391`). SQL Server must be running and accessible before applying the existing migration. Replace placeholders only in your local terminal; never commit connection credentials, database files, local settings or JWT keys.

```powershell
# Backend terminal: values last only for this session
$env:ConnectionStrings__FrmsDb = "Server=<SERVER>;Database=<DATABASE>;Trusted_Connection=True;TrustServerCertificate=True"
$env:Jwt__SigningKey = [Convert]::ToBase64String([System.Security.Cryptography.RandomNumberGenerator]::GetBytes(32))
dotnet tool restore
dotnet ef database update --project backend/Frms.DataAccess --startup-project backend/Frms.Api
dotnet run --project backend/Frms.Api
```

Runtime keeps the existing `ConnectionStrings:Frms` name (environment variable `ConnectionStrings__Frms`) and supports `ConnectionStrings:FrmsDb` as an alias. The existing name takes precedence, so set only one. EF tools support `FRMS_CONNECTION_STRING`, then `ConnectionStrings__Frms`, then `ConnectionStrings__FrmsDb`, and fail if no connection is configured. They no longer select a default database. The API never runs migrations automatically at startup.

`Frms.Api/appsettings.Example.json` is a placeholder reference, not an automatically loaded settings file. Local `appsettings.*.local.json` and `.env*` files are ignored; examples remain eligible for source control. The local example's `TrustServerCertificate=True` is not a production TLS policy.

The existing `/health` endpoint probes SQL Server through the registered `FrmsDbContext`. It returns 200 when reachable and 503 when unavailable, without exposing connection details. A healthy probe does not verify migration/schema completeness.

In another terminal at the repository root:

```powershell
Copy-Item frontend/.env.example frontend/.env.local
pnpm dev
```

`launchSettings.json` defines HTTP `http://localhost:5164`, which is the example proxy target. The HTTPS profile also defines `https://localhost:7235`: run the API with `--launch-profile https`, update the ignored frontend `.env.local` target accordingly, and opt into `VITE_API_PROXY_ALLOW_SELF_SIGNED=true` only for its self-signed local development certificate. Restart Vite after environment changes.

Frontend API requests use `/api/v1` on the same origin. Vite development forwards `/api` to `VITE_API_PROXY_TARGET` with no path rewrite. Production and Vite preview require the web server to route `/api` to the API. SQL configuration belongs only to the backend; never use `VITE_*` for secrets. See the root README for the full local setup.

`Frms.Api` implements the Phase 0 authentication endpoints and `/openapi/v1.json`; the retained SRS route catalogue is 501 contract scaffolding only. It has no production-account provisioning endpoint and no committed credentials. A real SQL Server integration fixture creates disposable accounts when `FRMS_TEST_CONNECTION_STRING` points to a disposable database. Phase 0 seed comprises five roles, Policy v1, and six approved DamageTypes only. ExtraFeeType schema/checks remain; its rows are deferred beyond Phase 0 until amounts and currency are approved.

Run solution validation from the repository root using `Frms.slnx`. Real SQL tests require `FRMS_TEST_CONNECTION_STRING` targeting a disposable `Frms_Test_*` database and fail if it is absent. No CI workflow is configured; the root README documents the full local commands and temporary API setup for Postman. See `docs/PHASE0_FOUNDATION.md` for migration/redeploy reasoning.
