# FRMS Frontend

Frontend chính thức của FRMS, xây dựng bằng React + TypeScript + Vite theo SRS V10.

## Yêu cầu môi trường

- Node.js đáp ứng yêu cầu của Vite hiện tại.
- pnpm 11.19.0 theo cấu hình tại repository root.
- Visual Studio Code hoặc IDE tương đương.

## Cài đặt

Chạy từ repository root:

```powershell
pnpm install --frozen-lockfile
Copy-Item frontend/.env.example frontend/.env.local
```

Không commit file `.env.local`.

## Chạy Development

Từ repository root:

```powershell
pnpm --dir frontend dev
```

Mặc định Vite mở tại `http://localhost:5173`.

Trong development, Vite chuyển tiếp request bắt đầu bằng `/api` tới
`VITE_API_PROXY_TARGET`, giữ nguyên đường dẫn và dùng `changeOrigin: true`.
Mẫu `.env.example` dùng HTTP profile `http://localhost:5164` được khai báo trong
`backend/Frms.Api/Properties/launchSettings.json`.

Nếu dùng HTTPS profile `https://localhost:7235`, chạy backend với
`--launch-profile https` và cập nhật target trong `.env.local`. Chỉ bật
`VITE_API_PROXY_ALLOW_SELF_SIGNED=true` cho development certificate tự ký trên
localhost; các target khác giữ xác minh chứng chỉ. Khởi động lại Vite sau khi đổi môi trường.

## Kiểm tra chất lượng

Chạy từ repository root:

```powershell
pnpm --dir frontend typecheck
pnpm --dir frontend lint
pnpm --dir frontend build
pnpm test:e2e
```

Nếu Playwright chưa có Chromium trên máy:

```powershell
pnpm --dir tests/e2e/playwright run install:browsers
```

Playwright dùng chung cho toàn repository tại `tests/e2e/playwright`; frontend
không duy trì một bộ cấu hình hoặc dependency Playwright riêng.

## Biến môi trường

```text
VITE_API_BASE_URL=/api/v1
VITE_API_PROXY_TARGET=<URL từ launchSettings.json>
VITE_API_PROXY_ALLOW_SELF_SIGNED=false
```

Giá trị tương đối này đi qua Vite proxy khi phát triển. Khi triển khai, hạ tầng
cần định tuyến cùng-origin `/api` tới backend. Production build và Vite preview
không dùng development proxy. HTTP client mặc định dùng `/api/v1` khi chưa đặt biến base URL.

Không đặt JWT signing key, database password, MoMo secret hoặc bất kỳ secret nào
trong biến `VITE_*`, vì các biến này được đóng gói vào mã chạy trên trình duyệt.

## Nguyên tắc tích hợp

- JSON dùng `camelCase`.
- Enum dùng string đúng SRS.
- Timestamp từ API là UTC; giao diện hiển thị GMT+7.
- Frontend xử lý lỗi bằng `code`, không phân tích `message`.
- Backend là nguồn chính thức của capacity, tiền và lifecycle.
- Release 1 không có refresh-token subsystem.

## Giao diện Flow 1 — CWP-01/03/04/05

Baseline nghiệp vụ: `docs/FRMS_SRS_V10.md` trên `main`, các mục 4 (Flow 1),
12 (business rules/state transitions), 13.10 và 13.13–13.16 (API contracts),
20.2 (feature traceability).

Đăng nhập CUSTOMER, mở `/customer`, rồi thực hiện lần lượt:

1. `/customer/storage-search`: chọn cơ sở, loại kho, kích thước và tháng thuê;
   bấm **Tìm kho** để đọc sức chứa từ CAT-003. Danh sách cơ sở và loại kho đọc
   đủ các trang catalog. Chọn kết quả để sang bước xác nhận.
2. `/customer/reservations/new?...`: kiểm tra lựa chọn; **Xác nhận đặt kho** gọi
   RES-001 chỉ với `facilityId`, `unitTypeId`, `startMonth`, `endMonth`.
3. `/customer/reservations/:reservationId`: xem đơn và Deposit Invoice,
   thanh toán MoMo qua PAY-001 hoặc hủy đơn đủ điều kiện qua RES-005.
4. `/customer/payments/result`: đọc PAY-003, kiểm tra lại tối đa sáu lần tự động
   khi giao dịch còn PENDING, và cho phép kiểm tra thủ công. Trang này không
   sử dụng query string của MoMo để quyết định kết quả thanh toán.
5. Khi hóa đơn tiền cọc PAID và đơn PENDING_DEPOSIT, mở trang `.../confirm`,
   chọn ngày bàn giao và gửi RES-004. Ngày hẹn theo captured Policy do BE kiểm tra.
6. `/customer/visits/:visitId`: xem lịch hẹn; đổi/hủy lịch SCHEDULED qua
   VIS-005/VIS-006 rồi đọc lại VIS-004 để hiển thị trạng thái chính thức.

Các trang `/customer/reservations`, `/customer/invoices`, `/customer/visits`
đọc danh sách của chính khách hàng và hỗ trợ phân trang. Tất cả route trên
được bảo vệ bởi RequireAuth và role CUSTOMER; BE vẫn phải kiểm tra quyền sở hữu.

Frontend hiển thị giá, tiền cọc và trạng thái trả về từ API, không tính hoặc lưu
thêm trạng thái nghiệp vụ. Việc giữ sức chứa atomic và chọn StorageUnit khi bàn giao
là trách nhiệm backend.

Giao diện Flow 1 dùng thiết kế FStoRent ở Page 1 của `Storage-Prototype`:

- `214:134482`: Storage Search Results.
- `214:136720`: popup Confirm your Reservation.
- `315:8019`: My Reservations, header/footer và sidebar khách hàng.
- `315:6277`: popup Create Visit, điều chỉnh thành lịch RESERVATION theo SRS V10.

CSS riêng tại `src/styles/customer.css`, không thêm Tailwind hoặc thư viện UI.
Các icon SVG lấy từ Figma được lưu tại `public/figma/flow1` và giữ kích thước gốc.
Inter và Plus Jakarta Sans được lưu tại `public/fonts`, kèm giấy phép OFL từ
repository Google Fonts. Tên khách hàng/email trên header/sidebar lấy từ AUTH-004.

Màn thanh toán và danh sách/chi tiết lịch hẹn dùng các component/style chung của
thiết kế đã đọc. Chưa xác minh khớp toàn bộ các frame này vì kết nối Figma đã hết
quota đọc. Toàn bộ nội dung giao diện do frontend cung cấp dùng tiếng Anh,
bao gồm form đăng nhập/đăng ký, điều hướng, thông báo lỗi và lịch chọn ngày.
Tên, mô tả và dữ liệu nhập của người dùng được hiển thị nguyên văn từ API.
Ngày/giờ hiển thị theo locale tiếng Anh, giữ timezone GMT+7; tiền tệ vẫn là VND.

Tìm kho: đăng nhập Customer → chọn **Find Storage** trên header (hoặc mở
`/customer/storage-search`) → nhập Location / Facility, Unit Type, Unit Size,
Check-in Month, Check-out Month → bấm **Search**. Kết quả nằm trong section
**Storage Search Results** ngay dưới form trên cùng trang. Trước khi tìm,
trang hiển thị hướng dẫn thay vì dữ liệu mẫu. Khi CAT-001/CAT-003 còn trả 501,
frontend chưa có kết quả thật để hiển thị.

Dữ liệu mẫu trong Figma không được đưa vào runtime. Thời hạn cọc lấy từ
`depositInvoice.dueDate`; tiền và trạng thái lấy từ API. API hiện cung cấp
`paymentUrl`, chưa cung cấp QR, nên FE chuyển sang cổng MoMo. Tiền thuê tháng đầu
không được gộp thành tiền cọc. Lịch bàn giao thuộc Reservation và không có trường
chọn nhân viên hoặc khung giờ trong contract hiện tại.

Ảnh cơ sở, kích thước thành nhiều chiều, danh sách vật dụng phù hợp, mã tham chiếu
thân thiện, số Contract/StorageUnit sau bàn giao và bộ lọc global trong Figma chưa
có đủ dữ liệu trong response hiện tại: FE không tự thêm vào DTO hoặc tạo dữ liệu
thay thế. Danh sách Reservation hiển thị mã loại kho từ API trong lúc chờ contract
hiển thị bổ sung được phê duyệt; không biến UUID thành tên kho giả.

Các controller CAT/RES/BIL/PAY/VIS hiện trên baseline `main` còn trả HTTP 501.
Vì vậy cần backend hoàn thiện các endpoint trước khi kiểm thử tích hợp với SQL Server
và MoMo sandbox. Test bằng API giả lập chỉ xác nhận hành vi frontend và payload;
không thay thế các release gate DBT/CON/INT/E2E trong SRS.
