# FRMS Backend Scaffold

This directory contains the ASP.NET Core backend technical and API-contract scaffold for FRMS. It is a foundation for module owners, not a business-complete implementation.

The governing requirements remain the repository-level source documents listed in [`../README.md`](../README.md). FRMS SRS V10 FINAL has the highest authority.

## Target framework

- .NET SDK: `10.0.401`, pinned by `../global.json`
- Target framework: `net10.0`
- ASP.NET Core and EF Core packages: `10.0.12`
- Test framework: NUnit 4
- Nullable reference types: enabled
- Preview SDKs/packages: not used

## Solution structure

```text
backend/
├── Frms.sln
├── Frms.Api/
│   ├── Authentication/
│   ├── Authorization/
│   ├── BackgroundJobs/
│   ├── Controllers/
│   ├── DependencyInjection/
│   ├── DTOs/{Requests,Responses}/
│   ├── Mapping/
│   ├── Middleware/
│   ├── Validation/
│   └── Program.cs
├── Frms.Business/
│   ├── Abstractions/{External,Security,Time}/
│   ├── Models/{Commands,Common,Results}/
│   ├── Services/{Interfaces,Implementations}/
│   ├── Calculations/ Events/ Exceptions/ Mapping/ Rules/ Validators/
│   └── DependencyInjection/
├── Frms.DataAccess/
│   ├── Persistence/{Entities,Configurations}/
│   ├── Repositories/{Interfaces,Implementations}/
│   ├── StoredProcedures/{Commands,Models,Sql}/
│   ├── Migrations/
│   └── DependencyInjection/
├── Frms.Infrastructure/
│   ├── Payments/ Email/ Ai/ Notifications/
│   └── DependencyInjection/
└── tests/
    ├── Frms.UnitTests/
    ├── Frms.ApiTests/
    ├── Frms.IntegrationTests/
    └── Frms.ArchitectureTests/
```

There is no `Frms.Worker`. Future scheduled execution belongs in `Frms.Api/BackgroundJobs` and must call Business Services only.

## Project references

| Project | FRMS project references |
|---|---|
| `Frms.Api` | `Frms.Business`, `Frms.DataAccess`, `Frms.Infrastructure` |
| `Frms.Business` | `Frms.DataAccess` |
| `Frms.DataAccess` | None |
| `Frms.Infrastructure` | `Frms.Business` |
| `Frms.UnitTests` | `Frms.Business` |
| `Frms.ApiTests` | `Frms.Api` |
| `Frms.IntegrationTests` | `Frms.Api`, `Frms.DataAccess` |
| `Frms.ArchitectureTests` | All four production assemblies for boundary verification |

The required request path is:

```text
Controller -> Business Service -> Repository -> EF/SP -> SQL Server
```

`Frms.Api` references DataAccess and Infrastructure only at the composition root. Controllers, middleware, and BackgroundJobs must not call Repository, `FrmsDbContext`, or stored procedures directly.

## Technical foundation present

`Frms.Api` currently provides:

- controller discovery;
- camel-case JSON and string enum serialization;
- built-in OpenAPI generation;
- process health endpoint `GET /health`;
- JWT Bearer options binding and authentication/authorization middleware;
- constants for the five locked role names;
- global exception middleware with sanitized `500 INTERNAL_SERVER_ERROR` and trace ID;
- `AddBusiness`, `AddDataAccess`, and `AddInfrastructure` registration extensions;
- Development, Testing, and Production configuration;
- HTTPS redirection outside Testing.

`AddBusiness` and `AddInfrastructure` do not register placeholder or fake implementations. `AddDataAccess` registers only `FrmsDbContext` with the SQL Server provider.

## Restore, build, run, and test

Run from the repository root:

```powershell
dotnet --info
dotnet restore backend/Frms.sln
dotnet build backend/Frms.sln --no-restore
dotnet test backend/Frms.sln --no-build --no-restore
dotnet format backend/Frms.sln --verify-no-changes --no-restore
dotnet run --project backend/Frms.Api --no-build --no-restore --launch-profile http
```

Default local technical endpoints:

- Health: `http://localhost:5029/health`
- OpenAPI contract: `http://localhost:5029/openapi/v1.json`
- Example requests: `Frms.Api/Frms.Api.http`

The scaffold uses the built-in ASP.NET Core OpenAPI document. No Swagger UI package was added because the governing documents do not lock a UI/package choice.

## Configuration and secrets

`Frms.Api/appsettings*.json` contains placeholders only. Supply real local values through a secret store or environment variables, for example:

```powershell
$env:ConnectionStrings__FrmsDatabase = "<local SQL Server connection string>"
$env:Jwt__SigningKey = "<local development signing key>"
dotnet run --project backend/Frms.Api --launch-profile http
```

Never commit database passwords, JWT signing keys, MoMo secrets, email credentials, AI keys, notification credentials, or production passwords.

The application does not call `EnsureCreated`, create a database, or run migrations at startup.

## API contract scaffold

The solution contains 88 action signatures from the SRS API catalogue:

| Controller | API groups | Scaffold actions |
|---|---|---|
| `AuthController` | AUTH-001..005 | register, customer/employee login, current account, profile update |
| `CatalogController` | CAT-001..004 | browse/get facility and list/get unit type |
| `AiController` | AI-001 | unit-type recommendation |
| `ReservationsController` | RES-001..005 | create/list/get/confirm/cancel reservation |
| `BillingController` | BIL-001..002, PAY-001..004 | invoices, payment initiation/detail, callback |
| `ContractsController` | CON-001..005, VIS-001..002, INS-008 | contracts, renewal, billing/return summary, create visits, finalize return |
| `VisitsController` | VIS-003..006, OPS-002/003/005 | visits, schedule/cancel, check-in/out, confirm return |
| `StaffOperationsController` | OPS-001/004 | staff work items and handover |
| `InspectionsController` | INS-001..007, INS-009..010 | inspection, claim, damage, fees, evidence, decision, damage types |
| `SupportTicketsController` | SUP-001..006 | create/list/get/cancel/assign/complete ticket |
| `StorageUnitsController` | UNIT-001..005 | list/create/get/update/status |
| `BusinessController` | BOM-001..014 | facilities, unit types, policies, discounts, extra-fee types |
| `ReportsController` | REP-001..004 | facility and business reports/export |
| `AdminController` | ADM-001..012 | users, customers, employees, assignments, histories, credential resend |

Every business action returns `501 Not Implemented`. Actions do not inject a Service, Repository, or DbContext; do not change state; do not access a database/provider; and do not return fake business data. The OpenAPI operation description identifies this status.

## DTO contracts present

Request DTOs exist only where request fields are sufficiently defined. They cover:

- customer registration and customer/employee login;
- AI unit-type recommendation;
- reservation create/confirm/cancel;
- invoice and first-month MoMo payment initiation;
- renewal and visit operations;
- inspection damage, fee, evidence, completion, and decision;
- support ticket create/cancel/assign/complete;
- storage-unit create/update/status;
- facility, price, policy, discount, and extra-fee administration;
- employee creation and assignment.

Response DTOs cover common envelopes/errors, authentication tokens, catalogue and availability, reservations, invoices/payments, visits/handover/return, renewal, settlement, support tickets, and the currently defined report schemas.

API Request/Response DTOs, Business Commands/Results, EF Entities, and Stored Procedure Models remain separate boundaries.

## Current backend limitations

The scaffold intentionally does not implement:

- business logic, lifecycle transitions, calculations, business validators, or rule implementations;
- Business Service or Repository implementations;
- EF Core entities/configurations, migrations, or seed data;
- stored-procedure or trigger SQL;
- login/register behavior, password hashing, or JWT token generation;
- Facility/resource-ownership authorization handlers;
- MoMo, email, AI, or notification adapters;
- scheduled jobs or hosted-service implementations;
- business, real SQL Server, stored-procedure, trigger, or concurrency tests;
- fake production providers or fake successful endpoint responses.

`FrmsDbContext` is empty: it has no `DbSet`, entity mapping, business-specific `OnModelCreating`, seed, or migration-on-start behavior.

## Contracts blocked by incomplete canonical schemas

These areas stop at the route/action boundary until the owner approves exact schemas or signatures:

- AUTH-005 profile update fields and patch semantics;
- PAY-004 raw MoMo callback wire contract;
- Contract detail `extensions` element schema;
- Inspection nested damage/fee/evidence schemas;
- selected UNIT, BOM, ADM, and CON list/detail/success response schemas;
- BOM-003 and ADM-006 update fields;
- REP-002 and REP-004 report filters/export schema;
- Business Service, Repository, and provider method signatures that depend on those schemas.

Do not fill these gaps with `object`, `dynamic`, dictionaries, a generic repository, Unit of Work, or invented Commands/Results.

## Technical tests

The current suite verifies:

- application startup in Testing;
- `/health` success;
- JWT Bearer default scheme;
- sanitized exception response and trace ID;
- 88 OpenAPI operations and scaffold descriptions;
- direct scaffold actions return 501;
- exactly five locked roles;
- SQL Server provider registration and an empty EF model;
- exact production project references and presentation boundaries;
- no BackgroundJob implementation type;
- no `Frms.Worker` project.

The latest scaffold verification passed 17 tests: 1 Unit, 8 API, 1 Integration, and 7 Architecture tests. These tests do not claim that business workflows or SQL Server lifecycle behavior are implemented.

## Module handoff

| Module | Owner | Allowed areas | Contracts still required | API IDs | Not implemented yet |
|---|---|---|---|---|---|
| Authentication/Profile | — | Api Authentication/DTOs; Business Security/Services; DataAccess Repositories | auth/security service and repository signatures | AUTH-001..005 | login/register/profile/token/password logic |
| Catalogue/AI | — | Api Catalog/Ai; Business Services/External; Infrastructure Ai | catalogue service; AI provider methods | CAT-001..004, AI-001 | queries and recommendations |
| Reservation | — | Api Reservations; Business Models/Rules; DataAccess Repositories/SP | reservation service/repository | RES-001..005 | lifecycle and capacity transaction |
| Billing/Payment | — | Api Billing; Business External; DataAccess; Infrastructure Payments | billing contracts; payment gateway methods | BIL-001..002, PAY-001..004 | invoice/payment/callback behavior |
| Contract/Visit/Handover | — | Api Contracts/Visits/StaffOperations; Business; DataAccess/SP | contract/visit contracts | CON-001..005, VIS-001..006, OPS-001..005 | handover/access/renewal/return lifecycle |
| Inspection/Settlement | — | Api Inspections; Business Rules; DataAccess/SP | inspection/damage/settlement contracts | INS-001..010 | claims, damage, fees, finalization |
| Support | — | Api SupportTickets; Business; DataAccess | support contracts | SUP-001..006 | ticket lifecycle and assignment |
| Facility Units | — | Api StorageUnits; Business; DataAccess | storage-unit contracts | UNIT-001..005 | create/update/status behavior |
| Business Operations | — | Api Business; Business Rules; DataAccess | facility/policy/discount/fee contracts | BOM-001..014 | operational configuration behavior |
| Reporting | — | Api Reports; Business; DataAccess/SP | reporting queries/contracts | REP-001..004 | report generation/export |
| Administration | — | Api Admin; Business Security; DataAccess; Infrastructure Email | admin and email-provider contracts | ADM-001..012 | account administration/audit/email behavior |
