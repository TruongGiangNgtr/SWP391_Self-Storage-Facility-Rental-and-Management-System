# FRMS backend — Phase 0

The backend is split into four assemblies:

- `Frms.Api`: HTTP, request/response DTOs, mapping, authentication middleware, and DI composition root.
- `Frms.Business`: command/result service contracts, authentication orchestration, `IClock`, and external provider interfaces.
- `Frms.DataAccess`: EF Core persistence entities, the authoritative initial migration, reference-data seeds, and repositories.
- `Frms.Infrastructure`: external-provider registration boundary; no provider behavior is fabricated in Phase 0.

The HTTP path is API → Business → repository → SQL Server. Infrastructure implements Business-owned interfaces. `Program.cs` composes DataAccess/Infrastructure registrations. Controllers and background-job types cannot access repositories or `DbContext` directly; `Frms.ArchitectureTests` verifies this.

## Configure and run

From the repository root (`C:\SWP391`), create the ignored `backend/Frms.Api/.env` file locally and add the approved `ConnectionStrings__FrmsDb`, `Jwt__SigningKey`, and `Payment__PayOS__*` values from the SRS. Preserve an existing local file and never commit it:

```powershell
dotnet run --project .\backend\Frms.Api
```

From `C:\SWP391\backend`, use `dotnet run --project .\Frms.Api`. The default launch profile selects Development. Because Development reads the local `.env` file automatically, you do not need to set the same `$env:...` values again whenever a new terminal is opened. Restart the backend after changing `.env`.

In Development only, DotNetEnv loads `.env` from the API's `ContentRootPath`, independently of the working directory. `ConnectionStrings__FrmsDb` maps to `ConnectionStrings:FrmsDb`. Windows/PowerShell environment variables and command-line configuration have higher priority than the file; the loader does not modify process environment variables. A missing file is allowed, and existing configuration sources remain available. Production, Staging and Testing ignore the file.

The existing `ConnectionStrings:Frms` name remains supported and takes precedence over the `FrmsDb` alias; set only one name locally. Missing connection settings fail clearly. This runtime loader does not configure EF design-time tools, run migrations, create a test database or change tables. SQL Server must be running for database access, but startup itself does not validate the schema.

The real `.env` is ignored by Git and excluded from build/publish items. Create `backend/Frms.Api/.env` yourself; there is no backend `.env.example`. Never commit connection credentials, database files, local settings, JWT keys or payOS ApiKey/ChecksumKey, and never print secrets, signed payment URLs or raw signed webhooks. `TrustServerCertificate=True` is for approved local test configuration only, not a production TLS policy.

Use only `Payment:PayOS`; old VNPay environment keys are no longer bound to an active provider:

```text
Payment__PayOS__ClientId=<configured locally>
Payment__PayOS__ApiKey=<secret/configured locally; never share or commit>
Payment__PayOS__ChecksumKey=<secret/configured locally; never share or commit>
Payment__PayOS__ApiBaseUrl=https://api-merchant.payos.vn
Payment__PayOS__ReturnUrl=http://localhost:5173/customer/payments/result
Payment__PayOS__CancelUrl=http://localhost:5173/customer/payments/result
Payment__PayOS__WebhookUrl=https://lying-ladder-showroom.ngrok-free.dev/api/v1/payments/payos/webhook
Payment__PayOS__ExpiryMinutes=15
```

payOS has no sandbox/staging: its API is production. This revision authorizes **code/mock tests only**. Do not create real links, call /confirm-webhook, register a live webhook or transfer money until separately approved.
Missing payment credentials do not prevent startup; PAY-001 raises EXTERNAL_PROVIDER_NOT_CONFIGURED before creating an attempt.
POST /api/v1/invoices/{invoiceId}/payments/payos needs only the UUID Idempotency-Key header; no caller amount/redirect is used. Use a new UUID for a deliberate action and reuse it for automatic retries.
PAY-003 is the authorized detail read and excludes session URL/idempotency key. PAY-004 is anonymous POST JSON /api/v1/payments/payos/webhook, verified before mapping ProviderOrderCode and invoking authoritative SQL.
Only Created may invoke the provider; timeouts leave PENDING and do not trigger automatic HTTP create retries. Typed HttpClient keeps exact wire JSON/signing inside Infrastructure, allows deterministic fake-handler tests, and avoids SDK logging/retry behavior in the Created-only boundary. No payOS SDK dependency is added.
Description FRMS is display-only; SQL sequence orderCode starts1000, filtered-unique, and is authoritative identity. Verified transactionDateTime is interpreted UTC+7 by owner approval then persisted UTC. Signed unknown/sample code returns200 without mutation.
Browser return/cancel do not mark success/failure; frontend refreshes PAY-003. First month remains offline with no Invoice/Payment; PAY-002 retired. MOMO/VNPAY rows and migrations are retained, never rewritten. Historical VNPay algorithm/options fixtures now compile only in the unit-test assembly.
The forward migration adds nullable BIGINT ProviderOrderCode, dbo.ProviderOrderCodeSequence, unique filtered index and PAYOS method support. Down refuses PAYOS/order-code evidence; do not use it to erase historical financial data.

[ ] Separately approved live payOS E2E
[ ] Successful real payment/webhook and Invoice.PaidAt evidence
[ ] EPS-01 release acceptance (NOT COMPLETE)

The existing `/health` endpoint probes SQL Server through the registered `FrmsDbContext`. It returns 200 when reachable and 503 when unavailable, without exposing connection details. A healthy probe does not verify migration/schema completeness.

In another terminal at the repository root, create the ignored `frontend/.env.local` yourself with `VITE_API_PROXY_TARGET=http://localhost:5164`. The frontend template was removed by current main; do not depend on copying it. If a frontend-owned `.env.example` is supplied later, it must contain only public frontend settings, never backend secrets:

```powershell
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

Payment migration, persistence and concurrency changes require the real SQL integration gate with `FRMS_TEST_CONNECTION_STRING` pointing only to an approved disposable `Frms_Test_*` database.
