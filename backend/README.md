# FRMS backend — Phase 0

The backend is split into four assemblies:

- `Frms.Api`: HTTP, request/response DTOs, mapping, authentication middleware, and DI composition root.
- `Frms.Business`: command/result service contracts, authentication orchestration, `IClock`, and external provider interfaces.
- `Frms.DataAccess`: EF Core persistence entities, the authoritative initial migration, reference-data seeds, and repositories.
- `Frms.Infrastructure`: external-provider registration boundary; no provider behavior is fabricated in Phase 0.

The dependency direction is API → Business → DataAccess. `Program.cs` is the sole place where API references DataAccess/Infrastructure for registration. Controllers and background-job types cannot access repositories or `DbContext` directly; `Frms.ArchitectureTests` verifies this.

## Configure and run

Provide a SQL Server connection string and JWT signing key through environment variables. Never commit either value.

```powershell
$env:ConnectionStrings__Frms = 'Server=localhost;Database=Frms_Local;Trusted_Connection=True;TrustServerCertificate=True'
$env:Jwt__SigningKey = '<at-least-32-byte-secret>'
dotnet ef database update --project Frms.DataAccess --startup-project Frms.Api
dotnet run --project Frms.Api
```

`Frms.Api` exposes the Phase 0 authentication contract and `/openapi/v1.json` only. It has no account-provisioning endpoint and no seeded user credentials because neither identities nor credentials are approved source data. The reference seed includes the five roles, Policy v1, and the six approved damage types. Extra-fee monetary defaults need an approved deployment-data decision.
