# FRMS backend — Phase 0

The backend is split into four assemblies:

- `Frms.Api`: HTTP, request/response DTOs, mapping, authentication middleware, and DI composition root.
- `Frms.Business`: command/result service contracts, authentication orchestration, `IClock`, and external provider interfaces.
- `Frms.DataAccess`: EF Core persistence entities, the authoritative initial migration, reference-data seeds, and repositories.
- `Frms.Infrastructure`: external-provider registration boundary; no provider behavior is fabricated in Phase 0.

The HTTP path is API → Business → repository → SQL Server. Infrastructure implements Business-owned interfaces. `Program.cs` composes DataAccess/Infrastructure registrations. Controllers and background-job types cannot access repositories or `DbContext` directly; `Frms.ArchitectureTests` verifies this.

## Configure and run

From the repository root (`C:\SWP391`), copy the placeholder example to the ignored backend file and edit its connection settings locally. Preserve an existing local file before copying:

```powershell
Copy-Item .\backend\Frms.Api\.env.example .\backend\Frms.Api\.env
# Edit the local .env; never commit it.
$env:Jwt__SigningKey = [Convert]::ToBase64String([System.Security.Cryptography.RandomNumberGenerator]::GetBytes(32))
dotnet run --project .\backend\Frms.Api
```

From `C:\SWP391\backend`, use `dotnet run --project .\Frms.Api` in the terminal with the session-only JWT key. The default launch profile selects Development.

In Development only, DotNetEnv loads `.env` from the API's `ContentRootPath`, independently of the working directory. `ConnectionStrings__FrmsDb` maps to `ConnectionStrings:FrmsDb`. Windows/PowerShell environment variables and command-line configuration have higher priority than the file; the loader does not modify process environment variables. A missing file is allowed, and existing configuration sources remain available. Production, Staging and Testing ignore the file.

The existing `ConnectionStrings:Frms` name remains supported and takes precedence over the `FrmsDb` alias; set only one name locally. Missing connection settings fail clearly. This runtime loader does not configure EF design-time tools, run migrations, create a test database or change tables. SQL Server must be running for database access, but startup itself does not validate the schema.

`Frms.Api/.env.example` contains placeholders only. The real `.env` is ignored by Git and excluded from build/publish items. `appsettings.Example.json` remains a placeholder reference, not an automatically loaded settings file. Never commit connection credentials, database files, local settings or JWT keys. `TrustServerCertificate=True` in the local example is not a production TLS policy.

The existing `/health` endpoint probes SQL Server through the registered `FrmsDbContext`. It returns 200 when reachable and 503 when unavailable, without exposing connection details. A healthy probe does not verify migration/schema completeness.

In another terminal at the repository root:

```powershell
Copy-Item frontend/.env.example frontend/.env.local
pnpm dev
```

`launchSettings.json` defines HTTP `http://localhost:5164`, which is the example proxy target. The HTTPS profile also defines `https://localhost:7235`: run the API with `--launch-profile https`, update the ignored frontend `.env.local` target accordingly, and opt into `VITE_API_PROXY_ALLOW_SELF_SIGNED=true` only for its self-signed local development certificate. Restart Vite after environment changes.

Frontend API requests use `/api/v1` on the same origin. Vite development forwards `/api` to `VITE_API_PROXY_TARGET` with no path rewrite. Production and Vite preview require the web server to route `/api` to the API. SQL configuration belongs only to the backend; never use `VITE_*` for secrets. See the root README for the full local setup.

`Frms.Api` implements the Phase 0 authentication endpoints and `/openapi/v1.json`; the retained SRS route catalogue is 501 contract scaffolding only. It has no production-account provisioning endpoint and no committed credentials. A real SQL Server integration fixture creates disposable accounts when `FRMS_TEST_CONNECTION_STRING` points to a disposable database. Phase 0 seed comprises five roles, Policy v1, and six approved DamageTypes only. ExtraFeeType schema/checks remain; its rows are deferred beyond Phase 0 until amounts and currency are approved.

## Local configuration validation

From the repository root, run only the suites that do not require a SQL test database:

```powershell
dotnet restore backend/Frms.slnx --disable-parallel
dotnet build backend/Frms.slnx --no-restore --configuration Release
dotnet test backend/tests/Frms.UnitTests/Frms.UnitTests.csproj --configuration Release
dotnet test backend/tests/Frms.ApiTests/Frms.ApiTests.csproj --configuration Release
dotnet test backend/tests/Frms.ArchitectureTests/Frms.ArchitectureTests.csproj --configuration Release
```

No migration or SQL integration test is required for this configuration task. The existing real SQL integration suite remains a separate release gate; its implementation is unchanged. See the root README and `docs/PHASE0_FOUNDATION.md` for broader Phase 0 validation and migration/redeploy reasoning.
