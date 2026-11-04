# FRMS Technical Baseline

## 1. Purpose and authority

This document summarizes the owner-approved current `main` engineering baseline under `docs/FRMS_SRS_V11.md`. Code-present scope is approved for documentation; code-absent proposals are outside the current baseline. Release tests and deployed database state remain separate evidence. Earlier SRS versions provide history where V11 has not superseded them. This document does not replace the SRS. A new semantic change still requires an approved SRS revision with its associated change record.

Exact framework/runtime versions are not specified by the SRS. They must be selected deliberately, recorded in project manifests and lockfiles, and supported consistently across Development, Testing, and Production. A dependency upgrade must not change business semantics implicitly.

## 2. Baseline stack

| Area | Baseline | Required use |
|---|---|---|
| Frontend | React + TypeScript | Role-based web portals and typed API client |
| Backend | ASP.NET Core Web API + C# | REST API, authentication/authorization, services |
| Persistence | PostgreSQL | Authoritative relational state and transactions |
| ORM | Entity Framework Core | Mapping, repositories, migrations, ordinary queries |
| Critical database operations | PostgreSQL routines | Atomic multi-table lifecycle and concurrency control |
| Backend tests | NUnit | Unit, controller, repository, integration, and security suites |
| API tests | Postman | Functional and regression contract testing |
| Browser E2E | Playwright | Seven end-to-end business journeys |
| API documentation | Swagger/OpenAPI | Executable contract synchronized with SRS and frontend types |
| Password hashing | BCrypt | Configurable work factor, initial value 12 |
| Authentication | ASP.NET Core Authentication + JWT Bearer | FRMS-issued access tokens; no ASP.NET Core Identity |
| Design tools | Draw.io, Figma | Architecture/behavior diagrams and role-specific prototypes |

Runtime AI is optional, provider-agnostic, non-authoritative, and isolated behind `IAiRecommendationProvider`.

## 3. System context

```text
Browser (React + TypeScript)
        |
        | HTTPS / JSON / multipart / CSV
        v
ASP.NET Core Web API
        |
        | EF Core + PostgreSQL routines
        v
PostgreSQL

External adapters: payOS, email, notification delivery, optional AI
```

FRMS retains three logical application layers. `Frms.Infrastructure` is a supporting adapter assembly, not an additional business layer. Release 1 has no separate Worker project.

The five fixed roles are:

```text
CUSTOMER
FACILITY_STAFF
FACILITY_MANAGER
BUSINESS_OPERATIONS_MANAGER
SYSTEM_ADMINISTRATOR
```

Authorization is the intersection of authenticated identity, active account, role, facility scope, resource ownership, and current business state.

## 4. Backend architecture

### 4.1 Dependency direction

```text
HTTP:      Frms.Api -> Frms.Business -> Frms.DataAccess -> PostgreSQL
Scheduled: Frms.Api/BackgroundJobs -> Frms.Business -> Frms.DataAccess -> PostgreSQL
External:  Frms.Infrastructure -> Frms.Business provider interfaces
```

Permitted calls:

```text
Controller -> Service
Api BackgroundJob -> Service
Service -> Repository
Repository -> EF Core / DbContext
Repository -> PostgreSQL routine access
Infrastructure adapter -> Business provider interface
```

`Frms.Api/Program.cs` is the primary dependency-injection composition root and may reference DataAccess/Infrastructure registration extensions. That assembly reference does not permit Controllers, middleware or handlers to call their implementations directly.

Forbidden calls:

```text
Controller -> Repository or DbContext
Controller -> external-provider implementation
Api BackgroundJob -> Repository or DbContext
Service -> DbContext
Business -> Api or Infrastructure implementation
Frontend -> Database
Repository -> Controller
```

### 4.2 Project structure

```text
backend/
├── Frms.Api/
│   ├── Controllers/
│   ├── DTOs/Requests/
│   ├── DTOs/Responses/
│   ├── Mapping/
│   ├── Validation/
│   ├── Middleware/
│   ├── Authentication/
│   ├── Authorization/
│   ├── BackgroundJobs/
│   ├── DependencyInjection/
│   └── Program.cs
├── Frms.Business/
│   ├── Services/Interfaces/
│   ├── Services/Implementations/
│   ├── Models/Commands/
│   ├── Models/Results/
│   ├── Models/Common/
│   ├── Abstractions/Security/
│   ├── Abstractions/External/
│   ├── Abstractions/Time/
│   ├── Validators/
│   ├── Rules/
│   ├── Calculations/
│   ├── Mapping/
│   ├── Events/
│   └── Exceptions/
├── Frms.DataAccess/
│   ├── Repositories/Interfaces/
│   ├── Repositories/Implementations/
│   ├── Persistence/Entities/
│   ├── Persistence/Configurations/
│   ├── Persistence/FrmsDbContext.cs
│   ├── StoredProcedures/Commands/
│   ├── StoredProcedures/Models/
│   ├── StoredProcedures/Sql/
│   ├── Migrations/
│   └── DependencyInjection/
├── Frms.Infrastructure/
│   ├── Payments/
│   ├── Email/
│   ├── Ai/
│   ├── Notifications/
│   └── DependencyInjection/
└── tests/
    ├── Frms.UnitTests/
    ├── Frms.ApiTests/
    ├── Frms.IntegrationTests/
    └── Frms.ArchitectureTests/

tests/
├── postman/
└── e2e/playwright/
```

### 4.3 Layer ownership

| Layer | Owns | Must not own |
|---|---|---|
| Presentation | HTTP, DTOs, auth middleware/policies, status mapping, version routing | SQL, DbContext, authoritative calculations, lifecycle orchestration |
| Business | Use-case orchestration, Commands/Results, validation, authorization context, calculations, provider abstractions, `IClock` | API DTOs, EF entity exposure, direct DbContext access, provider implementations |
| Data Access | Repositories, EF Core, entity configuration, migrations, SP invocation | Actor/business workflow decisions |
| Infrastructure support | External-provider adapter implementations and wire DTOs | Business orchestration, repository/DbContext access |
| API BackgroundJobs | In-process scheduling and Business service invocation | Repository/DbContext/SP access |

Critical operations use this boundary:

```text
Service -> Repository -> PostgreSQL routine -> transaction
```

Simple reads and CRUD may use repository/EF Core. Internal events are allowed only for hooks such as audit, notification, logging, cache invalidation, or post-transaction activity.

Boundary types are mandatory:

```text
API Request DTO -> API Mapping -> Business Command
Business Result -> API Mapping -> API Response DTO
Business Command/Result != API DTO != EF Entity
```

## 5. Frontend architecture

```text
frontend/src/
├── app/
├── routes/
├── layouts/
├── pages/
├── components/
├── features/
├── api/
├── hooks/
├── models/
├── auth/
└── utils/
```

Frontend rules:

- Organize business-specific code by feature and share only genuinely reusable components/utilities.
- Use TypeScript domain/API types synchronized with Swagger and SRS schemas.
- Centralize the HTTP client, bearer-token handling, error conversion, bounded polling, and cancellation.
- Route groups and navigation may improve UX, but server authorization is authoritative.
- Render timestamps in `Asia/Ho_Chi_Minh`; keep month strings as month values.
- Do not calculate authoritative price, discount, capacity, settlement, or payment success in the browser.
- Always implement loading, empty, validation, forbidden, not-found, conflict, and unexpected-error states.

## 6. API contract

| Concern | Baseline |
|---|---|
| Base route | `/api/v1` |
| JSON naming | `camelCase` |
| Enum representation | String |
| Timestamp representation | UTC ISO-8601 |
| Month representation | `YYYY-MM` |
| Pagination defaults | `page=1`, `pageSize=20` |
| Pagination maximum | `pageSize=100` |
| Error consumption | Stable `error.code`, never parsed message text |
| Binary evidence | `multipart/form-data` |
| Report export | UTF-8 CSV response |

Request DTO, response DTO, Business Command, Business Result and persistence Entity are different concerns. `Frms.Api` owns DTO boundary mapping. Business service contracts must not reference API DTOs or expose EF entities. API responses must never serialize EF entities directly. API errors use the SRS envelope, stable codes, correct HTTP status, and a `traceId`.

Any API change is one atomic contract change across:

1. approved SRS revision with its associated change record when semantics change;
2. request/response DTOs and controller behavior;
3. Swagger/OpenAPI;
4. frontend TypeScript types/client;
5. CTL/API/SEC and applicable E2E tests.

## 7. Authentication and authorization

### Customer

- Identifier: `PhoneNumber`.
- Role: `CUSTOMER`.
- Endpoint: `POST /api/v1/auth/customer/login`.

### Employee

- Identifier: `Email`.
- Roles: the four non-customer roles.
- Endpoint: `POST /api/v1/auth/employee/login`.

### Shared security contract

- Custom `UserAccount`/`UserRole`; ASP.NET Core Identity is not used.
- BCrypt password validation: 8-64 characters, encoded input no more than 72 bytes.
- BCrypt work factor starts at 12 and is configurable.
- JWT contains at least `sub`/`userAccountId` and `role`; expiry is mandatory and environment-configurable.
- Refresh tokens are not required for Release 1.
- Account status must be `ACTIVE`.
- Server re-checks facility/resource state when it matters; token convenience claims are not sufficient.
- `Email` and `PhoneNumber` are globally unique in both application validation and database constraints.
- Unknown login identifiers do not create fabricated `LoginHistory.UserAccountId` values.

## 8. Data and lifecycle baseline

Release 1 has exactly 28 persisted entities: the historical 27 plus the approved HandoverUnitAssignment in SRS section 9.2A. The model intentionally has no separate Booking, MonthlyRentalFee, ReturnProcess, Renewal, Refund, DamageFee, DailyTask, Report, DiscountRedemption, or Maintenance table.

Important state rules:

- Reservation is month-based capacity, not physical-unit allocation.
- Concrete `StorageUnit` selection occurs at handover.
- Storage unit states are `AVAILABLE`, `IN_USE`, `INSPECTION`, `MAINTENANCE`.
- Reservation states are `PENDING_DEPOSIT`, `CONFIRMED`, `COMPLETED`, `CANCELLED`.
- Contract states are `ACTIVE`, `COMPLETED`, `TERMINATED`.
- Damage decisions are `PENDING -> APPROVED|REJECTED` and belong to the same-facility Manager.
- Policy versions are immutable historical rows.
- `ContractExtension`, `LoginHistory`, and `AuditLog` are append-only.
- No normal hard delete is allowed for historical core business data.

### 8.1 Transaction owners

The authoritative stored-procedure catalogue is in SRS section 9.9. At minimum, treat reservation creation/confirmation, handover, access/return creation, actual return, inspection claim/damage/fees/evidence/completion, return finalization, renewal, invoice generation, payment results, policy versioning, support assignment/completion, account status, and notification queue/retry as database-integrity-sensitive operations.

Triggers enforce bounded invariants and legal transitions. They must not orchestrate Complete Handover or Finalize Return.

### 8.2 Migration and seed

- Migrations are version-controlled and can recreate an empty database.
- Environments derive from source + migrations + required seed + environment configuration.
- Seed the five roles and approved policy/fee/damage master values from the SRS.
- UnitType master rows are deployment data; do not invent a Release 1 UnitType CRUD workflow.
- Schema/data hot fixes made only in a developer database are not deployment artifacts.

## 9. Time, currency, configuration, and logging

| Concern | Baseline |
|---|---|
| Persistence time | UTC |
| API time | UTC ISO-8601 |
| Business/display timezone | `Asia/Ho_Chi_Minh` / GMT+7 |
| Time abstraction | `IClock` or equivalent |
| Currency type | `decimal` |
| Environments | Development, Testing, Production |
| Technical log | `ILogger<T>` |
| Business/security audit | `AuditLog` |

Business calendar rules use GMT+7; month-only values are not timestamp conversions. Configuration owns policy/provider settings and environment-specific token lifetime. Passwords, hashes, JWT signing keys, database credentials, payOS secrets, AI keys, notification credentials, access tokens, and binary evidence must never be committed or logged.

## 10. External integration boundaries

| Integration | Interface boundary | Failure rule |
|---|---|---|
| payOS | `IPaymentGateway` | Provider failure never marks an invoice paid; callback application is idempotent |
| AI Size Guide | `IAiRecommendationProvider` | Optional and non-authoritative; failure does not block manual flow |
| Initial employee credential email | `IEmailService` | Failure leaves account inactive; plaintext is not stored or logged |
| Notifications — OD-06 approved | Internal business event -> NotificationLog.IsInApp -> own-Customer read | In-app visibility follows commit independently of SMTP/SENT/SentAt. Pg018 excludes in-app-only events from the legacy email worker. Other email flows are separate; deployment/tests pending. |

Adapter wire DTOs do not leak into domain contracts. Use controlled 502/503 behavior for provider failures where applicable.

## 11. Engineering conventions

### C#

- PascalCase for classes, methods, properties, enums, DTOs; `I` prefix for interfaces.
- Async methods use the `Async` suffix.
- camelCase parameters/locals; `_camelCase` private fields.
- Backend I/O is asynchronous where the underlying operation is asynchronous.
- Propagate `CancellationToken` through Controller -> Service -> Repository/provider.

### React/TypeScript

- PascalCase components/pages/types and `PascalCase.tsx` component files.
- Hooks begin with `use`; functions/variables/API modules/utilities use camelCase.
- Constants use `UPPER_SNAKE_CASE`; feature folders and routes use kebab-case.

### Validation placement

```text
DTO      -> required, format, primitive range
Service  -> actor, ownership, lifecycle, business rules
Database -> integrity, uniqueness, transaction, concurrency invariants
```

## 12. Quality strategy

| Prefix | Test group | Primary evidence |
|---|---|---|
| `UT-*` | Business unit | Services, validators, calculators, rules |
| `CTL-*` | Controller/DTO contract | Mapping and HTTP contract |
| `DAL-*` | Repository/EF integration | Repository and query behavior |
| `DBT-*` | Database integrity/lifecycle | SQL constraints, triggers, SPs, transitions |
| `CON-*` | Concurrency/idempotency | Races, locking, rollback, duplicate actions |
| `SEC-*` | Authentication/authorization | BCrypt, JWT, RBAC, facility/ownership scope |
| `API-*` | Postman/API | Functional and regression behavior |
| `INT-*`, `JOB-*` | Providers/jobs | Integration isolation and retry behavior |
| `E2E-*` | Playwright | Seven business journeys |
| `REG-*`, `ACC-*` | Regression/acceptance | Whole-system confidence |

Real PostgreSQL is mandatory for routine, trigger, partial-index, locking, isolation, and concurrency evidence. EF Core InMemory is insufficient for these claims.

`Frms.ArchitectureTests` must reject Controller/Api BackgroundJob access to Repository or DbContext, Business references to Api/Infrastructure implementations, and service contracts that expose API DTOs or EF entities.

Critical blockers include the `DBT-01..25` baseline, capacity races, same-unit handover, renewal versus reservation, inspection/damage decision races, duplicate callbacks/invoices/return finalization, and role/facility/resource isolation.

## 13. Build and delivery gates

The repository contains backend projects, frontend package manifests and implemented routes. Build and delivery verification uses the existing solution and package scripts. CI and release evidence should cover:

1. restore and build the backend;
2. install from the frontend lockfile and build/type-check the frontend;
3. run unit and controller tests;
4. run `Frms.ArchitectureTests`;
5. provision real PostgreSQL and run DAL/DBT/CON/SEC suites;
6. verify clean-database migration and seed;
7. run Postman regression and Playwright `E2E-F01..F07` where the gate requires them;
8. verify Swagger/TypeScript synchronization;
9. scan for committed secrets;
10. retain migration version, SRS identifier, Swagger snapshot, and test summaries as release evidence.

A feature is Done only when code, contracts, authorization, validation, error behavior, logging/audit, migrations/seed, traceability, and applicable automated tests agree.

## 14. Manifest versions and deployment configuration

Use the existing project manifests and lockfiles for selected runtime and library versions. Current repository source does not establish production deployment configuration. Verify the deployed choices for:

- .NET SDK and Node.js runtime compatibility with committed project manifests;
- PostgreSQL server version and applied migration history;
- CI provider and environment topology;
- actual configured email, notification, and optional AI adapters;
- production secret store, observability backend, and hosting platform.

Deployment configuration must preserve the approved current-main business model and API contracts.
