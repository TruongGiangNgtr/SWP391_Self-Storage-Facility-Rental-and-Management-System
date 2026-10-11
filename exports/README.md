# FRMS V10.1 — Unified Development Seed

**CHỈ cho database `FRMS` trên SQL Server DEVELOPMENT đã chạy EF migrations. Không chạy trên FRMS production.**

Bộ này hợp nhất **3 SQL nguồn**:
- `FRMS_DevSeed_AllTables_V10_1_Corrected.sql`: bổ sung idempotent Role/Policy bootstrap, explicit ExtraFeeTypeId, Core Demo Reservation/Access fixture và các quy tắc V10.1.
- `FRMS_DevSeed_AllTables_EPS01_PayOS.sql`: dùng làm **fixture canonical** cho 27 bảng và các Payment mock có ProviderOrderCode/IdempotencyKey.
- `FRMS_EPS01_Seed_2000VND.sql`: seed 2.000 VND **một lần** trong module 06 (bỏ bản copy đã nhúng ở cuối file PayOS).

## Target database: `FRMS` duy nhất

`EPS01` trong tên file là mã feature thanh toán, **không phải** tên database hoặc bảng. Mọi `INSERT` dùng `dbo.<table>` trong database `FRMS` đã chọn tại SSMS. Không có `CREATE DATABASE`, không tự chuyển database và không tạo schema/bảng phụ.

Nếu `FRMS` là database đang dùng chứa dữ liệu cần giữ, hãy backup trước; mỗi module COMMIT riêng và không rollback các module đã thành công khi module sau lỗi.

## Bảng seed / thứ tự thực thi

| Thứ tự | File | Nhóm dữ liệu | Tables |
|---|---|---|---|
| 1 | `01_Master_Accounts_Units.sql` | Master/Accounts/Facility/Unit | UserRole, Policy, Facility, UnitType, ExtraFeeType, DamageType, UserAccount, Customer, Employee, Discount, StorageUnit |
| 2 | `02_Reservation_Contract_Visit.sql` | Reservation/Handover snapshot | Reservation, Contract, ContractExtension, Visit |
| 3 | `03_Invoice_Payment_LateFee.sql` | Deposit/rental invoices + Payment mô phỏng | Invoice, Payment, LateFee |
| 4 | `04_Support_Inspection_Audit.sql` | Operations/Return/Support/Audit | SupportTicket, Inspection, DamageRecord, InspectionEvidence, ExtraFee, DepositSettlement, LoginHistory, AuditLog, NotificationLog |
| 5 | `05_Core_Demo_Reservation.sql` | Fixture Reservation/Confirm/Access (chỉ có trong V10.1 Corrected) | thêm Reservation, Invoice, Payment PAYOS mock, StorageUnit/Visit khi đủ điều kiện |
| 6 | `06_EPS01_2000VND.sql` | Fixture thử nghiệm Deposit 2.000 VND | thêm Customer, Facility, UnitType, StorageUnit, Reservation, Invoice (KHÔNG tạo Payment) |
| **ALL** | **`00_RUN_ALL.sql`** | **Tất cả các module theo đúng thứ tự, nội dung nhúng đầy đủ** | **27 bảng thuộc `FRMS`**, không cần SQLCMD `:r` |

## Cách chạy trong SSMS (bản sửa Msg 51401 / Msg 156)

1. Backup database development. Bảo đảm đã áp dụng EF Core migrations (Invoice.PaidAt, Payment.IdempotencyKey/ProviderOrderCode, sequence payOS và usp_ApplyPaymentResult).
2. Mở **`00_RUN_ALL.sql`** trong SSMS và chọn đúng database **`FRMS`** trên SQL Server development ở thanh dropdown.
3. **Xác nhận biến database development trên ĐẦU CÙNG FILE**, không chạy lệnh riêng ở tab khác:

```sql
DECLARE @ConfirmThisIsDevFrms bit = 1; -- CHỈ nếu FRMS là database development của bạn
DECLARE @FixtureBcryptHash nvarchar(255) = N'$2y$12$8V9Q3T6rhU8m2rkRVbVibuEORO7M2Pk0TOGT1A9K/kxfgv459UMn2'; -- mật khẩu DEMO: 11111111
```

Hash BCrypt cost-12 đã được điền sẵn cho mật khẩu DEMO `11111111` (không phải plain text). Không cần tạo hay dán thêm hash. Tuyệt đối không dùng mật khẩu `11111111` cho production. Các module độc lập vẫn đọc hash từ `SESSION_CONTEXT` như trước; nên chạy `00_RUN_ALL.sql` để được thiết lập tự động. Bộ này chỉ chấp nhận `DB_NAME() = FRMS`; không tạo database mới. Cần xác nhận FRMS là bản development.

4. Chọn **toàn bộ** file `00_RUN_ALL.sql` (hoặc không chọn vùng nào) và nhấn **F5 một lần**. Không chạy riêng khối `DECLARE` vì nó phải ở cùng batch phía trên `GO` để tạo session context.
5. Kiểm tra `PASS: All unified V10.1 seed modules executed.` và bảng `TableName/TotalRows`. Nếu lỗi, dừng và gửi **lỗi đầu tiên**; module nào đã COMMIT trước khi lỗi sẽ không rollback theo các module sau.

**Đã sửa hai lỗi trước:** (a) authorization đặt trực tiếp trong RUN_ALL sau xác nhận local dev; (b) đổi alias `RowCount` thành `TotalRows`, tránh lỗi SQL Server Msg 156. Nếu thiếu xác nhận/hash hoặc module trước fail, script báo lỗi preflight và SKIP các module kế tiếp, không tạo ra 6 lỗi 51401 liên tiếp. Không cần SQLCMD Mode.

**Lưu ý:** Những file `01_...` đến `06_...` vẫn có thể chạy độc lập, nhưng cần thiết lập `SESSION_CONTEXT` trong cùng connection khi chạy riêng theo quy định từng file. `RUN_ALL` là đường chạy được khuyến nghị.

## Quy tắc đã thống nhất

- First-month Rental Fee: **offline**, không tạo Invoice/Payment cho tháng đầu Contract.
- `Invoice.PaidAt`: các dòng PAID trong fixture mock có UTC timestamp mô phỏng, **KHÔNG là bằng chứng webhook hay tiền thật**.
- PAYMENT mới: có `IdempotencyKey` duy nhất, `ProviderOrderCode` từ sequence, `PaymentMethod=PAYOS`.
- Fixture 2.000 VND chỉ tạo **UNPAID Deposit Invoice** cho Reservation bắt đầu **tháng sau** theo lịch GMT+7. Không tạo Payment, checkout URL, webhooks hoặc gọi API payOS thật.
- Seed `IF NOT EXISTS` với **ngoại lệ do yêu cầu: cập nhật duy nhất `UserAccount.PasswordHash` của 7 demo accounts** (6 tài khoản chính + EPS01) khi ID, role, email và phone khớp. Rerun sẽ reset mật khẩu của 7 fixture này về `11111111`, không sửa các trường khác hoặc account không thuộc fixture. Không làm sạch database, không thay thế dữ liệu nghiệp vụ cũ hoặc migration history. Nếu fixed IDs hoặc data cũ xung đột với dữ liệu có sẵn thì transaction sẽ báo lỗi; không tự sửa/xóa.
- `ExtraFeeType` có GUID khóa chính không NULL; DefaultAmount chỉ là **demo values**, không dùng làm production seed.
- ZIP có embedded **BCrypt hash của mật khẩu DEMO `11111111` theo yêu cầu**; không có JWT/payOS live credentials. Chỉ dùng local development.
- Không tự chạy các 3 SQL nguồn cũ sau khi chạy package này. File `00_RUN_ALL.sql` đã nhúng 6 module: **chỉ chọn RUN_ALL hoặc từng file**, không cần chạy cả hai lượt.

## Tài khoản test

- CUSTOMER: `0901000001`, `0901000002`
- FACILITY_STAFF: `staff.demo@frms.local`
- FACILITY_MANAGER: `manager.demo@frms.local`
- BUSINESS_OPERATIONS_MANAGER: `bom.demo@frms.local`
- SYSTEM_ADMINISTRATOR: `admin.demo@frms.local`
- Deposit 2.000 VND: `0990002000`
- **Mật khẩu chung 7 tài khoản demo: `11111111`** (`BCrypt` cost 12 trong `PasswordHash`).

## Validation/status

- PASS kiểm tra tĩnh: bcrypt cost-12 được sinh và xác thực, RUN_ALL tự cấu hình hash, chỉ reset PasswordHash của 7 account demo có ID và identity khớp; không thay đổi dữ liệu nghiệp vụ. Chưa chạy SQL Server thực tế.
- NOT RUN: Không có SQL Server local, do đó chưa kiểm chứng triggers, constraints, SQL execution, e2e, idempotency trên database đã có dữ liệu.
- Các trigger/procedure thuộc migration legacy chưa được chạy/kiểm nghiệm bằng gói seed này.
