# FRMS backend — Phase 0

The backend is split into four assemblies:

- `Frms.Api`: HTTP, request/response DTOs, mapping, authentication middleware, and DI composition root.
- `Frms.Business`: command/result service contracts, authentication orchestration, `IClock`, and external provider interfaces.
- `Frms.DataAccess`: EF Core persistence entities, the authoritative initial migration, reference-data seeds, and repositories.
- `Frms.Infrastructure`: external-provider registration boundary; no provider behavior is fabricated in Phase 0.

The HTTP path is API → Business → repository → SQL Server. Infrastructure implements Business-owned interfaces. `Program.cs` composes DataAccess/Infrastructure registrations. Controllers and background-job types cannot access repositories or `DbContext` directly; `Frms.ArchitectureTests` verifies this.

## Configure and run

Provide a SQL Server connection string and JWT signing key through environment variables. Never commit either value.

```powershell
$env:ConnectionStrings__Frms = '<your-local-sql-server-connection-string>'
$env:Jwt__SigningKey = '<at-least-32-byte-secret>'
dotnet tool restore
dotnet ef database update --project Frms.DataAccess --startup-project Frms.Api
dotnet run --project Frms.Api
```

`Frms.Api` implements the Phase 0 authentication endpoints and `/openapi/v1.json`; the retained SRS route catalogue is 501 contract scaffolding only. It has no production-account provisioning endpoint and no committed credentials. A real SQL Server integration fixture creates disposable accounts when `FRMS_TEST_CONNECTION_STRING` points to a disposable database. Phase 0 seed comprises five roles, Policy v1, and six approved DamageTypes only. ExtraFeeType schema/checks remain; its rows are deferred beyond Phase 0 until amounts and currency are approved.

Run solution validation from the repository root using `Frms.slnx`. Real SQL tests require `FRMS_TEST_CONNECTION_STRING` targeting a disposable `Frms_Test_*` database and fail if it is absent. No CI workflow is configured; the root README documents the full local commands and temporary API setup for Postman. See `docs/PHASE0_FOUNDATION.md` for migration/redeploy reasoning.
