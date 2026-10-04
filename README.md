# FRMS backend scaffold

FRMS (Self-Storage Facility Rental and Management System) hiện ở trạng thái **backend scaffold**. Repository này cung cấp technical foundation và contract foundation để chia task; chưa chứa business logic, persistence model hay external-provider implementation.

## Authority và phạm vi

Thứ tự authority được áp dụng:

1. FRMS SRS V10 FINAL.
2. FRMS Data Dictionary V2.1.
3. Topic/Scope.
4. Technical implementation detail trong `AGENTS.md` và `TECH_BASELINE.md`, miễn không thay đổi semantics của SRS.

Scaffold được dựng sau khi đọc đầy đủ Topic PDF, SRS V10, Data Dictionary V2.1, `AGENTS.md`, `TECH_BASELINE.md` và README ban đầu. Không dùng SRS V9 làm implementation authority.

## Target framework

- .NET SDK: `10.0.401` được pin bằng `global.json`.
- Target framework: `net10.0`.
- ASP.NET Core/EF Core packages: `10.0.12`, không dùng preview package.
- Nullable reference types và implicit global usings được bật.
- Backend tests dùng NUnit 4.

.NET 10 được chọn vì repository chưa có backend TFM trước đó, SDK LTS này đã được cài sẵn và tương thích với toàn bộ package scaffold.

## Cấu trúc

```text
backend/
├── Frms.sln
├── Frms.Api/
│   ├── Authentication/
│   ├── Authorization/
│   ├── BackgroundJobs/
│   ├── Controllers/
│   ├── DependencyInjection/
│   ├── DTOs/Requests/
│   ├── DTOs/Responses/
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

tests/
└── postman/
```

Không có `Frms.Worker`, frontend hay Playwright scaffold trong thay đổi này. Scheduled execution sau này nằm trong `Frms.Api/BackgroundJobs` và chỉ được gọi Business Service.

## Project references

| Project | FRMS project references |
|---|---|
| `Frms.Api` | `Frms.Business`, `Frms.DataAccess`, `Frms.Infrastructure` |
| `Frms.Business` | `Frms.DataAccess` |
| `Frms.DataAccess` | Không có |
| `Frms.Infrastructure` | `Frms.Business` |
| `Frms.UnitTests` | `Frms.Business` |
| `Frms.ApiTests` | `Frms.Api` |
| `Frms.IntegrationTests` | `Frms.Api`, `Frms.DataAccess` |
| `Frms.ArchitectureTests` | Bốn production assemblies để kiểm tra boundary |

Luồng bắt buộc là `Controller -> Business Service -> Repository -> EF/SP -> SQL Server`. Tham chiếu của `Frms.Api` tới DataAccess và Infrastructure chỉ dành cho composition root trong `Program.cs`; Controller, middleware và BackgroundJob không được gọi Repository, `FrmsDbContext` hoặc stored procedure.

## Technical foundation

`Frms.Api` có:

- controller discovery;
- JSON `camelCase` và enum dạng string;
- built-in OpenAPI document;
- process health check `GET /health`;
- JWT Bearer configuration binding và authentication/authorization middleware;
- năm role-name constants đã khóa;
- global exception middleware log exception cùng trace ID và trả sanitized `500 INTERNAL_SERVER_ERROR`;
- focused DI extensions `AddBusiness`, `AddDataAccess`, `AddInfrastructure`;
- configuration cho Development, Testing và Production;
- HTTPS redirection ngoài Testing environment.

`AddBusiness` và `AddInfrastructure` chưa đăng ký placeholder hoặc fake implementation. `AddDataAccess` chỉ đăng ký `FrmsDbContext` với SQL Server.

`FrmsDbContext` hiện rỗng: không có `DbSet`, entity mapping, business-specific `OnModelCreating`, seed, `EnsureCreated` hoặc migration-on-start. Repository cũng chưa có implementation.

## Restore, build, run và test

Từ repository root:

```powershell
dotnet --info
dotnet restore backend/Frms.sln
dotnet build backend/Frms.sln --no-restore
dotnet test backend/Frms.sln --no-build --no-restore
dotnet format backend/Frms.sln --verify-no-changes --no-restore
dotnet run --project backend/Frms.Api --no-build --no-restore --launch-profile http
```

Khi API chạy ở launch profile mặc định:

- process health: `http://localhost:5029/health`;
- OpenAPI/Swagger contract: `http://localhost:5029/openapi/v1.json`;
- các request kỹ thuật mẫu: `backend/Frms.Api/Frms.Api.http`.

Scaffold dùng built-in ASP.NET Core OpenAPI document và không tự thêm Swagger UI package vì source documents không khóa package/UI đó. Mọi operation nghiệp vụ trong document có description cho biết đây là contract scaffold trả `501 Not Implemented`.

## Configuration và secrets

`appsettings*.json` chỉ chứa placeholder. Không commit database password, JWT signing key hoặc credential của MoMo/email/AI/notification.

Ưu tiên secret store hoặc environment variables. Ví dụ:

```powershell
$env:ConnectionStrings__FrmsDatabase = "<local SQL Server connection string>"
$env:Jwt__SigningKey = "<local development signing key>"
dotnet run --project backend/Frms.Api --launch-profile http
```

Không gọi `EnsureCreated`, không tự tạo database và không tự chạy migration khi ứng dụng start.

## Contract scaffold

Có 88 action signatures, phân theo catalogue SRS:

| Controller | API groups | Actions |
|---|---|---|
| `AuthController` | AUTH-001..005 | register customer, customer login, employee login, current account, customer profile update |
| `CatalogController` | CAT-001..004 | browse/get facility, list/get unit type |
| `AiController` | AI-001 | unit-type recommendation |
| `ReservationsController` | RES-001..005 | create/list/get/confirm/cancel reservation |
| `BillingController` | BIL-001..002, PAY-001..004 | invoice list/detail, start MoMo payments, payment detail, callback |
| `ContractsController` | CON-001..005, VIS-001..002, INS-008 | contract list/detail/renew/billing/return summary, create access/return visit, finalize return |
| `VisitsController` | VIS-003..006, OPS-002/003/005 | visit list/detail/reschedule/cancel, check-in/out, confirm return |
| `StaffOperationsController` | OPS-001/004 | work items, complete handover |
| `InspectionsController` | INS-001..007, INS-009..010 | inspection list/detail/claim, damage/fee/evidence, complete, decision, damage types |
| `SupportTicketsController` | SUP-001..006 | create/list/get/cancel/assign/complete support ticket |
| `StorageUnitsController` | UNIT-001..005 | storage-unit list/create/get/update/status |
| `BusinessController` | BOM-001..014 | facilities, unit types, policies, discounts, extra-fee types |
| `ReportsController` | REP-001..004 | facility operations/revenue, business overview/export |
| `AdminController` | ADM-001..012 | users, customers, employees, assignments, login history, audit logs, credential resend |

Mỗi action chỉ trả `501 Not Implemented`; action không inject service/repository/DbContext, không thay đổi state, không truy cập database và không trả fake `200 OK`. Authorization metadata chỉ được đặt theo role catalogue đã xác định trong SRS.

### DTO đã tạo

Request DTO chỉ được tạo khi field đủ rõ:

- authentication: `RegisterCustomerRequest`, `CustomerLoginRequest`, `EmployeeLoginRequest`;
- catalogue/AI: `RecommendUnitTypeRequest`;
- reservation: `CreateReservationRequest`, `ConfirmReservationRequest`, `CancelReservationRequest`;
- payment: `StartInvoiceMomoPaymentRequest`, `StartFirstMonthMomoPaymentRequest`;
- contract/visit: `RenewContractRequest`, `CreateAccessVisitRequest`, `CreateReturnVisitRequest`, `RescheduleVisitRequest`, `CancelVisitRequest`, `CompleteHandoverRequest`, `ConfirmActualReturnRequest`;
- inspection: `RecordDamageRequest`, `RecordExtraFeeRequest`, `UploadInspectionEvidenceRequest`, `CompleteInspectionRequest`, `DecideDamageRequest`;
- support: `CreateSupportTicketRequest`, `CancelSupportTicketRequest`, `AssignSupportTicketRequest`, `CompleteSupportTicketRequest`;
- storage unit: `CreateStorageUnitRequest`, `UpdateStorageUnitRequest`, `ChangeStorageUnitStatusRequest`;
- business operations: `CreateFacilityRequest`, `UpdateUnitTypePriceRequest`, `CreatePolicyVersionRequest`, `CreateCustomerDiscountRequest`, `UpdateDiscountRequest`, `UpdateExtraFeeTypeRequest`;
- administration: `CreateEmployeeRequest`, `AssignEmployeeRequest`.

Response DTO đã tạo cho common envelopes/errors; auth token; facility/unit-type availability and recommendation; reservation detail/confirmation; invoice/payment initiation and detail; visit/work-item/handover/return; renewal; damage type/deposit settlement/final return; support ticket; facility operations và business overview report.

API Request/Response DTO, Business Command/Result, EF Entity và Stored Procedure Model vẫn là các boundary riêng. Scaffold chưa tạo Command/Result hoặc persistence type vì chưa có service/repository signature đủ chính xác để sử dụng chúng.

### Contract chưa tạo do thiếu canonical schema/signature

Các mục sau dừng ở route/action boundary:

- AUTH-005: field update profile và required/optional/patch semantics chưa đủ duy nhất;
- PAY-004: raw MoMo callback wire contract thuộc provider adapter và chưa được source khóa;
- CON contract detail: schema phần tử `extensions` chưa đủ;
- inspection detail: nested damage/fee/evidence schemas chưa đủ;
- một số list/detail/summary của UNIT, BOM, ADM và CON chưa có canonical response schema đầy đủ;
- BOM-003 và ADM-006: update fields/patch semantics chưa đủ;
- REP-002 và REP-004: filter/export schema chưa đủ;
- một số create/update success response schemas chưa đủ.

Không tạo Service interface, Repository interface hoặc provider method signature để lấp các khoảng trống trên. Khi module owner nhận task, chữ ký phải được xác nhận từ SRS/approved clarification trước khi thêm. Không dùng `object`, `dynamic`, dictionary, generic repository hoặc Unit of Work để né blocker.

## Chưa được triển khai

- business logic, lifecycle, calculation, validator/rule và business mapping;
- service/repository implementation;
- EF Core entity/configuration/migration/seed;
- stored procedure, trigger và scheduled-job implementation;
- login/register/token generation/password hashing;
- Facility/resource-ownership authorization handler;
- MoMo/email/AI/notification adapter, fake hoặc mock production provider;
- database integration/concurrency/business tests;
- frontend, E2E và `Frms.Worker`.

## Quy tắc nhận task module

Một module task chỉ được sửa trong các folder được giao. Controller map DTO sang Business Command/Result; Business Service gọi Repository/provider interface; Repository chịu EF/SP access. Module task không được đưa API DTO vào Business, EF Entity ra khỏi DataAccess, hoặc gọi DbContext từ Controller/Service.

| Module | Owner | Allowed folders | Required interfaces | Dependencies | Not implemented yet | Relevant SRS/API IDs | Relevant database objects | Relevant test groups |
|---|---|---|---|---|---|---|---|---|
| Authentication/Profile | — | Api Authentication/Controllers/DTOs; Business Security/Services; DataAccess Repositories | Business auth/security contracts after exact signatures are approved | JWT wiring, account persistence | login/register/profile/token/password logic | AUTH-001..005 | `UserAccount`, `UserRole`, `Customer`, `Employee`, `LoginHistory` | UT, CTL, DAL, SEC, API |
| Catalogue/AI | — | Api Catalog/Ai controllers/DTOs; Business Services/External; Infrastructure Ai | service contract; `IAiRecommendationProvider` methods pending | Facility/unit-type reads; optional AI adapter | catalogue query and recommendation | CAT-001..004, AI-001 | `Facility`, `UnitType`, facility/unit-type relationship | UT, CTL, DAL, INT, API |
| Reservation | — | Api Reservations/DTOs/Mapping; Business Services/Models/Rules; DataAccess Repositories/SP | reservation service/repository contracts pending | catalogue, capacity, policy | reservation lifecycle/capacity transaction | RES-001..005 | `Reservation` and SRS reservation-capacity objects | UT, CTL, DAL, DBT, CON, SEC, API |
| Billing/Payment | — | Api Billing/DTOs; Business Services/External; DataAccess Repositories/SP; Infrastructure Payments | billing contracts; `IPaymentGateway` methods pending | invoice, contract/reservation, MoMo | billing/payment/idempotency/callback | BIL-001..002, PAY-001..004 | `Invoice`, `Payment`, reservation/contract snapshots | UT, CTL, DAL, DBT, CON, SEC, INT, API |
| Contract/Visit/Handover | — | Api Contracts/Visits/StaffOperations/DTOs; Business Services/Models; DataAccess Repositories/SP | contract/visit service and repository contracts pending | reservation, unit, billing | handover/access/renewal/return lifecycle | CON-001..005, VIS-001..006, OPS-001..005 | `Contract`, `ContractExtension`, `Visit`, `StorageUnit` | UT, CTL, DAL, DBT, CON, SEC, API |
| Inspection/Settlement | — | Api Inspections/DTOs; Business Services/Models/Rules; DataAccess Repositories/SP | inspection/damage/settlement contracts pending | contract return, policy, fee types | claim/damage/fee/finalization lifecycle | INS-001..010 | `Inspection`, `DamageRecord`, `DamageType`, evidence and extra-fee objects | UT, CTL, DAL, DBT, CON, SEC, API |
| Support | — | Api SupportTickets/DTOs; Business Services/Models; DataAccess Repositories | support service/repository contracts pending | customer/staff/facility scope | support lifecycle/assignment | SUP-001..006 | `SupportTicket` and its authoritative history objects | UT, CTL, DAL, DBT, SEC, API |
| Facility Units | — | Api StorageUnits/DTOs; Business Services/Models; DataAccess Repositories | storage-unit service/repository contracts pending | facility/unit type | create/update/status logic | UNIT-001..005 | `StorageUnit`, `Facility`, `UnitType` | UT, CTL, DAL, DBT, SEC, API |
| Business Operations | — | Api Business/DTOs; Business Services/Models/Rules; DataAccess Repositories | facility/policy/discount/fee contracts pending | facility and policy data | operational configuration logic | BOM-001..014 | facility, unit type, policy, discount and extra-fee objects from SRS §9.2 | UT, CTL, DAL, DBT, SEC, API |
| Reporting | — | Api Reports/DTOs; Business Services/Models; DataAccess Repositories/SP | reporting service/query contracts pending | operational read models | report queries/export | REP-001..004 | authoritative reporting sources named by each SRS report | UT, CTL, DAL, DBT, API |
| Administration | — | Api Admin/DTOs; Business Services/Security; DataAccess Repositories; Infrastructure Email | admin contracts; `IEmailService` methods pending | accounts, roles, facility assignments | account admin/audit/email logic | ADM-001..012 | `UserAccount`, `UserRole`, `Employee`, assignments, `LoginHistory`, `AuditLog` | UT, CTL, DAL, DBT, SEC, INT, API |
| Technical foundation | — | Api Middleware/DI/Configuration; project files; technical tests | existing focused DI extensions | ASP.NET Core, EF Core SQL Server | future non-business observability refinements | Technical baseline only | none | API, Architecture, Integration |

## Technical test scope

Current tests verify host startup, `/health`, JWT scheme wiring, sanitized exception behavior, the 88-operation OpenAPI catalogue and scaffold descriptions, universal direct-action `501`, locked roles, empty `FrmsDbContext`, DI registration, exact project references, presentation/data-access boundaries, no BackgroundJob implementation type and absence of `Frms.Worker`.

Test-project README files describe future test ownership. Current tests do not claim that any business flow, SQL Server behavior, authentication flow, provider integration, stored procedure, trigger or database concurrency behavior works.
