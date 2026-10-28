# Đề nghị duyệt: Notifications trong ứng dụng độc lập với email

**Mã thay đổi:** REV-2026-10-29-INAPP-NOTIFICATIONS
**Ngày:** 2026-10-29
**Trạng thái:** DRAFT — CHỜ DUYỆT; chỉ sửa tài liệu. Chưa cho phép sửa FE/BE, DB, SQL, migration, cấu hình hoặc test code.
**Baseline:** origin/main tại e54c4c9; runtime/deployment chưa được xác nhận.

## 1. Phạm vi đã thu hẹp theo yêu cầu của owner

Chỉ sửa OD-06 và tài liệu liên quan trực tiếp đến Notifications/Overview. Không bổ sung OD về email verification; không sửa đăng ký Customer/Employee, OTP/link, đăng nhập, kích hoạt tài khoản, gửi mật khẩu ban đầu, phân role/facility hay bootstrap Admin. Các mục AUTH, ADM, DD-01, DD-16 và EmailVerifiedAt giữ nguyên bản trước nhiệm vụ này. Mọi quyết định về xác nhận email sẽ là công việc riêng nếu owner yêu cầu sau.

| Trước thay đổi | Đích đề nghị duyệt |
|---|---|
| Notifications là lịch sử gửi email thành công; chỉ đọc SENT có SentAt | Notifications là thông báo nghiệp vụ trong ứng dụng, xem được sau khi sự kiện và thông báo đã commit, không phụ thuộc SMTP/SENT/SentAt. |
| Worker gửi email quyết định khi nào thông báo xuất hiện | GET Notifications/Overview đọc các sự kiện in-app hợp lệ đã lưu cho đúng Customer. |
| DTO bắt buộc sentAt, FE ghi “Sent notification history” | DTO đề xuất notificationId, notificationType, content, createdAt; hiển thị thời điểm tạo, không yêu cầu sentAt. |
| Các sự kiện nghiệp vụ đi vào hàng đợi gửi mail | Các thông báo thuộc phạm vi này là in-app only; worker không tự gửi chúng qua email. Những email của luồng khác giữ nguyên yêu cầu trước đây. |

## 2. Quy tắc chức năng đề nghị

1. Ghi thông báo và thay đổi nghiệp vụ thành công trong cùng transaction; rollback/no-op không tạo thông báo thành công. Không gọi SMTP trong transaction hoặc khi đọc trang.
2. Dùng lại các loại sự kiện đã có: Reservation CONFIRMED/CANCELLED; tạo Invoice hoặc PAID/OVERDUE/CANCELLED; Visit scheduled/rescheduled/checked-in/checked-out/cancelled; Contract created/renewed/closed; SupportTicket created/status; DepositSettlement finalized. Không tạo nghĩa tài chính mới; notice settlement không xác nhận đã chuyển tiền hoàn cọc.
3. Đổi ngày Access Visit thực sự tạo VISIT_RESCHEDULED; lưu cùng ngày không tạo. Ghi đúng Customer từ Reservation/Contract hiện hành, không nhận người nhận do client tự khai.
4. Giữ GET /api/v1/customers/me/notifications, ACTIVE CUSTOMER own-account, scope trước count/pagination; mặc định page=1/pageSize=20, tối đa 100; createdAt DESC rồi notificationId DESC. Overview lấy ba bản ghi đầu từ cùng API.
5. DTO mục tiêu: notificationId, notificationType, content, createdAt. sentAt không còn là trường bắt buộc/điều kiện đọc. FE, DTO/controller/Business result, repository và OpenAPI phải đồng bộ sau duyệt.
6. Chỉ công bố nội dung plain text của sự kiện in-app được cho phép. Không mở toàn bộ PENDING/log nội bộ, không chứa mã/link xác nhận, mật khẩu, CCCD hoặc dữ liệu người khác. Không fake SENT/SentAt để lách bộ lọc cũ.
7. Không thêm đã đọc/chưa đọc, badge, reminder gần ngày, mark-as-read, link suy từ nội dung hoặc realtime/polling. Mở lại/Refresh sau commit phải đọc được thông báo mới; thao tác đọc không tạo thêm sự kiện.
8. Giữ nguyên authentication, role/facility/ownership, capacity, giá/policy snapshot, công thức tiền, payOS, thu tháng đầu offline và mọi email của các luồng khác.

## 3. Việc cần duyệt trước khi sửa code

| Quyết định | Cần chốt |
|---|---|
| NOT-D01 — persistence/routing | Có thể tái sử dụng NotificationLog, nhưng phải xác định rõ in-app eligibility/provenance, xử lý các cột delivery cũ và bảo đảm worker không chọn in-app-only rows. Không mặc định phê duyệt bảng/column/channel/status mới. |
| NOT-D02 — lịch sử/chuyển đổi | Bản ghi cũ nào được hiển thị; bảo toàn bằng chứng SENT/SentAt; dừng/lọc business-alert mail subscriptions mà không ảnh hưởng email luồng khác; không replay/backfill hàng loạt. |

Nếu cần sửa routine/constraint/index/schema thì dùng forward EF migration sau khi thiết kế được duyệt; không sửa migration đã áp dụng. Chỉ bỏ điều kiện SENT mà chưa có quy tắc eligible data là chưa đủ. Chỉ tắt worker cũng không tạo ra phân tách routing bền vững.

## 4. Traceability và acceptance đề nghị

IMPL-CUSACT-01 / Overview -> SRS OD-06 (revision này), BR-NOT-APP-01..05 -> GET notifications -> CustomerActivityService.NotificationsAsync -> CustomerActivityRepository + eligible event recorder -> NotificationLog reuse/routing theo NOT-D01/02 -> API-NOTAPP/SEC-NOTAPP/DBT-NOTAPP/E2E-NOTAPP -> gate V11-G16. Transaction History OD-07 giữ nguyên.

- DBT-NOTAPP-001: đổi ngày Visit ghi một thông báo cho đúng Customer; rollback/no-op không tạo thông báo thành công; retry/concurrency không gây trùng ngoài sự kiện thật.
- API-NOTAPP-001: SMTP tắt/lỗi và sentAt null vẫn đọc được sự kiện in-app hợp lệ; đúng bốn field, UTC, pagination/order/count.
- SEC-NOTAPP-001: Guest/Employee/Customer khác không đọc được; không công bố log nội bộ hoặc credential/proof.
- DBT-NOTAPP-002: worker không gửi email in-app-only notices; không fake SENT/SentAt, không xóa/sửa/replay lịch sử; không ảnh hưởng email luồng khác.
- E2E-NOTAPP-001: reschedule -> mở lại/Refresh Notifications -> thấy notice; Overview cùng nguồn; loading/empty/error/plain text/GMT+7/session switch; đọc lại không ghi thêm.

Đây là kế hoạch kiểm thử, chưa tạo/sửa test code và không tuyên bố PASS thực tế. Bản source hiện tại vẫn yêu cầu SENT/SentAt; tài liệu mới không chứng minh triển khai đã hoạt động.

## 5. Checklist duyệt

- [ ] Duyệt OD-06 sửa đổi và phạm vi sự kiện.
- [ ] Duyệt bốn-field API, NOT-D01/02 và chuyển đổi dữ liệu/worker.
- [ ] Owner cho phép sửa source (hiện chưa được phép).
- [ ] BE/DB/FE triển khai đồng bộ và cung cấp acceptance thực tế.

Việc thu hẹp tài liệu không phê duyệt hay phủ nhận yêu cầu xác nhận email ngoài phạm vi này; phần tài khoản giữ nguyên baseline để tránh gộp hai thay đổi.
