# Đối chiếu file V2 với FRMS_EPS01_Dev — 2026-10-11

Trạng thái: COMPLETE cho việc đối chiếu schema và xuất SQL bàn giao; **chưa triển khai**.
Chiều bổ sung: DB được tạo từ file V2 -> những thành phần hiện có trong FRMS_EPS01_Dev.
Không phải chép toàn bộ procedure/trigger V2 vào DB EPS; không so sánh/copy dữ liệu khách hàng,
tài khoản, thanh toán hay cấu hình kết nối. File đính kèm là dữ liệu tham chiếu, không thay thế SRS V10.

## Nguồn đã kiểm tra

- File: `C:/SWP391/database/FRMS_Database_V2_Final_InspectionSafe.sql`.
- SHA256: `71218DF0F8BAA331C659DEEDAB0B1EA2A1F56C89EC8748B32F2BEB02F3211FC2`.
- Metadata SQL Server trực tiếp: `FRMS_EPS01_Dev`, đọc qua cấu hình hiện tại của backend.
- Procedure trực tiếp trên DB EPS khớp nội dung migration
  `20261008094647_InvoicePaymentTimestamp.Sql.cs` sau chuẩn hóa CREATE/whitespace.
- Phạm vi nghiệp vụ: EPS-01 / PAY-001 / PAY-004; SRS V10 §12.7.1, §15.1;
  Data Dictionary V2.1 §4.13.1. Không đưa thay đổi mới từ Frms_Test_CWP04 vào gói này.

## Tổng quan

| Thành phần | File V2 sau toàn bộ các phần override | FRMS_EPS01_Dev hiện tại |
| --- | --- | --- |
| Bảng nghiệp vụ | 27 | 27 |
| Bảng lịch sử EF | Không có | __EFMigrationsHistory |
| Cột nghiệp vụ | 188 | 193 |
| Foreign key | 38 | 38, không thấy liên kết còn thiếu trong file |
| Procedure riêng biệt | 41 | 1: usp_ApplyPaymentResult |
| Trigger riêng biệt | 40 | 0 |
| Sequence cấp mã payOS | Không có | ProviderOrderCodeSequence |

Tên index/constraint khác nhau chưa đủ để kết luận thiếu: đã đối chiếu khóa, thứ tự cột,
unique/filter. Nhiều CHECK của EPS gom nhiều CHECK của file thành một; không thêm CHECK trùng.
File V2 có nhiều ràng buộc/procedure hơn nhưng có nghiệp vụ cũ; EPS không phải bản đầy đủ toàn hệ thống.

## Thành phần file V2 còn thiếu so với EPS

| Thành phần | Định nghĩa EPS |
| --- | --- |
| Payment.IdempotencyKey | uniqueidentifier NOT NULL, globally UNIQUE |
| Payment.PaymentUrl | nvarchar(2048) NULL |
| Payment.PaymentUrlExpiresAt | datetime2(3) NULL |
| Payment.ProviderOrderCode | bigint NULL, UNIQUE khi khác NULL |
| Invoice.PaidAt | datetime2(3) NULL |
| dbo.ProviderOrderCodeSequence | bigint, START 1000, INCREMENT 1, NO CYCLE |
| CK_Payment_UrlExpiry | PaymentUrlExpiresAt IS NULL OR PaymentUrl IS NOT NULL |
| PaymentMethod | Cho phép MOMO, VNPAY, PAYOS; không đổi phương thức của dòng lịch sử |
| LoginHistory.LoginAt default | SYSUTCDATETIME(), chỉ tác động INSERT sau triển khai |

Hai unique index mới là `IX_Payment_IdempotencyKey` và `IX_Payment_ProviderOrderCode`.
Unique TransactionCode đã có trong file (`UX_Payment_TransactionCode`), tương đương
`IX_Payment_TransactionCode` trên EPS, nên không tạo thêm index trùng.
Index Payment(InvoiceId,Status,CreatedAt) cũng đã có, chỉ khác tên.

## Khác biệt cần ALTER hoặc giữ nguyên

| Cột | File V2 | EPS |
| --- | --- | --- |
| Facility.Description | nvarchar(max) NULL | nvarchar(1000) NULL |
| UnitType.Description | nvarchar(max) NULL | nvarchar(1000) NULL |
| UserAccount.PasswordHash | varchar(500) NOT NULL | varchar(255) NOT NULL |
| Payment.InvoiceId | uniqueidentifier NULL | uniqueidentifier NOT NULL |
| Payment.TransactionCode | varchar(255) NULL | varchar(150) NULL |
| SupportTicket.Description | nvarchar(max) NOT NULL | nvarchar(1000) NOT NULL |
| SupportTicket.ResultNote | nvarchar(max) NULL | nvarchar(1000) NULL |
| Inspection.ConditionNote | nvarchar(max) NULL | nvarchar(1000) NULL |
| DamageRecord.Note | nvarchar(max) NULL | nvarchar(1000) NULL |
| ExtraFee.Reason | nvarchar(max) NOT NULL | nvarchar(1000) NOT NULL |
| AuditLog.Action | varchar(100) NOT NULL | varchar(150) NOT NULL |
| AuditLog.EntityType | varchar(100) NOT NULL | varchar(150) NOT NULL |
| NotificationLog.Content | nvarchar(max) NOT NULL | nvarchar(1000) NOT NULL |

Script chỉ:
- Siết InvoiceId NOT NULL sau khi xác nhận không có NULL.
- Nới AuditLog.Action / EntityType lên varchar(150), không thu hẹp cột đang lớn hơn.
- Giữ nguyên 10 cột có độ dài lớn hơn EPS. Không cắt TransactionCode, PasswordHash hay nội dung.
  Muốn đồng nhất tuyệt đối độ dài cần một migration riêng được duyệt, kiểm tra dữ liệu/dependency.
- Facility.Status đã được phần V2 cuối file đổi thành INACTIVE giống EPS; không thêm lại.
- SupportTicket.Category đã thêm OTHER; không thêm lại.
- V2 đã bỏ unique Inspection.ContractId; không tái lập.
- Không tạo __EFMigrationsHistory hay tự đánh dấu các migration là đã chạy.

## Procedure thanh toán khác phiên bản

File V2: `@PaymentId, @TransactionCode varchar(255), @ResultStatus, @PaidAt`.

EPS: `@PaymentId, @Status, @TransactionCode varchar(150), @VerifiedAmount decimal(38,18),
@VerifiedPaidAtUtc datetime2(3), @Source, @Outcome OUTPUT, @Reason OUTPUT`.

Script chứa nguyên definition hiện dùng trên EPS / migration tương ứng, không viết lại logic gateway.
Việc thay procedure là opt-in riêng vì `trg_Audit_Payment` của V2 và procedure EPS cùng ghi
`PAYMENT_RESULT`: triển khai cả hai gây audit trùng. Script chặn thay procedure nếu có
trigger Payment đang bật chứa PAYMENT_RESULT; không tự xóa/tắt trigger hay quyết định chủ thể audit.

## Cách team sử dụng SQL

File chính: `20261011_FRMS_V2_To_EPS01_Additions.sql`.

1. Backup và review DB đích được tạo từ file V2; chạy sau toàn bộ file V2.
   **Không chạy lại file V2 trên DB EPS đang dùng**: file có USE FRMS, CREATE TABLE và override cũ.
2. Mặc định `@ApplyPatch=0`: chỉ trả về DB đang chọn và thông báo, không sửa gì.
3. Sau khi duyệt, team đặt `@ExpectedDatabase` bằng tên DB đích thực tế và `@ApplyPatch=1`.
   Script không tự chọn DB, tạo DB hay chuyển connection string.
4. `@ReplacePaymentProcedure=0`: chỉ bổ sung schema, chưa làm callback cũ tương thích EPS.
   Muốn đặt 1 cần giải quyết audit ownership bằng thay đổi riêng được duyệt; nếu còn trigger
   xung đột hoặc mã giao dịch lịch sử dài hơn 150 byte thì toàn bộ giao dịch bị chặn/rollback.
5. `@AddOptionalForeignKeyIndexes=0`: không tự thêm index hiệu năng.
   Có thể đặt 1 sau review; các index đã có index không filter cùng cột đầu sẽ được bỏ qua.
6. Payment có dữ liệu nhưng chưa có IdempotencyKey thật, hoặc InvoiceId NULL: **dừng**.
   Không tạo UUID/link/timestamp giả, không xóa dữ liệu để cho script chạy.
7. Với DB quản lý bằng EF, team cần migration/adoption plan tương ứng trước triển khai.
   Đây là SQL bàn giao cho DB quản lý thủ công, không phải lý do tự ghi lịch sử EF.

Rollback trong script là atomic khi lỗi. Sau khi áp dụng thành công, cần kế hoạch restore
definition/backup được duyệt; không drop các cột chứa bằng chứng tài chính mới đã phát sinh.
Không gọi callback nghiệp vụ, gateway, payout hay refund.

## Index hiệu năng bổ sung (tùy chọn)

29 index một cột trên EPS chưa có index đúng nguyên định nghĩa trong file V2.
Một số đã được index nhiều cột hiện có bao phủ ở tiền tố, nên không bắt buộc tạo bản trùng.

| Bảng | Index EPS | Cột |
| --- | --- | --- |
| AuditLog | IX_AuditLog_UserAccountId | UserAccountId |
| Contract | IX_Contract_CustomerId | CustomerId |
| Contract | IX_Contract_DiscountId | DiscountId |
| Contract | IX_Contract_FacilityId | FacilityId |
| Contract | IX_Contract_PolicyId | PolicyId |
| ContractExtension | IX_ContractExtension_ContractId | ContractId |
| DamageRecord | IX_DamageRecord_DamageTypeId | DamageTypeId |
| DamageRecord | IX_DamageRecord_InspectionId | InspectionId |
| Discount | IX_Discount_CustomerId | CustomerId |
| ExtraFee | IX_ExtraFee_ExtraFeeTypeId | ExtraFeeTypeId |
| Inspection | IX_Inspection_ContractId | ContractId |
| Inspection | IX_Inspection_EmployeeId | EmployeeId |
| Inspection | IX_Inspection_StorageUnitId | StorageUnitId |
| Inspection | IX_Inspection_VisitId | VisitId |
| InspectionEvidence | IX_InspectionEvidence_InspectionId | InspectionId |
| Invoice | IX_Invoice_DiscountId | DiscountId |
| LoginHistory | IX_LoginHistory_UserAccountId | UserAccountId |
| NotificationLog | IX_NotificationLog_UserAccountId | UserAccountId |
| Reservation | IX_Reservation_CustomerId | CustomerId |
| Reservation | IX_Reservation_FacilityId | FacilityId |
| Reservation | IX_Reservation_PolicyId | PolicyId |
| Reservation | IX_Reservation_UnitTypeId | UnitTypeId |
| StorageUnit | IX_StorageUnit_FacilityId | FacilityId |
| StorageUnit | IX_StorageUnit_UnitTypeId | UnitTypeId |
| SupportTicket | IX_SupportTicket_AssignedEmployeeId | AssignedEmployeeId |
| SupportTicket | IX_SupportTicket_ContractId | ContractId |
| SupportTicket | IX_SupportTicket_CustomerId | CustomerId |
| UserAccount | IX_UserAccount_RoleId | RoleId |
| Visit | IX_Visit_EmployeeId | EmployeeId |

## Chiều ngược lại — không nằm trong script bổ sung này

40 procedure của file không có trên DB EPS:
- `usp_CreateReservation`
- `usp_ConfirmReservation`
- `usp_CancelReservation`
- `usp_CheckInVisit`
- `usp_CancelVisit`
- `usp_CompleteHandover`
- `usp_CreateAccessVisit`
- `usp_CreateReturnVisit`
- `usp_ConfirmActualReturn`
- `usp_ClaimInspection`
- `usp_RecordDamage`
- `usp_RecordExtraFee`
- `usp_AddInspectionEvidence`
- `usp_CompleteInspection`
- `usp_FinalizeReturn`
- `usp_RenewContract`
- `usp_CreateMonthlyInvoice`
- `usp_CreatePaymentAttempt`
- `usp_CalculateLateFee`
- `usp_MarkOverdueInvoices`
- `usp_CreatePolicyVersion`
- `usp_AssignSupportTicket`
- `usp_CompleteSupportTicket`
- `usp_CancelSupportTicket`
- `usp_SetUserAccountStatus`
- `usp_QueueNotification`
- `usp_MarkNotificationSent`
- `usp_RetryPendingNotification`
- `usp_ExpirePendingReservations`
- `usp_ProcessReservationNoShow`
- `usp_GenerateMonthlyInvoices`
- `usp_RecalculateOpenLateFees`
- `usp_RescheduleVisit`
- `usp_CreateSupportTicket`
- `usp_SetDamageRecordStatus`
- `usp_CreateCustomerAccount`
- `usp_CreateEmployeeAccount`
- `usp_UpdateEmployeeRoleFacility`
- `usp_DecideDamage`
- `usp_ReplaceInactiveEmployeeCredentialHash`

40 trigger của file không có trên DB EPS:
- `trg_Visit_ValidateEntity`
- `trg_Invoice_ValidateEntity`
- `trg_Invoice_ProtectSnapshot`
- `trg_Invoice_StatusTransition`
- `trg_Reservation_StatusTransition`
- `trg_Visit_StatusTransition`
- `trg_Contract_StatusTransition`
- `trg_Inspection_StatusTransition`
- `trg_Inspection_ValidateVisit`
- `trg_StorageUnit_StatusTransition`
- `trg_SupportTicket_StatusTransition`
- `trg_Policy_ProtectHistoricalVersion`
- `trg_Policy_ProtectBusinessValues`
- `trg_ContractExtension_AppendOnly`
- `trg_LoginHistory_AppendOnly`
- `trg_AuditLog_AppendOnly`
- `trg_Contract_ValidateDiscountOwnership`
- `trg_Employee_ValidateFacilityByRole`
- `trg_UserAccount_ValidateRoleProfile`
- `trg_LateFee_ValidateInvoice`
- `trg_Audit_Reservation`
- `trg_Audit_Visit`
- `trg_Audit_Contract`
- `trg_Audit_Invoice`
- `trg_Audit_Payment`
- `trg_Audit_ContractExtension`
- `trg_Audit_Inspection`
- `trg_Audit_DamageRecord`
- `trg_Audit_ExtraFee`
- `trg_Audit_DepositSettlement`
- `trg_Audit_Policy`
- `trg_Audit_UnitType`
- `trg_Audit_Discount`
- `trg_Audit_SupportTicket`
- `trg_Audit_UserAccount`
- `trg_Audit_Employee`
- `trg_StorageUnit_ProtectUnitTypeReassignment`
- `trg_Discount_ProtectReferencedSemantics`
- `trg_DamageRecord_StatusTransition`
- `trg_UserAccount_ProtectLoginIdentifiers`

Đây là chênh lệch triển khai chiều ngược lại, không phải thành phần file thiếu so với EPS.
Không tự triển khai chúng: cần review theo SRS V10, đặc biệt handover/first-month offline,
return finalization, ngày Visit GMT+7 và audit. Không tuyên bố 41 procedure/40 trigger cũ đều đúng.

## Kiểm chứng và Git

- Đọc metadata trực tiếp: thành công; chỉ schema/migration metadata, không xuất dữ liệu nghiệp vụ.
- PARSEONLY trên SQL Server: thành công cho script ngoài và 44 batch SQL nhúng; không thực thi DDL.
- Chạy script nguyên trạng @ApplyPatch=0: thành công, trả FRMS_EPS01_Dev / DRY RUN / no changes.
- Chưa chạy nhánh @ApplyPatch=1 trên DB V2 clone; chưa kiểm chứng tương tác với toàn bộ trigger cũ.
  PARSEONLY không xác nhận constraint/data/dependency/runtime/concurrency.
- Không đổi code ứng dụng, EF migration, test, frontend, file SQL gốc, .env hoặc DB trong lượt này.
  Không chạy build/test vì chỉ xuất artifact SQL; không coi đây là nghiệm thu triển khai.
- Nhánh: be/CWP-04/Reservation-Deposit-Management; HEAD/base c49d8a43a618687bc2015adfbb1a23f0d52ed2cf.
  origin/main e3966be05954ba36559f5b55347557f34ae3b0ce sau fetch; không merge/rebase.
- Chỉ thêm file script và báo cáo này; các thay đổi CWP-04 và file untracked có trước được giữ nguyên.
  Không stage/commit/push; working tree vẫn có thay đổi chưa commit.
