# FRMS — Self-Storage Facility Rental and Management System

FRMS là hệ thống đặt thuê và quản lý kho tự lưu trữ: từ tìm cơ sở, đặt chỗ và thanh toán tiền cọc đến bàn giao kho, vận hành hợp đồng, lập hóa đơn, gia hạn, trả kho và hỗ trợ khách hàng.

Repository gồm frontend React, backend ASP.NET Core, database PostgreSQL và các bộ kiểm thử. Code đã vượt giai đoạn nền tảng Phase 0; sự hiện diện của màn hình hoặc endpoint không đồng nghĩa nghiệp vụ đã được nghiệm thu đầy đủ.

## Tài liệu và trạng thái hiện tại

> **2026-10-29 — Notifications-only review, NOT approved for implementation.** [REV-2026-10-29-INAPP-NOTIFICATIONS](docs/FRMS_InApp_Notifications_Review.md). The pending OD-06 proposal separates in-app notices from email delivery. See the review record for scope and approval gates; source is unchanged. Account registration, email verification, Employee provisioning and role/Facility rules are unchanged.

- [AGENTS.md](AGENTS.md): quy tắc làm việc và giới hạn thay đổi repository.
- [SRS V11](docs/FRMS_SRS_V11.md): luồng nghiệp vụ, quy tắc, quyết định của chủ dự án và các điều kiện nghiệm thu.
- [API V11](docs/FRMS_API_V11.md): hợp đồng HTTP/API.
- [Data Dictionary V11](docs/FRMS_Data_Dictionary_V11.md): mô hình dữ liệu và routine database.
- [TECH_BASELINE.md](TECH_BASELINE.md): tài liệu kỹ thuật còn chứa nội dung cũ, cần đối chiếu với code và các quyết định được phê duyệt.

**Lưu ý về tính nhất quán:** AGENTS.md chỉ định SRS V11 FINAL tại `project_sources/`, nhưng bộ V11 trong checkout nằm tại `docs/` và còn ghi `REVIEW CANDIDATE`, với một số quyết định đã được duyệt riêng. AGENTS.md/TECH_BASELINE.md còn mô tả SQL Server, trong khi runtime hiện dùng PostgreSQL/Npgsql. README này ghi nhận hiện trạng, không phê duyệt thay đổi kiến trúc hoặc thay thế yêu cầu nghiệp vụ. Các điểm chưa được duyệt cần chủ dự án chốt trước khi triển khai thay đổi liên quan.

## Vai trò sử dụng

| Vai trò | Phạm vi chính |
|---|---|
| CUSTOMER | Đặt chỗ, thanh toán, theo dõi hợp đồng, lịch đến kho, gia hạn/trả kho và yêu cầu hỗ trợ của mình |
| FACILITY_STAFF | Tiếp nhận lịch đến, bàn giao, xử lý công việc vận hành, kiểm tra và hoàn tất trả kho theo quyền |
| FACILITY_MANAGER | Quản lý vận hành cơ sở, chọn kho bàn giao, nhân viên, quyết định thiệt hại và điều phối hỗ trợ |
| BUSINESS_OPERATIONS_MANAGER | Giá, chính sách, giảm giá, phụ phí và theo dõi hoạt động/doanh thu nhiều cơ sở |
| SYSTEM_ADMINISTRATOR | Quản trị tài khoản và chức năng hệ thống theo quyền được cấp |

Khách chưa đăng nhập có thể xem thông tin công khai; Guest không phải vai trò nghiệp vụ thứ sáu. Quyền theo cơ sở, chủ sở hữu tài nguyên và trạng thái nghiệp vụ phải được kiểm tra ở backend.

## Bảy luồng nghiệp vụ

Bảng sau tóm tắt mục tiêu nghiệp vụ trong SRS; không phải báo cáo tất cả chức năng đã hoàn thành kiểm thử.

| Luồng | Hành trình |
|---|---|
| F01 — Đặt chỗ | Xem cơ sở/loại kho → chọn khoảng tháng → đăng nhập và hoàn thiện thông tin cần thiết → xác nhận đặt chỗ → hóa đơn cọc → payOS → xác nhận và lịch bàn giao |
| F02 — Nhận và bàn giao kho | Reservation đã xác nhận → Staff check-in → Manager chọn kho phù hợp → Staff xác nhận thu đủ tiền thuê tháng đầu trực tiếp → hoàn tất bàn giao và tạo Contract |
| F03 — Sử dụng kho đang thuê | Contract đang hoạt động → Customer đặt lịch ACCESS → Staff check-in/check-out → tiếp tục hợp đồng |
| F04 — Giá, phí và doanh thu | Quản lý giá/chính sách/giảm giá/phụ phí → lập hóa đơn các tháng sau → theo dõi quá hạn, phí trễ và báo cáo |
| F05 — Vận hành cơ sở và nhân sự | Quản lý kho vật lý, nhân viên và phạm vi cơ sở → danh sách công việc từ lịch đến, kiểm tra và hỗ trợ → giám sát hoạt động |
| F06 — Gia hạn, quá hạn và trả kho | Gia hạn liên tục sau kiểm tra sức chứa; hoặc lịch RETURN → kiểm tra → xử lý thiệt hại/phụ phí → quyết toán cọc và hoàn tất trả kho |
| F07 — Hỗ trợ | Customer tạo ticket gắn với hợp đồng → Manager phân công → Staff xử lý và ghi nhận kết quả → hoàn tất/hủy theo vòng đời được phép |

Các nguyên tắc xuyên suốt:

- Đặt chỗ giữ sức chứa theo **cơ sở + loại kho + khoảng tháng**, chưa gán kho vật lý. Kho được chọn trong quá trình bàn giao.
- Tiền cọc đi qua Invoice/Payment; tháng thuê đầu thu trực tiếp tại bàn giao, không tạo Invoice/Payment cho khoản này. Hóa đơn thuê định kỳ bắt đầu sau tháng đầu hợp đồng.
- Giá và chính sách đã chụp tại giao dịch phải được bảo toàn. Giảm giá hợp đồng không áp dụng cho tiền thuê tháng đầu.
- Kiểm tra xong chưa giải phóng kho. Finalize Return bị chặn khi còn DamageRecord `PENDING`; bước hoàn tất mới chuyển kho sang trạng thái được phép.
- Callback thanh toán, giữ sức chứa, bàn giao, gia hạn, lập hóa đơn và hoàn tất trả kho cần bảo đảm giao dịch, concurrency và retry theo SRS.
- AI là tùy chọn; lỗi AI không được chặn thao tác đặt chỗ thủ công.

## Tech stack và kiến trúc hiện có

| Thành phần | Công nghệ trong repository |
|---|---|
| Frontend | React 19, TypeScript 6, Vite 8, React Router 7, CSS, Lucide |
| Backend | C# / ASP.NET Core, .NET 10; SDK theo `global.json` |
| Dữ liệu | PostgreSQL, EF Core, Npgsql; project migration độc lập |
| Xác thực | JWT Bearer, BCrypt, tài khoản/vai trò tùy chỉnh |
| Tích hợp | payOS; SMTP qua các interface Business; interface AI tùy chọn |
| Công việc định kỳ | Hosted services trong API gọi Business services |
| Kiểm thử | NUnit, Postman/Newman, Playwright |
| Workspace frontend | pnpm theo `package.json` và lockfile |

```text
Browser / React
    → /api/v1 / Controller
    → Business Service (Commands / Results)
    → Repository
    → EF Core / PostgreSQL routines
    → PostgreSQL

API BackgroundJob → Business Service → Repository → Database
Infrastructure adapters → interfaces do Business sở hữu
```

Controller xử lý HTTP và mapping DTO. Business điều phối và kiểm tra nghiệp vụ. DataAccess quản lý truy cập dữ liệu. Infrastructure triển khai provider bên ngoài. Frontend không quyết định tiền, sức chứa, quyền truy cập hoặc trạng thái thanh toán.

## Cấu trúc repository

```text
backend/Frms.Api                  HTTP, DTO, auth, OpenAPI, background jobs, DI
backend/Frms.Business             Services, Commands/Results, provider interfaces, IClock
backend/Frms.DataAccess           Entities, repositories, EF Core/Npgsql
backend/Frms.Infrastructure       Payment, email và các adapter
backend/Frms.Migrations.PostgreSql Migration/schema deployment độc lập
backend/sql/archive              Nguồn SQL Server lịch sử
backend/tests                    Unit, API, integration, architecture tests
frontend/src                     Các trang, layout theo vai trò, feature và API client
tests/postman                    Bộ kiểm tra API
tests/e2e/playwright              Kiểm thử trình duyệt
docs                             SRS/API/Data Dictionary và báo cáo theo phạm vi
```

## Chạy môi trường phát triển

Chạy từ repository root, trừ bước migration được ghi riêng bên dưới.

### 1. Công cụ và dependency

Cần .NET SDK theo `global.json` (hiện `10.0.401`), Node.js tương thích Vite 8, pnpm `11.19.0` và PostgreSQL có ICU. Docker chỉ cần nếu dùng database container cục bộ.

```powershell
pnpm install --frozen-lockfile
dotnet tool restore --tool-manifest backend/dotnet-tools.json
dotnet restore backend/Frms.slnx --disable-parallel
```

### 2. Database và cấu hình backend

Có thể dùng PostgreSQL cục bộ hoặc database được cấp cho môi trường phát triển. `backend/compose.postgresql.yml` cung cấp PostgreSQL 18.6, mặc định chỉ mở cổng localhost 5433; cần cung cấp `POSTGRES_PASSWORD` trước khi chạy:

```powershell
docker compose -f backend/compose.postgresql.yml up -d
```

Tự tạo file local được Git ignore `backend/Frms.Api/.env` nếu chưa có. Không phụ thuộc vào `.env.example`, không ghi đè cấu hình đã tồn tại. Cấu hình bằng giá trị thực do nhóm cung cấp:

- `ConnectionStrings__Frms` hoặc `ConnectionStrings__FrmsDb`: kết nối PostgreSQL; alias `Frms` được ưu tiên.
- `Jwt__SigningKey`: khóa bí mật cho môi trường tương ứng.
- `Payment__PayOS__*`, `Email__*`: chỉ cấu hình khi cần kiểm thử tích hợp đã được cho phép.

Không đưa secrets vào Git hoặc biến `VITE_*`. Development đọc file local; Staging/Production/Testing cần cấu hình qua môi trường hoặc hệ quản lý secrets.

Sau khi tạo database và cấu hình kết nối, chạy migration **từ thư mục `backend/`**:

```powershell
dotnet tool run dotnet-ef -- database update --project Frms.Migrations.PostgreSql --startup-project Frms.Migrations.PostgreSql --configuration Release -- --environment Development
```

Dùng project migration làm cả target và startup; API không tự triển khai schema. Chỉ chạy trên database đã xác nhận đúng môi trường. Xem [hướng dẫn migration](backend/Frms.Migrations.PostgreSql/README.md) cho connection override, seed và redeploy; không dùng SQL Server archive để bootstrap PostgreSQL.

Seed nền tảng không cung cấp sẵn tài khoản vận hành, cơ sở và bảng giá cho một hệ thống có thể demo ngay. Cần dữ liệu và tài khoản được cấp theo quy trình của nhóm.

### 3. API và frontend

Từ repository root:

```powershell
dotnet run --project backend/Frms.Api
```

HTTP profile mặc định dùng `http://localhost:5164`. `/health` kiểm tra kết nối database, không xác nhận schema hoặc nghiệp vụ đầy đủ. OpenAPI tại `/openapi/v1.json`; Swagger UI được bật trong Development.

Tự tạo `frontend/.env.local` nếu chưa có, với nội dung công khai:

```text
VITE_API_PROXY_TARGET=http://localhost:5164
```

Mở terminal khác tại repository root:

```powershell
pnpm dev
```

Frontend ở `http://localhost:5173`; Vite chuyển tiếp `/api` tới API và giữ nguyên đường dẫn. Nếu chọn backend HTTPS profile, cập nhật target theo launch settings; tùy chọn bỏ kiểm tra chứng chỉ chỉ dành cho chứng chỉ tự ký localhost.

### 4. Tác vụ nền và dịch vụ ngoài

API có job hết hạn đặt chỗ, lập hóa đơn/quá hạn/phí trễ, xử lý no-show và retry notification. Billing mặc định bật khi thiếu cấu hình; NoShow và Notifications mặc định tắt. Cần kiểm tra schema và cờ `BackgroundJobs__*__Enabled` trước khi khởi động trên database dùng chung. Xem [chi tiết recurring jobs](backend/Frms.Api/BackgroundJobs/RECURRING_JOBS.md).

payOS là gateway hiện hành trong code. Redirect trình duyệt không chứng minh thanh toán thành công; backend xác minh webhook. Kiểm thử tạo giao dịch/thanh toán thật phải có phạm vi được phê duyệt. SMTP cần cấu hình provider và thông tin xác thực; xem [email adapter](backend/Frms.Infrastructure/Email/README.md). Không suy ra dịch vụ ngoài đã hoạt động chỉ vì API khởi động được.

## Kiểm tra chất lượng

Các lệnh dưới đây là hướng dẫn chạy, không phải kết quả PASS được chứng nhận bởi README:

```powershell
dotnet build backend/Frms.slnx --configuration Release
dotnet test backend/tests/Frms.UnitTests/Frms.UnitTests.csproj --configuration Release
dotnet test backend/tests/Frms.ApiTests/Frms.ApiTests.csproj --configuration Release
dotnet test backend/tests/Frms.ArchitectureTests/Frms.ArchitectureTests.csproj --configuration Release
pnpm --dir frontend typecheck
pnpm --dir frontend lint
pnpm build:frontend
```

Integration tests cần đọc cấu hình từng fixture và database dùng một lần được cho phép. Repository còn test/hướng dẫn SQL Server lịch sử bên cạnh PostgreSQL; không truyền kết nối PostgreSQL vào fixture SQL Server hoặc coi toàn bộ suite đã tương thích provider mới.

```powershell
dotnet test backend/tests/Frms.IntegrationTests/Frms.IntegrationTests.csproj --configuration Release
pnpm test:postman
pnpm --dir tests/e2e/playwright run install:browsers
pnpm test:e2e
```

Postman/E2E cần API, dữ liệu, tài khoản và cấu hình đúng theo bộ test. Nghiệm thu nghiệp vụ quan trọng phải có kiểm thử quyền truy cập, dữ liệu/giao dịch/concurrency và hành trình E2E tương ứng; không chỉ kiểm tra màn hình.

## Hướng phát triển và triển khai

Các bước dưới đây là đề xuất thứ tự công việc, chưa thay thế kế hoạch được chủ dự án duyệt:

1. **Đồng bộ baseline:** chốt trạng thái phê duyệt V11, PostgreSQL, API và Data Dictionary; giải quyết các chênh lệch giữa hướng dẫn cũ và code.
2. **Hoàn thiện bảy luồng:** đối chiếu từng feature → quy tắc → API → service → repository/routine → test; xác định endpoint scaffold, giao diện còn dùng mock và phần chưa nghiệm thu.
3. **Củng cố các đường quan trọng:** sức chứa, thanh toán/webhook, bàn giao, hóa đơn, gia hạn, kiểm tra thiệt hại và hoàn tất trả kho; xác minh retry, quyền và chạy đồng thời.
4. **Chuẩn bị staging:** đóng gói FE/BE, migration riêng, secrets, HTTPS, lưu file bền vững, backup/phục hồi, health/logging và cấu hình job/provider.
5. **Nghiệm thu trước production:** chạy các release gate phù hợp, kiểm tra phục hồi dữ liệu và rollback ứng dụng, sau đó triển khai theo phê duyệt.

Một phương án staging là reverse proxy phục vụ React và chuyển `/api` tới ASP.NET Core, kết nối PostgreSQL qua mạng nội bộ hoặc kết nối bảo mật đến dịch vụ quản lý. Production build và `vite preview` không dùng Vite development proxy; hạ tầng cần cấu hình routing API và fallback cho React Router. Đây là hướng đề xuất, chưa phải hệ thống deploy đã có sẵn trong repository.

## Quy ước tích hợp

- REST `/api/v1`, JSON `camelCase`, enum dạng string.
- Timestamp UTC ISO-8601; lịch nghiệp vụ và hiển thị theo `Asia/Ho_Chi_Minh`.
- Tháng giữ dạng `YYYY-MM`; tiền dùng `decimal` ở backend.
- Frontend xử lý lỗi qua mã ổn định `error.code`; response lỗi có trace ID.
- Customer đăng nhập bằng số điện thoại; Employee bằng email. Backend kiểm tra tài khoản ACTIVE, vai trò, phạm vi cơ sở và quyền sở hữu tài nguyên.
