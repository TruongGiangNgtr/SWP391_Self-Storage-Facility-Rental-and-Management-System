# Phase 0 merge validation

Date: 2026-10-05. Branch: `setup/project-scaffold`. Status: **In Progress — ExtraFeeType seed gate blocked**.

## Scope and merge

The checkout already had the four backend assemblies, root `Frms.slnx`, React/TypeScript technical shell, authentication services, EF entities/initial migration, architecture tests, and technical test harnesses. This change integrates `origin/main` (`7832e1b`) and preserves its contract-only controller/DTO catalogue while retaining implemented Phase 0 authentication. Duplicate authentication/error types and the duplicate `backend/Frms.sln` were removed during conflict resolution; root `Frms.slnx` is the single solution.

Added EF Design tooling to the API, a corrective schema migration, real SQL Server authentication/constraint tests, four fail-not-configured provider registrations, repository SQL-translation fix, and operational documentation. Generated/user-local files are untracked but preserved locally. Class1 placeholders are removed and recoverable from Git. Formatting follows the inherited editor configuration with generated-migration exceptions; initial migration contents are unchanged.

No Phase 1 business implementation, production account/secret seed, extra persisted table/state, real external-provider integration, or business Playwright journey was added. No push or change to local/remote main is part of this work.

## Actual validation

All rows below completed successfully after fixing discovered failures. The local pnpm executable was supplied using temporary Corepack shims with the repository-pinned pnpm 11.19.0.

| Command | Actual result |
|---|---|
| `dotnet tool restore` | PASS — dotnet-ef 10.0.12 |
| `dotnet restore Frms.slnx --disable-parallel` | PASS |
| `dotnet build Frms.slnx --no-restore --configuration Release` | PASS — 0 warnings, 0 errors |
| `dotnet test Frms.slnx --no-restore --configuration Release` | PASS — Passed: 53, Failed: 0, Skipped: 0; Unit 10, Architecture 13, API 17, Integration 13 |
| `pnpm install --frozen-lockfile` | PASS — unchanged lockfile |
| `pnpm build:frontend` | PASS — TypeScript + Vite production build |
| `pnpm test:postman` | PASS — 1 HTTP request, 2 assertions, 0 failures, against temporary localhost API |
| `pnpm --dir tests/e2e/playwright run install:browsers` | PASS — Chromium installed/available |
| `pnpm test:e2e` | PASS — `E2E-P0-001 technical React shell loads without a business workflow` |
| `dotnet format Frms.slnx --verify-no-changes --no-restore` | PASS |
| `dotnet tool run dotnet-ef migrations script --project backend/Frms.DataAccess --startup-project backend/Frms.Api --no-build --configuration Release --idempotent` | PASS — 1,296 script lines; both migration IDs and corrected constraints present |
| `dotnet tool run dotnet-ef database update --project backend/Frms.DataAccess --startup-project backend/Frms.Api --no-build --configuration Release` | PASS — initial + corrective migration on empty local `Frms_Phase0_MergeVerification` database |
| Real SQL subset (`--filter 'FullyQualifiedName~RealSqlServer'`) | PASS — Passed: 9, Failed: 0, Skipped: 0 |
| Git whitespace/conflict checks | PASS — no unresolved conflict entries or whitespace errors |

Full tests run with `FRMS_TEST_CONNECTION_STRING` targeting isolated local `Frms_Test_Phase0_MergeGate` using Windows integrated authentication. Connection encryption is disabled for this local-only test instance. Tests enforce the `Frms_Test_*` database prefix. Test fixture accounts remain in that disposable database; schema fixture transactions roll back. The temporary API was stopped after Postman. No shared/production database was changed.

An actual SQL query on the empty-database verification instance confirms **27 domain tables, 5 roles, 1 Policy, 6 DamageTypes, 0 ExtraFeeTypes** and both migration IDs. The zero ExtraFeeType count is the documented blocked seed gate, not a passing complete-seed assertion.

## Migration changes

`20261004145122_InitialPhaseZero` is preserved because it already existed on the shared branch. New `20261004191200_PhaseZeroBaselineConstraints` and its designer/snapshot add deterministic status defaults, including Facility INACTIVE and StorageUnit AVAILABLE; Policy.CreatedAt defaults to SYSUTCDATETIME; Policy visit days require start <= end; NotificationLog requires PENDING/null SentAt or SENT/non-null SentAt. No financial values are seeded. See [foundation record](PHASE0_FOUNDATION.md) for existing-data preflight and rollback/redeploy reasoning.

Real API testing exposed an EF translation failure when filtering properties after positional record construction. The repository now filters UserAccount before joining/projecting AuthenticationAccount; it retains server-side bounded lookup and the existing contract.

## Requirement coverage and Phase 0 exit gate

| SRS §6.1.1 requirement | Status | Evidence |
|---|---|---|
| Backend builds | PASS | Required Release build |
| Frontend builds | PASS | Required frontend build |
| API DTOs excluded from Business contracts | PASS | `BusinessServiceContracts_DoNotExposeApiDtosOrEfEntities` |
| Commands/Results exclude EF entities | PASS | `BusinessServiceContracts_DoNotExposeApiDtosOrEfEntities` |
| Controller → service → repository boundaries | PASS | `Controllers_DependOnBusinessServicesNotRepositoryOrDbContext`, architecture suite |
| External providers resolved through Business interfaces | PASS | `ExternalProviderBoundaries_ResolveAndFailExplicitlyWhenNotConfigured` resolves all four adapters and asserts stable code + 503 for every call |
| Background workers exclude direct data dependencies | PASS | `ApiBackgroundJobs_DoNotDependOnDataAccessOrDbContext`; hosting boundary only, no Phase 1 jobs |
| Empty database migration | PASS | Both migrations applied to empty SQL Server database |
| Required seed loads | BLOCKED | Five roles, Policy v1, six DamageTypes present; mandatory five ExtraFeeType rows await approved amounts/currency |
| Customer phone login | PASS | `INT_AUTH_001_CustomerAndEmployeeLogin_UseRealSqlServerBcryptAndJwtAndRejectInactiveAccount` |
| Employee email login with test account | PASS | Same INT_AUTH_001; asserts staff role and actual current-account facility scope |
| JWT authorization | PASS | Same INT_AUTH_001 accepts both issued JWTs on /auth/me; deactivation rejects earlier token with 401 UNAUTHORIZED |
| BCrypt | PASS | Same INT_AUTH_001 uses real factor-12 hash/verification; `BCryptPasswordHasher_ConfiguredWorkFactor_HashesAndVerifiesWithoutPersistingPlaintext` checks wrong password rejection |
| Global Exception Middleware | PASS | `GlobalExceptionHandler_BusinessException_WritesStableSafeErrorEnvelope` plus real inactive-login 403/code/trace checks |
| ILogger logging | PASS | Real host/logger DI and observed exception diagnostics during SQL failure investigation; fixture suppresses output and no sensitive-data logging is enabled |
| Swagger/OpenAPI | PASS | `OpenApiDocumentContainsAuthAndPreservedContractScaffoldEndpoints` + Newman |
| API JSON conventions | PASS | API mapping/error tests and actual camelCase login/current-account/error deserialization |
| UTC/GMT+7 | PASS | `ToBusinessTime_UtcInstant_ConvertsToAsiaHoChiMinhWithoutChangingInstant`; `DBT_PHASE0_001_DatabaseDefaults_ApplyFacilityUnitNotificationStatusAndUtcCreatedAt` |
| NUnit projects run | PASS | 53 tests, zero skipped |
| Postman environment runs | PASS | Actual HTTP 200 and auth-path assertions |
| Playwright project runs | PASS | Technical shell smoke test only |

Additional SQL assertions:
- `DBT_PHASE0_002_PolicyVisitDays_RejectReversedRangeAndAcceptEqualBoundary`: accepts 1..31 and 10..10; rejects 20..19 with SQL error 547 naming CK_Policy_Days.
- `DBT_PHASE0_003_NotificationSentAt_MatchesDeliveryStatus`: all four PENDING/SENT × null/non-null cases; rejects violations with SQL error 547 naming CK_NotificationLog_SentAt.
- INT_AUTH_001 rejects inactive customer login with ACCOUNT_INACTIVE/403 and a non-empty trace ID.

Release 1 DBT-01..25, concurrency and E2E-F01..F07 remain later-phase work and are not claimed by these Phase 0 tests.

## Exact remaining owner input

Phase 0 owner **Nguyễn Trần Trường Giang** must supply approved non-negative decimal DefaultAmount values for KEY_REPLACEMENT, ACCESS_CARD_REPLACEMENT, LOCK_REPLACEMENT, CLEANING_FEE, OTHER, plus the currency basis. Architecture review owner: **Bùi Đình Long**. No zero or guessed amount is inserted. After approval, add a forward seed migration and verify all five rows on SQL Server. Only then may the seed gate and Phase 0 status become Completed.

## Exact changed-file manifest

Relative to branch HEAD before integrating main (`b3f8c12`), including files brought in from main. A = added; M = modified; D = removed from Git (the three generated/user-local copies remain on disk). Line-ending-only working-copy normalization produces no substantive Git change and is excluded.

```text
A .editorconfig
A backend/Frms.Api/Authorization/RoleNames.cs
A backend/Frms.Api/BackgroundJobs/README.md
A backend/Frms.Api/Controllers/AdminController.cs
A backend/Frms.Api/Controllers/AiController.cs
A backend/Frms.Api/Controllers/BillingController.cs
A backend/Frms.Api/Controllers/BusinessController.cs
A backend/Frms.Api/Controllers/CatalogController.cs
A backend/Frms.Api/Controllers/ContractsController.cs
A backend/Frms.Api/Controllers/InspectionsController.cs
A backend/Frms.Api/Controllers/ReportsController.cs
A backend/Frms.Api/Controllers/ReservationsController.cs
A backend/Frms.Api/Controllers/ScaffoldControllerBase.cs
A backend/Frms.Api/Controllers/StaffOperationsController.cs
A backend/Frms.Api/Controllers/StorageUnitsController.cs
A backend/Frms.Api/Controllers/SupportTicketsController.cs
A backend/Frms.Api/Controllers/VisitsController.cs
A backend/Frms.Api/DTOs/Requests/AdministratorRequests.cs
A backend/Frms.Api/DTOs/Requests/BusinessRequests.cs
A backend/Frms.Api/DTOs/Requests/CatalogRequests.cs
A backend/Frms.Api/DTOs/Requests/ContractRequests.cs
A backend/Frms.Api/DTOs/Requests/InspectionRequests.cs
A backend/Frms.Api/DTOs/Requests/PaymentRequests.cs
A backend/Frms.Api/DTOs/Requests/RegisterCustomerRequest.cs
A backend/Frms.Api/DTOs/Requests/ReservationRequests.cs
A backend/Frms.Api/DTOs/Requests/StorageUnitRequests.cs
A backend/Frms.Api/DTOs/Requests/SupportTicketRequests.cs
A backend/Frms.Api/DTOs/Requests/VisitRequests.cs
A backend/Frms.Api/DTOs/Responses/AuthenticationResponses.cs
A backend/Frms.Api/DTOs/Responses/BillingResponses.cs
A backend/Frms.Api/DTOs/Responses/CatalogResponses.cs
A backend/Frms.Api/DTOs/Responses/CommonResponses.cs
A backend/Frms.Api/DTOs/Responses/ContractResponses.cs
A backend/Frms.Api/DTOs/Responses/InspectionResponses.cs
A backend/Frms.Api/DTOs/Responses/ReportingResponses.cs
A backend/Frms.Api/DTOs/Responses/ReservationResponses.cs
A backend/Frms.Api/DTOs/Responses/SupportTicketResponses.cs
A backend/Frms.Api/DTOs/Responses/VisitResponses.cs
A backend/Frms.Api/Mapping/README.md
A backend/Frms.Api/Validation/README.md
A backend/Frms.Business/Abstractions/External/.gitkeep
A backend/Frms.Business/Abstractions/External/ExternalProviderAbstractions.cs
A backend/Frms.Business/Abstractions/Security/.gitkeep
A backend/Frms.Business/Abstractions/Time/.gitkeep
A backend/Frms.Business/Calculations/.gitkeep
A backend/Frms.Business/Events/.gitkeep
A backend/Frms.Business/Exceptions/.gitkeep
A backend/Frms.Business/Mapping/.gitkeep
A backend/Frms.Business/Models/Commands/.gitkeep
A backend/Frms.Business/Models/Common/.gitkeep
A backend/Frms.Business/Models/Results/.gitkeep
A backend/Frms.Business/README.md
A backend/Frms.Business/Rules/.gitkeep
A backend/Frms.Business/Services/Implementations/.gitkeep
A backend/Frms.Business/Services/Interfaces/.gitkeep
A backend/Frms.Business/Validators/.gitkeep
A backend/Frms.DataAccess/Migrations/.gitkeep
A backend/Frms.DataAccess/Migrations/20261004191200_PhaseZeroBaselineConstraints.Designer.cs
A backend/Frms.DataAccess/Migrations/20261004191200_PhaseZeroBaselineConstraints.cs
A backend/Frms.DataAccess/Persistence/Configurations/.gitkeep
A backend/Frms.DataAccess/Persistence/Entities/.gitkeep
A backend/Frms.DataAccess/README.md
A backend/Frms.DataAccess/Repositories/Implementations/.gitkeep
A backend/Frms.DataAccess/Repositories/Interfaces/.gitkeep
A backend/Frms.DataAccess/StoredProcedures/Commands/.gitkeep
A backend/Frms.DataAccess/StoredProcedures/Models/.gitkeep
A backend/Frms.DataAccess/StoredProcedures/Sql/.gitkeep
A backend/Frms.Infrastructure/Ai/.gitkeep
A backend/Frms.Infrastructure/Email/.gitkeep
A backend/Frms.Infrastructure/External/UnconfiguredProviderAdapters.cs
A backend/Frms.Infrastructure/Notifications/.gitkeep
A backend/Frms.Infrastructure/Payments/.gitkeep
A backend/Frms.Infrastructure/README.md
A backend/tests/Frms.ApiTests/ApplicationFactoryTests.cs
A backend/tests/Frms.ApiTests/AuthenticationWiringTests.cs
A backend/tests/Frms.ApiTests/ExternalProviderRegistrationTests.cs
A backend/tests/Frms.ApiTests/FrmsWebApplicationFactory.cs
A backend/tests/Frms.ApiTests/HealthEndpointTests.cs
A backend/tests/Frms.ApiTests/OpenApiTests.cs
A backend/tests/Frms.ApiTests/README.md
A backend/tests/Frms.ApiTests/RoleNamesTests.cs
A backend/tests/Frms.ApiTests/ScaffoldEndpointTests.cs
A backend/tests/Frms.ArchitectureTests/PresentationBoundaryTests.cs
A backend/tests/Frms.ArchitectureTests/ProjectDependencyTests.cs
A backend/tests/Frms.ArchitectureTests/README.md
A backend/tests/Frms.ArchitectureTests/SolutionBoundaryTests.cs
A backend/tests/Frms.IntegrationTests/DataAccessRegistrationTests.cs
A backend/tests/Frms.IntegrationTests/FrmsWebApplicationFactory.cs
A backend/tests/Frms.IntegrationTests/README.md
A backend/tests/Frms.IntegrationTests/RealSqlServerAuthenticationTests.cs
A backend/tests/Frms.IntegrationTests/RealSqlServerBaselineTests.cs
A backend/tests/Frms.UnitTests/BusinessRegistrationTests.cs
A backend/tests/Frms.UnitTests/README.md
A docs/PHASE0_MERGE_VALIDATION.md
A tests/postman/README.md
D backend/Frms.Api/Frms.Api.csproj.user
D backend/Frms.Business/Class1.cs
D backend/Frms.DataAccess/Class1.cs
D backend/Frms.Infrastructure/Class1.cs
D frontend/tsconfig.app.tsbuildinfo
D frontend/tsconfig.node.tsbuildinfo
M .gitignore
M README.md
M backend/Frms.Api/Controllers/AuthController.cs
M backend/Frms.Api/Frms.Api.csproj
M backend/Frms.Api/Program.cs
M backend/Frms.DataAccess/Migrations/FrmsDbContextModelSnapshot.cs
M backend/Frms.DataAccess/Persistence/Configurations/FrmsModelConfiguration.cs
M backend/Frms.DataAccess/Persistence/FrmsDbContextFactory.cs
M backend/Frms.DataAccess/Repositories/Implementations/UserAccountRepository.cs
M backend/Frms.Infrastructure/DependencyInjection/InfrastructureServiceCollectionExtensions.cs
M backend/README.md
M backend/tests/Frms.ApiTests/OpenApiFoundationTests.cs
M backend/tests/Frms.IntegrationTests/Frms.IntegrationTests.csproj
M backend/tests/Frms.IntegrationTests/FrmsDbContextModelTests.cs
M docs/PHASE0_FOUNDATION.md
```
