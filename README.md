# FRMS - Self-Storage Facility Rental and Management System

FRMS is a web-based platform for renting and operating self-storage facilities. It covers the customer journey from capacity search and reservation through payment, handover, access, renewal, return, inspection, settlement, and support, together with facility and system administration.

> Status: requirements and technical baseline are defined; application scaffolding has not yet been added to this repository.

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
| Data access | Entity Framework Core + stored procedures |
| Authentication | Custom accounts, JWT Bearer, BCrypt |
| Backend testing | NUnit |
| API testing | Postman |
| Browser E2E | Playwright |

The backend has three logical application layers plus an Infrastructure adapter assembly:

```text
HTTP:      React -> Controller -> Business Service -> Repository -> SQL Server
Scheduled: Api BackgroundJob -> Business Service -> Repository -> SQL Server
External:  Infrastructure Adapter -> Business-owned provider interface
```

Critical multi-table lifecycle changes are transactional database operations. Controllers and services never access `DbContext` directly.

## Repository documents

| File | Purpose |
|---|---|
| `project_sources/01-Topic.pdf` | Original one-page topic and actor summary |
| `project_sources/02-FRMS_SRS_V10.md` | FRMS SRS V10 FINAL — governing Release 1 implementation baseline |
| `project_sources/03-FRMS_Data_Dictionary_V2_1.md` | Database entities, lifecycle, stored procedures, triggers, jobs and constraints |
| `AGENTS.md` | Mandatory repository rules for coding agents and contributors |
| `TECH_BASELINE.md` | Consolidated architecture, stack, contracts, security, data, and quality baseline |

The SRS has highest authority. The Topic PDF is useful for orientation but must not override detailed SRS behavior.

## Planned repository structure

```text
.
├── backend/
│   ├── Frms.Api/
│   ├── Frms.Business/
│   ├── Frms.DataAccess/
│   ├── Frms.Infrastructure/
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
└── TECH_BASELINE.md
```

This is a target structure, not a claim that the projects already exist.

## Development phases

### Phase 0 - Technical foundation

- Backend and frontend structure.
- Three logical layer boundaries plus the Infrastructure adapter assembly.
- API DTO mapping and Business Command/Result contracts.
- Dependency-injection composition root and architecture-boundary tests.
- EF Core migration/seed baseline.
- Customer phone login and employee email login.
- JWT, BCrypt, global exception middleware, logging, Swagger.
- UTC persistence and GMT+7 business/display handling.
- NUnit, Postman, and Playwright test skeletons.

### Phase 1 - Core demo

Demonstrate all seven flows with the five roles, MoMo Sandbox deposit/rental payment, real transaction behavior, concurrency protection, authorization isolation, reproducible database setup, and seven Playwright journeys.

### Phase 2 - Release 1

Complete the approved feature catalogue, negative/empty/error states, jobs, full RBAC and ownership checks, API/Swagger synchronization, audit/logging, migration and seed validation, regression testing, traceability, and release evidence.

## Local setup

Executable projects and pinned tool versions are not present yet, so there is no honest one-command setup at this stage. Before application development starts, the team must record and pin:

- .NET SDK/ASP.NET Core version;
- Node.js and package-manager version;
- React, TypeScript, and Playwright versions;
- SQL Server version/edition;
- frontend build tool;
- environment-specific configuration and secret-management approach.

Once scaffolding is committed, this section should contain exact restore, configuration, migration, seed, run, and test commands verified from a clean machine.

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
- Enums: strings.
- API timestamps: UTC ISO-8601.
- Default pagination: `page=1&pageSize=20`; maximum `pageSize=100`.
- Frontend branches on stable error codes, not message text.
- Swagger, controller DTOs, SRS schemas, and TypeScript models change together.
- Inspection evidence uses multipart upload; report export is UTF-8 CSV.

## Testing and quality

The project uses ten test groups: unit, controller/DTO contract, repository/EF integration, database lifecycle, concurrency/idempotency, security, API/Postman, external integration/jobs, Playwright E2E, and regression/acceptance.

SQL Server-specific claims require real SQL Server tests. EF Core InMemory cannot prove stored procedures, filtered indexes, triggers, locking, isolation, or race handling.

A feature is not complete until it has the applicable positive, negative/authorization, database/concurrency, and E2E evidence. Release 1 includes the `DBT-01..25` minimum database baseline and `E2E-F01..F07` journeys.

## Contribution rules

Read `AGENTS.md` before changing code. In particular:

- identify the SRS requirement and traceability path first;
- do not invent business entities, states, permissions, formulas, or workflows;
- preserve Controller -> Service -> Repository boundaries;
- map API DTOs to Business Commands/Results at the Presentation boundary;
- keep external-provider implementations in Infrastructure behind Business interfaces;
- keep API-hosted background jobs on Business services rather than Repository/DbContext;
- keep authentication, role, facility, ownership, and state checks server-side;
- add migration/seed/test updates with model changes;
- update Swagger and frontend types with API changes;
- report exactly which verification commands ran.

Semantic changes require an approved SRS revision with its associated change record. An ADR, ticket, or implementation alone is not an approved business decision.

## Current source caveat

The sole governing SRS is **FRMS SRS V10 FINAL** at `project_sources/02-FRMS_SRS_V10.md`. Earlier SRS versions are obsolete and must not be used as implementation authority. V10 locks the backend project structure, type boundaries, external adapters, API-hosted background jobs and architecture tests.
