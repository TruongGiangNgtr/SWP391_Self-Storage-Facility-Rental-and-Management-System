# AGENTS.md - FRMS Repository Instructions

This file defines how coding agents and contributors must work in this repository. It applies to the repository root and every descendant path unless a deeper `AGENTS.md` explicitly narrows a non-business implementation detail.

## 1. Project identity

FRMS is the **Self-Storage Facility Rental and Management System**. It supports five business roles and seven end-to-end flows for reservation, handover, rental operation, billing, facility operation, renewal/return, and support.

The implementation target is a React + TypeScript frontend, an ASP.NET Core Web API backend, and SQL Server persistence through Entity Framework Core and authoritative stored procedures.

## 2. Source-of-truth order

Use this order when requirements conflict:

1. `project_sources/02-FRMS_SRS_V10.md`
2. `project_sources/03-FRMS_Data_Dictionary_V2_1.md`
3. FRMS Scope V8, when added to the repository
4. Approved ADRs and implementation decisions that do not change SRS semantics
5. Existing implementation details

`project_sources/01-Topic.pdf` is a scope summary, not a replacement for the SRS.

FRMS SRS V10 FINAL at `project_sources/02-FRMS_SRS_V10.md` is the sole governing SRS and implementation baseline. Earlier SRS versions must not be used as implementation authority.

An ADR or change record alone does not override the SRS. A semantic change becomes authoritative only after it is incorporated into an approved SRS revision. Record ambiguity and request an approved clarification instead of resolving it only in code or an ADR.

## 3. Non-negotiable behavior

Never silently introduce:

- a sixth business role;
- an eighth business flow;
- a new persisted entity/table, state, stateful workflow, permission, approval process, payment obligation, or financial formula;
- physical `StorageUnit` assignment at reservation time;
- `ASSIGNED` or `RESERVED` unit statuses;
- a standalone DAO layer;
- ASP.NET Core Identity;
- a direct Controller-to-Repository/DbContext or Service-to-DbContext dependency;
- an Api BackgroundJob-to-Repository/DbContext/Stored Procedure dependency;
- a Business-to-Api or Business-to-Infrastructure-implementation dependency;
- hard deletion or mutation of protected historical business data;
- a business decision hidden only in UI code, a trigger, a seed, a test fixture, or an external-provider adapter.

When the SRS is silent on business semantics, stop at the interface/configuration boundary and raise a decision request. Technical refinement is allowed only when it preserves approved semantics.

## 4. Fixed architecture boundaries

Allowed dependency direction:

```text
HTTP:      React -> Controller -> Business Service -> Repository -> EF/SP -> SQL Server
Scheduled: Api BackgroundJob -> Business Service -> Repository -> EF/SP -> SQL Server
External:  Infrastructure Adapter -> Business-owned provider interface
```

Responsibilities:

- **Presentation (`Frms.Api`)**: HTTP, request/response DTOs, authentication, authorization policies, middleware, status mapping, API versioning.
- **Business (`Frms.Business`)**: orchestration, Commands/Results, lifecycle and ownership validation, calculations, provider interfaces, `IClock`.
- **Data Access (`Frms.DataAccess`)**: repositories, EF Core, `FrmsDbContext`, entity configuration, stored-procedure calls, migrations.
- **Supporting adapters (`Frms.Infrastructure`)**: MoMo, email, notification and optional AI implementations behind Business interfaces.
- **Scheduled execution (`Frms.Api/BackgroundJobs`)**: in-process jobs that call Business services only; SQL Server Agent may call authoritative stored procedures directly.
- **Frontend**: role-specific UI and typed API consumption; it is never authoritative for capacity, money, payment status, authorization, or lifecycle state.

Controllers must be thin. Repositories must not decide business transitions. Request/Response DTOs, Business Commands/Results and persistence entities must remain separate types. DTO mapping belongs to `Frms.Api`; Business service contracts must not reference API DTOs or expose EF entities.

`Frms.Api/Program.cs` may reference DataAccess and Infrastructure registration extensions only as the dependency-injection composition root. Controllers, middleware, handlers and Api BackgroundJobs must not call Repository, DbContext or Stored Procedures directly.

Critical multi-table lifecycles must use the authoritative stored procedure and SQL Server transaction identified by the SRS. Delegates or internal events may run post-transaction hooks but must not hide critical transactions.

## 5. Business invariants to preserve

- Reservations hold capacity by `Facility + UnitType + month range`; a physical unit is chosen only during handover.
- A customer may own multiple contracts when all business rules permit it.
- Policy rows and `ContractExtension`, `LoginHistory`, and `AuditLog` history are append-only where specified.
- Captured prices, issued invoice values, reservation values, and historical policy values are immutable snapshots.
- Return finalization is blocked while a related `DamageRecord` is `PENDING`.
- A referenced Discount keeps immutable customer, percentage, and effective-period semantics.
- The first-month Rental Fee does not use the Contract Discount; eligible later invoices may use it.
- Payment callbacks, invoice generation, return confirmation/finalization, claims, and scheduled jobs must be retry-safe or idempotent as specified.
- Facility and resource ownership checks are server-side requirements, not UI filters.
- `SYSTEM_ADMINISTRATOR` does not automatically receive business-data mutation authority.

Before changing a lifecycle, consult the state-transition, business-rule, API, stored-procedure, and test sections of the SRS together.

## 6. Authentication, security, and privacy

- Customer login uses `PhoneNumber`; employee login uses `Email`.
- Use custom `UserAccount`/`UserRole`, ASP.NET Core Authentication/Authorization, JWT Bearer, and BCrypt.
- BCrypt work factor starts at 12 and is configuration-driven.
- Password length is 8-64 characters and encoded BCrypt input must not exceed 72 bytes.
- Enforce an `ACTIVE` account before issuing or accepting authenticated business access.
- Re-check role, facility scope, resource ownership, and current business state on the server.
- Never log or expose plaintext passwords, password hashes, JWTs, provider secrets, connection credentials, or binary inspection evidence.
- Keep technical logging (`ILogger<T>`) separate from business/security audit (`AuditLog`).
- Every API error response includes a trace ID and a stable machine-readable error code.
- Do not infer authorization from JWT convenience claims when authoritative data may have changed.

## 7. API and frontend contracts

- REST base path: `/api/v1`.
- JSON property names: `camelCase`.
- Enum values: strings.
- Timestamps: UTC ISO-8601.
- Month values: `YYYY-MM` and must not be timezone-shifted.
- Collection defaults: `page=1`, `pageSize=20`, maximum `pageSize=100`.
- Frontend error handling uses stable `error.code`; never parse human-readable messages.
- Keep SRS JSON examples, Swagger/OpenAPI, controller DTOs, and frontend TypeScript models synchronized in the same change.
- Binary inspection evidence uses `multipart/form-data`.
- Report export is UTF-8 CSV.
- UI must cover loading, empty, validation, forbidden, not found, conflict, and unexpected-error states.

## 8. Time, money, and external systems

- Persist timestamps in UTC and return UTC timestamps through APIs.
- Evaluate business calendar-day rules and display timestamps in `Asia/Ho_Chi_Minh` (GMT+7).
- Inject `IClock` for time-dependent application logic; do not scatter `DateTime.Now`.
- Use `decimal` for currency. Do not use floating-point types for money.
- Keep MoMo behind `IPaymentGateway`, AI behind `IAiRecommendationProvider`, initial credential email behind `IEmailService`, and notification delivery behind `INotificationSender`.
- External failures must not fabricate successful domain state. AI failure must not block manual reservation, and notification failure must remain retryable.

## 9. Database rules

The Release 1 model contains the 27 persisted entities enumerated in SRS section 9.2. Do not add a table for concepts intentionally excluded there.

Schema changes must:

1. be traceable to an approved requirement;
2. include an EF Core migration and any required SQL object changes;
3. preserve the ability to create an empty database from migrations plus seed;
4. include rollback/redeploy reasoning;
5. update real SQL Server integration tests for affected constraints, triggers, stored procedures, locking, or transitions.

Do not treat EF Core InMemory tests as evidence for SQL Server behavior. Triggers protect invariants but must not orchestrate handover or return finalization.

## 10. Expected repository layout

Follow the baseline in `TECH_BASELINE.md`:

```text
backend/Frms.Api
backend/Frms.Business
backend/Frms.DataAccess
backend/Frms.Infrastructure
backend/tests/Frms.UnitTests
backend/tests/Frms.ApiTests
backend/tests/Frms.IntegrationTests
backend/tests/Frms.ArchitectureTests
frontend/src
tests/postman
tests/e2e
docs
```

Do not create empty architecture layers or duplicate abstractions merely to match a diagram. Interfaces should enforce a real boundary or test seam.

## 11. Change workflow

Before editing:

1. Identify the feature/requirement ID and relevant SRS sections.
2. Inspect nearby code, migrations, tests, Swagger, and frontend types.
3. Determine whether the change touches capacity, payment, handover, renewal, inspection claim, damage decision, return finalization, policy versioning, or authorization.
4. If it changes business semantics, require an approved SRS revision with its associated change record before implementation.

While editing:

- make the smallest coherent change;
- preserve existing public contracts unless the approved requirement changes them;
- keep API DTO mapping at the Presentation boundary;
- keep Business service contracts on Commands/Results rather than API DTOs or EF entities;
- instantiate external providers only through dependency injection;
- propagate `CancellationToken` Controller -> Service -> Repository/provider for supported asynchronous work;
- avoid N+1 queries and unbounded report loads;
- centralize stable error codes and policy/configuration values;
- keep secrets outside committed configuration;
- do not modify unrelated user work.

Before handoff:

- build affected backend and frontend projects;
- run changed-module unit tests;
- run applicable contract, integration, DBT, concurrency, security, Postman, job/integration, and Playwright tests;
- apply migrations to a clean SQL Server database when schema changed;
- verify Swagger and TypeScript synchronization for API changes;
- run architecture-boundary tests when project references or cross-layer dependencies change;
- state exactly what was verified and what could not be run.

Do not claim a feature is complete because the UI works. A Release 1 blocker requires positive, negative/authorization, database/concurrency where applicable, and relevant E2E coverage.

## 12. Test naming and mandatory gates

Use `<Prefix>-<Domain>-<Sequence>` where applicable:

```text
UT-*   CTL-*  DAL-*  DBT-*  CON-*
SEC-*  API-*  INT-*  JOB-*  E2E-*  REG-*  ACC-*
```

Changes to critical paths must run their corresponding real SQL Server and concurrency suites. The minimum database baseline is `DBT-01` through `DBT-25`; the seven browser journeys are `E2E-F01` through `E2E-F07`.

## 13. Documentation discipline

Update traceability whenever an authoritative workflow changes:

```text
Feature -> Business rule/calculation -> API/job -> Service
        -> Repository/stored procedure -> Test -> Release gate
```

Use `README.md` for orientation and setup, `TECH_BASELINE.md` for locked technical decisions, Swagger for the executable API contract, and ADR/change records only for approved refinements that preserve SRS semantics. Do not duplicate a business rule differently across documents.
