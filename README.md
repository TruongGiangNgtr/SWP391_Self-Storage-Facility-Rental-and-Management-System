# FRMS - Self-Storage Facility Rental and Management System

FRMS is a web-based platform for renting and operating self-storage facilities. It covers the customer journey from capacity search and reservation through payment, handover, access, renewal, return, inspection, settlement, and support, together with facility and system administration.

## Current Development Status

The ASP.NET Core backend scaffold now exists under `backend/`. It provides the solution structure, technical hosting foundation, configuration, dependency wiring, contract-only API actions, and technical tests needed for module owners to continue development.

The scaffold is not a completed backend: business services, repositories, persistence entities, database objects, external-provider adapters, and frontend implementation remain outside the current implementation. See [`backend/README.md`](backend/README.md) for backend-specific setup, contracts, verification results, and limitations.

## Product scope

### Roles

| Role | Primary responsibility |
|---|---|
| Storage Customer | Browse capacity, reserve, pay, receive and manage units, renew/return, request support |
| Facility Staff | Check in visits, support handover/access/return, inspect units, record issues and fees |
| Facility Manager | Manage physical units, select units at handover, monitor facility operation, decide damage, assign support |
| Business Operations Manager | Manage facilities, pricing, policy versions, discounts, fees, and system-wide reporting |
| System Administrator | Manage accounts, roles, facility assignments, login history, and audit visibility |

The code-level role names are:

```text
CUSTOMER
FACILITY_STAFF
FACILITY_MANAGER
BUSINESS_OPERATIONS_MANAGER
SYSTEM_ADMINISTRATOR
```

### Seven core flows

1. Storage Unit Reservation.
2. Storage Check-in and Handover.
3. Rented Storage Unit Management.
4. Business Rules, Fee Management, and Revenue Monitoring.
5. Facility Storage and Staff Management.
6. Storage Renewal and Overdue Handling.
7. Support Request and Issue Handling.

A reservation holds capacity for a Facility, Unit Type, and month range. It does **not** reserve a physical unit; a Facility Manager selects the concrete `StorageUnit` during handover.

## Technology baseline

| Component | Technology |
|---|---|
| Frontend | React + TypeScript |
| Backend | ASP.NET Core Web API + C# |
| Database | SQL Server |
| Data access | Entity Framework Core + authoritative stored procedures |
| Authentication | Custom accounts, JWT Bearer, BCrypt |
| Backend testing | NUnit |
| API testing | Postman |
| Browser E2E | Playwright |

## High-level architecture

FRMS has three logical backend layers plus an Infrastructure adapter assembly:

```text
HTTP:      React -> Controller -> Business Service -> Repository -> EF/SP -> SQL Server
Scheduled: Api BackgroundJob -> Business Service -> Repository -> EF/SP -> SQL Server
External:  Infrastructure Adapter -> Business-owned provider interface
```

Responsibilities:

- `Frms.Api`: HTTP, DTOs, authentication/authorization wiring, middleware, status mapping, OpenAPI, and API-hosted background jobs.
- `Frms.Business`: orchestration, Commands/Results, rules, calculations, provider interfaces, and time abstractions.
- `Frms.DataAccess`: repositories, EF Core, `FrmsDbContext`, mappings, migrations, and stored-procedure calls.
- `Frms.Infrastructure`: external-provider adapters behind Business-owned interfaces.
- Frontend: role-specific presentation and typed API consumption; it is not authoritative for lifecycle, money, capacity, or authorization decisions.

Critical multi-table lifecycle changes use the authoritative stored procedure and SQL Server transaction identified by the SRS. Controllers and Business Services must not access `DbContext` directly.

## Source-of-truth documents

Use the following authority order when requirements conflict:

1. `project_sources/02-FRMS_SRS_V10.md`
2. `project_sources/03-FRMS_Data_Dictionary_V2_1.md`
3. FRMS Scope V8, when added to the repository
4. Approved technical decisions that preserve SRS semantics
5. Existing implementation details

| File | Purpose |
|---|---|
| `project_sources/01-Topic.pdf` | Original topic and actor summary; it does not replace the SRS |
| `project_sources/02-FRMS_SRS_V10.md` | FRMS SRS V10 FINAL — governing Release 1 implementation baseline |
| `project_sources/03-FRMS_Data_Dictionary_V2_1.md` | Database entities, lifecycle, stored procedures, triggers, jobs, and constraints |
| `AGENTS.md` | Mandatory repository rules for coding agents and contributors |
| `TECH_BASELINE.md` | Consolidated architecture, stack, contracts, security, data, and quality baseline |

Earlier SRS versions are historical only. An ADR or change record does not override SRS semantics unless the change is incorporated into an approved SRS revision.

## Repository structure

```text
.
├── backend/
│   ├── Frms.sln
│   ├── Frms.Api/
│   ├── Frms.Business/
│   ├── Frms.DataAccess/
│   ├── Frms.Infrastructure/
│   ├── README.md
│   └── tests/
│       ├── Frms.UnitTests/
│       ├── Frms.ApiTests/
│       ├── Frms.IntegrationTests/
│       └── Frms.ArchitectureTests/
├── frontend/
│   └── src/
├── tests/
│   ├── postman/
│   └── e2e/
├── docs/
├── project_sources/
├── AGENTS.md
├── README.md
└── TECH_BASELINE.md
```

The backend structure exists. Frontend and browser E2E implementation remain future project work and are shown to preserve the approved repository-level layout.

## Development phases

### Phase 0 - Technical foundation

- Backend and frontend structure.
- Three logical layer boundaries plus the Infrastructure adapter assembly.
- API DTO mapping and Business Command/Result contracts.
- Dependency-injection composition root and architecture-boundary tests.
- EF Core migration/seed baseline when the persistence model is approved.
- Customer phone login and employee email login.
- JWT, BCrypt, global exception middleware, logging, and OpenAPI.
- UTC persistence and GMT+7 business/display handling.
- NUnit, Postman, and Playwright test foundations.

### Phase 1 - Core demo

Demonstrate all seven flows with the five roles, MoMo Sandbox deposit/rental payment, real transaction behavior, concurrency protection, authorization isolation, reproducible database setup, and seven Playwright journeys.

### Phase 2 - Release 1

Complete the approved feature catalogue, negative/empty/error states, jobs, full RBAC and ownership checks, API/OpenAPI synchronization, audit/logging, migration and seed validation, regression testing, traceability, and release evidence.

## Local setup

Backend restore, build, run, test, configuration, health-check, and OpenAPI instructions are maintained in [`backend/README.md`](backend/README.md).

Frontend setup will be documented when its implementation and pinned toolchain are added. Environment-specific secrets must remain outside committed configuration.

## Configuration principles

Maintain separate `Development`, `Testing`, and `Production` configuration. Never commit database passwords, JWT signing keys, MoMo credentials, AI keys, email/notification credentials, access tokens, or plaintext passwords.

Time and money conventions:

- persist and exchange timestamps in UTC;
- evaluate business calendar rules and display timestamps in `Asia/Ho_Chi_Minh` (GMT+7);
- keep month-only values as `YYYY-MM`;
- use `decimal` for currency;
- use an injected `IClock` for time-dependent business logic.

## API conventions

- Base path: `/api/v1`.
- JSON fields: `camelCase`.
- Enum values: strings.
- API timestamps: UTC ISO-8601.
- Default pagination: `page=1&pageSize=20`; maximum `pageSize=100`.
- Frontend branches on stable `error.code`, not message text.
- OpenAPI, controller DTOs, SRS schemas, and frontend TypeScript models change together.
- Inspection evidence uses `multipart/form-data`; report export is UTF-8 CSV.

## Testing and quality

The project uses unit, controller/DTO contract, repository/EF integration, database lifecycle, concurrency/idempotency, security, API/Postman, external integration/jobs, Playwright E2E, and regression/acceptance test groups.

SQL Server-specific claims require real SQL Server tests. EF Core InMemory cannot prove stored procedures, filtered indexes, triggers, locking, isolation, or race behavior.

A feature requires its applicable positive, negative/authorization, database/concurrency, and E2E evidence. Release 1 includes the `DBT-01..25` database baseline and `E2E-F01..F07` browser journeys.

## Contribution rules

Read `AGENTS.md` before changing code. In particular:

- identify the SRS requirement and traceability path first;
- do not invent business entities, states, permissions, formulas, or workflows;
- preserve Controller -> Service -> Repository boundaries;
- map API DTOs to Business Commands/Results at the Presentation boundary;
- keep external-provider implementations in Infrastructure behind Business interfaces;
- keep API-hosted background jobs on Business Services rather than Repository/DbContext;
- keep authentication, role, facility, ownership, and state checks server-side;
- add migration/seed/test updates with approved model changes;
- update OpenAPI and frontend types with API changes;
- report exactly which verification commands ran.

Semantic changes require an approved SRS revision with its associated change record. An ADR, ticket, or implementation alone is not an approved business decision.
