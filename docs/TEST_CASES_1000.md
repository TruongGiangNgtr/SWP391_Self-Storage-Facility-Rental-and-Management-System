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

## 7. Additional edge-case test suites

These five new test files were added under the existing test projects. They exercise the current implementation directly and use test doubles where applicable; they do not imply that unfinished business workflows are implemented.

| Test file | Added test cases | Main edge cases covered |
|---|---:|---|
| `tests/Frms.UnitTests/AuthenticationServiceEdgeCaseTests.cs` | 38 | All supported employee roles; unsupported, misspelled, lowercase, blank and unknown roles; customer login with non-customer roles; inactive account statuses; exact case-sensitive role/status comparisons; missing account during active-token checks; failed login history is recorded. |
| `tests/Frms.UnitTests/CustomerProfileValidatorEdgeCaseTests.cs` | 31 | Null/empty/whitespace names; 150/151 and extremely long names; Unicode Vietnamese and punctuation; address and CCCD length boundaries; trimming; empty optional values normalized to null; no fields selected; validation error precedence; fields not selected for update. |
| `tests/Frms.UnitTests/PolicyVersionValidatorEdgeCaseTests.cs` | 45 | `int.MinValue`, negative, zero, maximum integer; day values below 1 and above 31; reversed date/day ranges; same-day ranges; valid minimum and maximum boundaries; deterministic first-invalid-field precedence. |
| `tests/Frms.UnitTests/PayOsOptionsEdgeCaseTests.cs` | 38 | Blank/relative/malformed URLs; unsupported schemes; insecure HTTP where HTTPS is required; credentials embedded in URLs; fragments; lookalike API origins; CR/LF in credentials; zero/negative/positive/extreme expiry values; return/cancel route mismatch; null options. |
| `tests/Frms.ApiTests/PasswordValidationEdgeCaseTests.cs` | 32 | Password character lengths 0–1000; boundaries at 7/8 and 64/65; UTF-8 byte boundaries at 72/over 72 bytes; Vietnamese characters, emoji and mixed strings; null/non-string values; validation error message. |
| **First additional batch total** | **184** | **Unit tests: 152 added; API tests: 32 added.** |

### 7.1. Second batch — bulk boundary and configuration cases

The second batch adds 503 NUnit test cases, primarily through parameterized test sources that vary boundary values while asserting the existing implementation's behavior.

| Test file | Added test cases | Main edge cases covered |
|---|---:|---|
| `tests/Frms.ApiTests/PasswordValidationBulkEdgeTests.cs` | 200 | ASCII password lengths 0–70; repeated Vietnamese multibyte characters; emoji; mixed ASCII/Vietnamese strings; checks character-length and UTF-8 byte-length constraints. |
| `tests/Frms.UnitTests/PolicyVersionValidatorBulkEdgeTests.cs` | 203 | Negative and zero duration values; invalid day values below 1 and above 31 across policy fields; inverted but individually valid day ranges; expected first invalid field. |
| `tests/Frms.UnitTests/PayOsOptionsBulkEdgeTests.cs` | 100 | Blank/control-character credentials; malformed or untrusted API origins; invalid return/cancel/webhook URLs; URL schemes, credentials and fragments; negative and zero expiry values. |
| **Second batch total** | **503** | **Unit tests: 303 added; API tests: 200 added.** |
| **All additional batches total** | **687** | **Unit tests: 455 added; API tests: 232 added.** |

The bulk cases are generated by NUnit `TestCaseSource`; each yielded `TestCaseData` is discovered as an individual test case. They exercise validation contracts and do not represent 503 distinct business workflows or database integration scenarios.

### Edge-case notes

- Authentication tests intentionally verify exact role/status matching because the current service uses exact string comparisons in these checks.
- Profile tests assert the current `CustomerProfileValidator` normalization/validation behavior; they are not database persistence tests.
- Policy validator tests assert the current validation order as well as accepted/rejected boundaries.
- PayOS tests cover configuration validation only; they do not make a real PayOS API request.
- Password tests distinguish Unicode character count from UTF-8 byte count, which is important for BCrypt's 72-byte input limit.
- These test cases are automated NUnit cases. The number of test cases in the final suite is taken from the actual `dotnet test` run below, not estimated from source-file counts.

## 8. Execution status

**Latest user-reported run: PASS — 1,000 passed / 0 failed / 1,000 total / 0 skipped.**

Reported command:

```powershell
dotnet test Frms.slnx
```

Reported summary:

```text
Test summary: total: 1000, failed: 0, succeeded: 1000, skipped: 0, duration: 22.9s
Build succeeded in 43.8s
```

Project-level discovered / executed counts in that run:

| Test project | Discovered / executed | Result |
|---|---:|---|
| `Frms.UnitTests` | 624 | Passed |
| `Frms.ArchitectureTests` | 13 | Passed |
| `Frms.IntegrationTests` | 14 | Passed |
| `Frms.ApiTests` | 349 | Passed |
| **Total** | **1,000** | **All passed** |

The earlier failures were corrected before the successful 497-test run:

- `FrmsDbContextModelTests.UserAccount_Status_HasActiveDefaultAndAllowedValuesConstraint` — the test now reads check-constraint metadata from EF Core's design-time model.
- `RbacV2ContractTests.ExistingContractListAllowsStaff` and `BOMMonitoringIsReadOnlyAtControllerGate` — assertions now match the role metadata declared by the current controllers.
- `RbacV2ContractTests.AssignSupportTicketCurrentlyHasNoMethodLevelAuthorizeAttribute` — the test now verifies the current `[Authorize(Roles = "FACILITY_MANAGER")]` declaration.

The latest 1,000-test result is reported by the user after applying the second batch. It confirms all discovered tests passed in that reported environment. It does **not** prove full business-flow coverage, and the legacy SQL Server integration source files listed above remain excluded from compilation until ported to PostgreSQL.

Run from `backend/` to reproduce:

```powershell
dotnet test Frms.slnx
```
