# Backend Test Cases — Implementation-Based

> **Basis:** current backend source code in this ZIP, not the SRS. This document distinguishes executable tests from legacy source files that are not currently compiled. Expected results below describe current code behavior, not a promise that unimplemented workflows exist.

## 1. Authentication service unit tests

Source: `tests/Frms.UnitTests/AuthenticationServiceTests.cs`

| ID | Test case | Input / condition | Expected result | Type |
|---|---|---|---|---|
| AUTH-UT-001 | Customer login succeeds | Active `CUSTOMER`, phone has surrounding spaces, valid password | Phone is trimmed; Bearer token returned; success history contains IP, user agent, and injected UTC time | Updated |
| AUTH-UT-002 | Employee login normalizes email | Active `FACILITY_STAFF`, email has surrounding spaces and mixed case | Email is trimmed/lowercased; token returned; successful history recorded | Updated |
| AUTH-UT-003 | Employee role allow-list | Each of `FACILITY_STAFF`, `FACILITY_MANAGER`, `BUSINESS_OPERATIONS_MANAGER`, `SYSTEM_ADMINISTRATOR` | Login succeeds for each listed role | Added/expanded |
| AUTH-UT-004 | Employee login rejects a non-employee role | `UNKNOWN_ROLE` or `CUSTOMER` | `AUTH_INVALID_CREDENTIALS`, HTTP 401 suggestion, failed history recorded | Added/expanded |
| AUTH-UT-005 | Wrong password | Existing active account, invalid password | `AUTH_INVALID_CREDENTIALS`, HTTP 401 suggestion, failed history recorded | Existing, clarified |
| AUTH-UT-006 | Unknown customer phone | Repository returns no account | `AUTH_INVALID_CREDENTIALS`; no login-history row is fabricated | Existing, clarified |
| AUTH-UT-007 | Unknown employee email | Repository returns no account | `AUTH_INVALID_CREDENTIALS`; no login-history row is fabricated | Added |
| AUTH-UT-008 | Customer endpoint resolves an employee account | Active account has employee role | `AUTH_INVALID_CREDENTIALS`; failed history recorded | Added |
| AUTH-UT-009 | Inactive account login | Account exists but status is not `ACTIVE` | `ACCOUNT_INACTIVE`, HTTP 403 suggestion, failed history recorded | Existing, clarified |
| AUTH-UT-010 | Current account projection | Active account with profile IDs and name | Result exposes the account, role/status, email/phone, customer/employee/facility IDs, and full name from the repository model | Added |
| AUTH-UT-011 | Current account is inactive | Existing account status is not `ACTIVE` | `ACCOUNT_INACTIVE`, HTTP 403 suggestion | Added |
| AUTH-UT-012 | Current account is missing | Repository returns no account for the ID | `UNAUTHORIZED`, HTTP 401 suggestion | Added |
| AUTH-UT-013 | Token account activity check | Match/mismatch of account status and exact role | `true` only for an active account whose role exactly matches; missing, inactive, or changed-role accounts return `false` | Added/expanded |

## 2. API authentication primitives and request validation

Sources: `tests/Frms.ApiTests/AuthenticationFoundationTests.cs`, `PasswordValidationTests.cs`, `LoginRequestValidationTests.cs`, `AuthenticationWiringTests.cs`

| ID | Test case | Input / condition | Expected result | Type |
|---|---|---|---|---|
| AUTH-API-001 | BCrypt hashes password | Valid password | Hash differs from plaintext; matching password verifies; wrong password does not | Existing |
| AUTH-API-002 | BCrypt receives malformed stored hash | Non-BCrypt hash string | Verification returns `false`, rather than leaking a parse exception | Added |
| AUTH-API-003 | JWT claims and expiry | Valid issuer/audience/signing key and 30-minute lifetime | Issuer, audience, subject, role and expiry match configured values | Existing |
| AUTH-API-004 | Business exception envelope | `AUTH_INVALID_CREDENTIALS`, status 401 | HTTP status, stable error code, and trace ID are present | Existing |
| AUTH-API-005 | API mapping boundary | Customer login request and authentication result | Request maps phone/IP/user-agent into command; result maps to token response | Existing |
| AUTH-API-006 | Customer login request valid data | Phone length within 30; password length within accepted range | Data annotations produce no validation errors | Added |
| AUTH-API-007 | Customer phone exceeds max length | 31 characters | Validation flags `PhoneNumber` | Added |
| AUTH-API-008 | Login password too short | Fewer than 8 characters | Password validation flags `Password` | Added |
| AUTH-API-009 | Employee email invalid | Malformed email | Validation flags `Email` | Added |
| AUTH-API-010 | Employee email exceeds max length | More than 254 characters | Validation flags `Email` | Added |
| AUTH-API-011 | Password boundary | 7, 8, and 64 characters | 7 rejected; 8 and 64 accepted, subject to byte limit | Existing |
| AUTH-API-012 | Password exceeds BCrypt byte limit | Multibyte password over 72 UTF-8 bytes | Rejected | Existing |
| AUTH-API-013 | JWT bearer is default scheme | Application service provider | Default authenticate scheme is JWT Bearer | Existing |

## 3. DataAccess configuration and EF model tests

Sources: `tests/Frms.IntegrationTests/DataAccessRegistrationTests.cs`, `FrmsDbContextModelTests.cs`

| ID | Test case | Input / condition | Expected result | Type |
|---|---|---|---|---|
| DAL-001 | Configure by `ConnectionStrings:Frms` | Valid PostgreSQL connection string | Npgsql provider selected; configured connection values preserved | Updated (was SQL Server expectation) |
| DAL-002 | Configure by `ConnectionStrings:FrmsDb` | Alias setting only | Npgsql provider selected; connection values preserved | Updated |
| DAL-003 | Environment-variable configuration | `ConnectionStrings__FrmsDb` under a test-specific prefix | Environment provider supplies the connection string | Updated |
| DAL-004 | Existing `Frms` takes precedence | Both `Frms` and `FrmsDb` configured | `Frms` connection is selected | Updated |
| DAL-005 | Reject Npgsql multiplexing | Connection has `Multiplexing=true` | Throws `InvalidOperationException` explaining multiplexing must be disabled for audit | Added |
| DAL-006 | Missing/blank connection | `null`, empty, or whitespace connection | Fails early with configuration guidance; no implicit DB is selected | Updated |
| DAL-007 | Design-time factory configuration | No process connection vs explicit `FrmsDb` alias | Missing configuration fails; explicit PostgreSQL string creates context without opening a connection | Updated |
| DAL-008 | Resolve application DbContext | Resolve `FrmsDbContext` from test host | Npgsql provider and current 27-entity model are available without opening a database connection | Updated |
| DAL-009 | Current entity model | Build model from PostgreSQL options | 27 entities include `UserAccount`, `Customer`, `Employee`, `LoginHistory`, and `Payment` | Rewritten from commented tests |
| DAL-010 | UserAccount unique indexes | Inspect EF model | `Email` and `PhoneNumber` each have a unique index | Added |
| DAL-011 | UserAccount status model | Inspect EF model | Default is `ACTIVE`; `CK_UserAccount_Status` is present; status column is `varchar(50)` | Added |

## 4. Existing API/architecture tests to retain

These existing files remain the source of tests for their named implementation areas:

- `tests/Frms.ApiTests/ApplicationFactoryTests.cs` — test host creation/configuration.
- `tests/Frms.ApiTests/HealthEndpointTests.cs` — `/health` returns `503` when the configured PostgreSQL endpoint is unreachable; connection details must not leak in response body. The test was updated from SQL Server to PostgreSQL.
- `tests/Frms.ApiTests/OpenApiFoundationTests.cs` and `OpenApiTests.cs` — OpenAPI endpoint/document behavior.
- `tests/Frms.ApiTests/RoleNamesTests.cs`, `RbacV2ContractTests.cs`, and `CustomerControllerConsolidationTests.cs` — current role/route metadata. These metadata tests do not prove the underlying business workflow succeeds. `ContractsController.ListContracts` currently declares `CUSTOMER,FACILITY_MANAGER,BUSINESS_OPERATIONS_MANAGER`; `InspectionsController.ListInspections` declares `FACILITY_STAFF,FACILITY_MANAGER`; `SupportTicketsController.ListSupportTickets` declares `CUSTOMER,FACILITY_STAFF,FACILITY_MANAGER`; `SupportTicketsController.AssignSupportTicket` declares `FACILITY_MANAGER`; `ReportsController.GetFacilityOperationsReport` declares `FACILITY_MANAGER`.
- `tests/Frms.ApiTests/ScaffoldEndpointTests.cs` — only actions still returning the scaffold error result are expected to return `501 ENDPOINT_NOT_IMPLEMENTED`.
- `tests/Frms.ApiTests/PaymentEndpointTests.cs` and `ReservationCancellationTests.cs` — API behavior exercised with test doubles; their assertions are not database integration tests.
- `tests/Frms.ArchitectureTests/*.cs` — assembly dependency and project-boundary rules.
- Other unit tests in `tests/Frms.UnitTests/` — retain where assertions match the current implementation; review failures against source rather than the SRS.

## 5. Legacy SQL Server integration fixtures — not active

The following source files still assume SQL Server (`UseSqlServer`, `Microsoft.Data.SqlClient`, `dbo.*`, `sys.tables`, T-SQL scripts or SQL Server error numbers), but current application DataAccess uses PostgreSQL/Npgsql and the migration project is PostgreSQL-specific:

- `tests/Frms.IntegrationTests/PaymentSqlTestSupport.cs`
- `tests/Frms.IntegrationTests/PaymentPersistenceTests.cs`
- `tests/Frms.IntegrationTests/PaymentResultSqlTests.cs`
- `tests/Frms.IntegrationTests/PaymentServiceSqlTests.cs`
- `tests/Frms.IntegrationTests/PayOsPersistenceTests.cs`
- `tests/Frms.IntegrationTests/RealSqlServerAuthenticationTests.cs`
- `tests/Frms.IntegrationTests/RealSqlServerBaselineTests.cs`
- `tests/Frms.IntegrationTests/ReservationCancellationSqlTests.cs` (currently commented-out source)
- `tests/Frms.IntegrationTests/SqlScriptTestDatabase.cs`

These files are excluded from compilation by `Frms.IntegrationTests.csproj` until ported to PostgreSQL. They are retained as reference and are **not counted as passing coverage**. In particular, SQL Server test database setup or the former `FRMS_TEST_CONNECTION_STRING` instructions must not be used to infer the current application's provider.

## 6.1 Current authorization metadata finding

`SupportTicketsController.AssignSupportTicket` currently declares `[Authorize(Roles = "FACILITY_MANAGER")]`. The test `AssignSupportTicketIsRestrictedToFacilityManagers` checks this method-level role gate. This is a metadata/contract test: it confirms the controller declaration, not that the full assignment workflow is implemented or succeeds end-to-end.

## 7. Execution status

The first run reported by the developer (before the latest fixes) was **309 passed / 3 failed / 312 total**. The three failures were:

- `FrmsDbContextModelTests.UserAccount_Status_HasActiveDefaultAndAllowedValuesConstraint` — fixed to inspect check-constraint metadata through EF Core's design-time model.
- `RbacV2ContractTests.ExistingContractListAllowsStaff` — updated to assert the roles actually declared by `ContractsController.ListContracts` (`CUSTOMER,FACILITY_MANAGER,BUSINESS_OPERATIONS_MANAGER`); the method does not declare `FACILITY_STAFF`.
- `RbacV2ContractTests.BOMMonitoringIsReadOnlyAtControllerGate` — updated to match the current role metadata on inspection, support-ticket and report endpoints. The existing assertion about `AssignSupportTicket` expecting a manager role was incorrect because the method currently has no method-level `[Authorize]` attribute; this is called out separately as a security review finding.

The latest developer-reported run after the previous patch was **312 passed / 1 failed / 313 total**. The remaining failure was in `RbacV2ContractTests.AssignSupportTicketCurrentlyHasNoMethodLevelAuthorizeAttribute`, where the test expected no method-level authorization, while the controller actually declares [Authorize(Roles = "FACILITY_MANAGER")]. The test was corrected to assert the role currently declared by the controller. The updated test suite has now been rerun successfully, with 313 tests passed, 0 failed, and 0 skipped in 17.1 seconds.

Run from `backend/`:

```powershell
dotnet test Frms.slnx
```

Update this execution status from the actual command output. Do not mark the final suite as passed until it has been rerun.
