# FRMS — Hướng dẫn thiết kế Frontend thống nhất Guest & Customer

**Phiên bản:** 1.3 · **Ngày:** 22/10/2026\
**Mục đích:** Sử dụng tài liệu này cùng hai bộ mẫu Guest Page và Customer Portal để dựng một trải nghiệm liên tục, dễ sử dụng và nhất quán.\
**Phạm vi:** Thiết kế giao diện, bố cục, component dùng chung, điều hướng và tiêu chí nghiệm thu UX. Tài liệu không thay thế SRS, không tự cấp quyền API và không xác nhận các chức năng đã được triển khai.

**Cập nhật UX 1.1 theo yêu cầu chủ dự án:** Navigation Customer phải hiển thị đủ các mục đã thiết kế, kể cả mục chưa triển khai chức năng; khi mở mục chưa triển khai phải có trang thông báo rõ. Icon sidebar kế thừa màu mục menu thay vì xanh cố định. Logo footer dùng cùng icon, ô xanh, typography và kích thước với header; chỉ đổi màu chữ FSto để đọc được trên nền tối. Đây là quyết định trình bày, không bổ sung nghiệp vụ hoặc quyền API.

**Cập nhật UX 1.2 theo phê duyệt chủ dự án ngày 14/10/2026:** Đợt đồng bộ này ưu tiên web trên máy tính/laptop cho Guest và Customer, nghiệm thu tại 1024, 1280, 1440 và 1920 CSS px. Mobile không là điều kiện hoàn tất của đợt này; giữ hỗ trợ co giãn/drawer đã có, không ép toàn trang có chiều rộng tối thiểu hoặc xóa responsive. Không thay đổi nghiệp vụ SRS, backend, database hoặc portal nhân viên. Các yêu cầu keyboard, modal, zoom và trạng thái lỗi vẫn áp dụng trên desktop.

**Cập nhật UX 1.3 theo phê duyệt chủ dự án ngày 22/10/2026:** Find Storage chỉ hiển thị kết quả sau khi có kỳ thuê hợp lệ được gửi qua form hoặc URL; mở trang chưa tìm không tự liệt kê toàn bộ Facility. Size Guide đọc `imageUrl`/`videoUrl` của chính UnitType, mở video khi khách yêu cầu và có trạng thái thay thế khi media thiếu/lỗi. Bỏ trang/menu Identity Verification riêng: thông tin họ tên và CCCD nằm trong Account Settings, đường dẫn cũ chuyển về trang này. Đây là thay đổi UX/tích hợp đọc API hiện có, không bổ sung xác minh danh tính, trạng thái Verified, quyền đọc hay sửa BE/database; quy tắc bảy mục menu ở dưới thay thế quy tắc tám mục trước đây.

## 1. Nguồn tham chiếu và thứ tự ưu tiên

| Nguồn | Vai trò |
|---|---|
| SRS V10 đang có hiệu lực trong repository | Quyết định quyền truy cập, nghiệp vụ, trạng thái, dữ liệu, tiền và điều kiện thực hiện hành động |
| Các quyết định UX đã thống nhất với chủ dự án | Guest và Customer dùng chung chức năng công khai; đăng nhập thông thường vào dashboard; đăng nhập giữa thao tác tiếp tục thao tác đó |
| `customer_dashboard/code.html` và `customer_dashboard/screen.png` | Chuẩn chính cho logo, header, sidebar, footer, typography, màu sắc, kích thước, card, button và input |
| Các ảnh Guest Page và frame Home trong Figma | Bố cục và nội dung các trang công khai, auth và bước chuyển từ Guest sang Customer |
| Các HTML/ảnh Customer Portal còn lại | Bố cục nội dung riêng, thông tin cần hiển thị và vị trí hành động từng trang |
| `storage_hub_ui/DESIGN.md` | Tham khảo bổ sung khi không xung đột với dashboard |
| Frontend hiện có | Nguồn component, route, formatter, auth và API để tái sử dụng |

**Quy tắc quan trọng:** Dashboard là chuẩn hình thức, không phải nguồn thay thế SRS cho dữ liệu hoặc nghiệp vụ. Nội dung mẫu trong dashboard cũng cần đối chiếu SRS.

Đường dẫn bộ mẫu:

- Guest: `D:\Ki5\SWP\UI\guest page`.
- Customer: `D:\Ki5\SWP\UI\customer portal\stitch_fstorent_customer_dashboard`.
- Dashboard chuẩn: `D:\Ki5\SWP\UI\customer portal\stitch_fstorent_customer_dashboard\customer_dashboard`.
- Figma Guest Home: https://www.figma.com/design/vsAJIrNF7AxPMS3j8rJqFl/Storage-Prototype?node-id=214-133299.
- Repository đã đối chiếu: `C:\Users\PC\Documents\ChatGPT\SWP 2`.
- SRS thực tế trong checkout đã đọc: `docs/FRMS_SRS_V10.md`. Khi vị trí tài liệu thay đổi, xác định đúng SRS đang có hiệu lực theo `AGENTS.md`; không sử dụng SRS V9 làm baseline.

## 2. Mô hình trải nghiệm đã thống nhất

### 2.1 Guest

Guest là trạng thái chưa đăng nhập, không tạo business role thứ sáu.

Mục tiêu UX của Guest:

- Tìm kiếm facility, xem thông tin facility và các loại storage.
- Xem kích thước, giá và kết quả tìm kiếm phù hợp kỳ thuê khi quyền đọc dữ liệu đã được phê duyệt.
- Xem How It Works, Size Guide, Help Center và chính sách công khai.
- Bắt đầu lựa chọn kho và được yêu cầu đăng nhập khi cần thực hiện hành động có tài khoản.

### 2.2 Customer

Customer tiếp tục dùng tất cả chức năng công khai qua cùng các trang/component, đồng thời có khu vực quản lý reservation, visit, contract, invoice/payment, phí/return và support theo SRS.

Không dựng một bản Find Storage, Size Guide hoặc Help Center khác chỉ vì người dùng đã đăng nhập. Các trang này giữ nguyên nội dung và hình thức, chỉ thay phần tài khoản và hành động được phép thực hiện.

### 2.3 Hai tình huống đăng nhập

| Điểm bắt đầu | Đích sau đăng nhập thành công |
|---|---|
| Chọn Sign In thông thường, không có thao tác đang chờ | Customer Dashboard/Overview |
| Chọn Create Reservation trong kết quả tìm kho | Tiếp tục lựa chọn và bước reservation đang thực hiện, sau khi kiểm tra điều kiện hiện tại |
| Mở một đường dẫn Customer yêu cầu đăng nhập | Trở lại đường dẫn nội bộ hợp lệ nếu tài khoản có quyền |

Identity Verification, Confirm Reservation và Deposit không tự động trở thành chuỗi bắt buộc sau mọi lần đăng nhập. Chỉ hiển thị bước tương ứng khi nghiệp vụ và dữ liệu của thao tác đang tiếp tục yêu cầu nó.

## 3. Hai layout dùng chung một thương hiệu

### 3.1 Public Layout

Áp dụng cho Guest Home, Find Storage, Facility Detail, Size Guide, How It Works, Help Center, Policy Detail và các trang auth được phê duyệt.

```text
Shared Header — khu vực tài khoản thay đổi theo phiên đăng nhập
Breadcrumb khi cần
Nội dung công khai rộng toàn khung
Shared Footer — liên kết Account thay đổi theo phiên đăng nhập
```

Customer mở trang công khai vẫn dùng Public Layout, không thêm sidebar. Nhờ vậy kết quả tìm kho và các lựa chọn không đổi vị trí sau đăng nhập.

### 3.2 Customer Portal Layout

Áp dụng cho dashboard và các trang quản lý cá nhân.

```text
Shared Header
Khung nội dung
├── Customer Sidebar
└── Breadcrumb + Page Header + nội dung trang
Shared Footer
```

Sidebar bổ sung công cụ quản lý; không thay thế bốn mục điều hướng công khai trên header.

### 3.3 Kích thước thống nhất

| Thuộc tính | Chuẩn |
|---|---|
| Header desktop | Cao 80px; fixed; nền trắng; viền dưới 1px |
| Khung header và nội dung | `max-width: 1240px`; căn giữa; tổng chiều rộng bao gồm padding |
| Padding ngang desktop/tablet | 24px mỗi bên |
| Sidebar desktop | 248px, không co; nằm ở đầu cột, không kéo cao theo toàn bộ nội dung |
| Khoảng cách sidebar–content | 24px |
| Khoảng cách nội dung dưới header | 32px, sau phần bù chiều cao header |
| Khoảng cách giữa khối nội dung | Thường 32px; trong grid 24px |
| Khung footer | `max-width: 1160px`; padding ngang 24px, dọc 48px |
| Card chuẩn | Padding 24px; bo 16px; viền 1px |
| Control chuẩn | Cao tối thiểu 44px; bo 12px |

Với khung 1240px có padding 24px mỗi bên, chiều rộng nội dung hữu dụng là 1192px. Có sidebar 248px và gap 24px thì cột chính khoảng 920px tại chiều rộng tối đa. Không cố giữ cột chính 968px rồi làm tràn khung.

Không sao chép `min-width: 1440px` từ Figma hoặc HTML modal vào ứng dụng. Không suy kích thước thiết kế từ độ rộng ảnh screenshot đã được thu nhỏ; dùng số liệu HTML/dashboard và kiểm tra ở cùng viewport.

## 4. Design tokens theo dashboard

### 4.1 Màu sắc

| Token | Giá trị | Sử dụng |
|---|---|---|
| Brand / Primary | `#2563EB` | Button chính, link, icon, mục đang chọn |
| Primary hover | `#1D4ED8` | Hover của button chính |
| Secondary blue | `#0EA5E9` | Điểm nhấn phụ; không dùng làm một màu logo footer khác header |
| Dark blue | `#1E40AF` | Điểm nhấn xanh đậm khi có trong mẫu |
| Text primary | `#0F172A` | Tiêu đề, thông tin quan trọng |
| Body text | `#334155` / `#475569` | Nội dung và mô tả |
| Text secondary | `#64748B` | Metadata, helper text |
| Canvas | `#F8FAFC` | Nền portal và các section nhẹ |
| Surface | `#FFFFFF` | Card, header, modal |
| Border | `#E2E8F0` | Viền, divider |
| Brand tint | `#EFF6FF` | Active item, info card nhẹ |
| Footer | `#0F172A` | Nền footer |
| Footer secondary text | `#94A3B8` | Mô tả và liên kết |

Status dùng xanh lá cho thành công, xanh/sky cho thông tin, amber cho cần chú ý và đỏ cho lỗi/hành động nguy hiểm. Luôn đi kèm chữ hoặc icon; không dùng màu làm dấu hiệu duy nhất. Status màu xám có thể dùng cho lịch sử/kết thúc trung tính theo ngữ cảnh.

Không tạo một palette tím riêng từ `#FAF8FF`, `#F2F3FF` hoặc `#004AC6` trong các HTML khác. Những màu này phải được quy về tokens dashboard cho thành phần dùng chung.

### 4.2 Typography

| Loại | Font | Cỡ / trọng lượng gợi ý |
|---|---|---|
| Logo header | Plus Jakarta Sans | 24px desktop, 20px mobile / 700 |
| Logo footer | Plus Jakarta Sans | Cùng cỡ và trọng lượng header tại cùng breakpoint: 24px desktop, 20px mobile / 700 |
| Public hero | Plus Jakarta Sans | 48px desktop; 32px mobile / 700 |
| Tiêu đề portal | Plus Jakarta Sans | 30px desktop; 24px mobile / 700 |
| Tiêu đề section | Plus Jakarta Sans | 20px / 700 |
| Tiêu đề card | Plus Jakarta Sans | 16–18px / 600–700 |
| Nội dung / input / navigation | Inter | 14px / 400–600 |
| Metadata / label | Inter | 12px / 500–600 |
| Eyebrow / badge nhỏ | Inter | 11–12px / 600–700 |

Không thu nhỏ dữ liệu quan trọng để cố nhét vào card. Số tiền, ngày, mã contract và trạng thái phải đọc được; khi thiếu chỗ thì đổi grid, xuống dòng hợp lý hoặc mở chi tiết.

### 4.3 Hình dạng, spacing và motion

- Dùng thang spacing 4, 8, 12, 16, 24, 32, 48 và 64px.
- Card/sidebar bo 16px; control bo 12px; icon tile bo 8–12px; badge pill.
- Shadow card chuẩn: `0 1px 3px rgba(15, 23, 42, 0.04)`.
- Card hiển thị dữ liệu không cần nhảy hoặc phóng to khi hover.
- Transition màu/shadow 150–200ms; tôn trọng `prefers-reduced-motion`.
- Mỗi trang dùng một bộ tokens trung tâm; không định nghĩa lại theme cho từng trang.

## 5. Header, sidebar và footer

### 5.1 Shared Header

Ba vùng giữ nguyên vị trí: logo trái, menu công khai giữa, tài khoản phải.

- Logo dùng icon khối hộp thống nhất; ô xanh 40 × 40px, bo 12px.
- Chữ `FSto` màu tối, `Rent` màu primary.
- Menu: Find Storage, How It Works, Size Guide, Help; gap desktop 32px.
- Guest: nút Sign In.
- Customer: chuông thông báo, divider, avatar 36px, tên, badge Customer và nút mở menu tài khoản.
- Chuông dẫn tới khu vực Notifications / Transaction History ngay cả khi khu vực đó mới có trang thông báo chưa triển khai. Không hiển thị chấm unread hoặc số lượng giả khi chưa có dữ liệu thật.
- Tên dài không đẩy menu ra khỏi khung; thu gọn phần tên trước khi làm vỡ header.
- Khi chuyển trạng thái đăng nhập, giữ nguyên chiều cao header và tọa độ logo/menu.
- Mục đang xem có trạng thái active rõ; dropdown có trạng thái mở, đóng và keyboard focus.

### 5.2 Customer Sidebar

Sao chép hình thức từ dashboard: profile tile nền slate nhạt, avatar 48px, tên, email khi có, badge Customer; nav bên dưới; divider và Sign Out màu đỏ.

Active item: nền brand tint, chữ/icon primary, viền trái 4px; dùng `aria-current="page"`. Trang chi tiết giữ active ở nhóm cha, ví dụ Contract Detail thuộc My Contracts.

**Tên mục cần chuẩn hóa theo chức năng thực tế:**

- `my_bookings` trong bộ mẫu thực chất là My Reservations.
- `my_reservations` trong bộ mẫu thực chất là My Visits.
- Không tạo thêm một nghiệp vụ Booking độc lập từ nhãn My Bookings.
- Khi dựng navigation, dùng một cấu hình chung và ánh xạ rõ Reservations ↔ reservation, Visits ↔ visit.
- Dashboard làm chuẩn hình thức sidebar; tên/mục được ánh xạ đúng ngữ nghĩa SRS. Không ẩn mục chỉ vì chức năng của trang đó chưa được triển khai.
- Sidebar và drawer Customer dùng cùng một cấu hình, cùng thứ tự đủ 7 mục: **Overview → My Visits → My Reservations → My Contracts → Deposit & Invoices → Support Tickets → Account Settings**, sau đó divider và Sign Out. Không có mục Identity Verification riêng.
- Account Settings chứa Profile Information với Full Name và National ID / CCCD theo AUTH-004/005. CCCD chỉ cho sửa khi own-profile GET có field hợp lệ (`string | null`); missing/malformed là chưa hỗ trợ, không phải giá trị rỗng để ghi đè. Phone Number và Email vẫn chỉ đọc. Các mục thật sự chưa triển khai có chú thích **Not available yet**, không dùng link `#`, không đưa tới 404 hay báo giả thành công.
- Icon của mọi mục sidebar/drawer dùng `currentColor`: màu trung tính `#334155` / `#475569` khi chưa chọn; primary `#2563EB` khi active. My Reservations/My Contracts không được có fill/stroke xanh hard-code lúc inactive. Active chỉ theo route hiện tại, không theo tiến độ triển khai.
- Notifications / Transaction History nằm chung một khu vực truy cập qua chuông header, không tạo hai mục sidebar riêng chỉ vì bộ mẫu có hai folder. Khi chưa triển khai, cả hai tab vẫn hiển thị và có thông báo tương ứng.
- Họ tên/CCCD là thông tin hồ sơ, không phải kết quả xác minh danh tính. Không tạo trạng thái APPROVED/Verified, workflow xét duyệt hoặc bước bắt buộc sau login. `/customer/identity-verification` chỉ là alias chuyển về Account Settings sau các guard đăng nhập/role hiện có; không xóa dữ liệu Customer hay policy công khai về giấy tờ.

### 5.3 Shared Footer

Giữ cùng logo, nền, bốn cột, kích thước và padding giữa Guest/Customer.

**Logo đồng nhất header/footer:** Dùng một component Brand và cùng asset icon hộp trắng; ô primary xanh 40 × 40px, bo 12px; wordmark Plus Jakarta Sans 24px/700 và gap 12px trên desktop. Trên mobile cả hai cùng dùng ô 36 × 36px, wordmark 20px/700 và gap 8px. Chữ **Rent giữ primary `#2563EB` ở cả hai nơi**. Chữ **FSto là `#0F172A` trên header trắng và `#FFFFFF` trên footer tối** để đảm bảo tương phản. Không dùng một icon/logo-footer khác hình hộp, bỏ ô xanh hoặc đổi toàn bộ wordmark thành trắng chỉ ở footer.

| Cột | Nội dung |
|---|---|
| Brand | Logo, mô tả ngắn |
| Storage | Find Storage, Size Guide, How It Works |
| Support | Help Center, Contact Us |
| Account — Guest | Sign In, Customer Registration |
| Account — Customer | My Account, Sign Out |

Policy nằm trong Help Center; có thể dẫn trực tiếp tới policy từ nội dung liên quan. Không bắt buộc thêm một menu Policies thứ năm trên header.

My Account của Customer dẫn tới Account Settings; khi trang đó chưa triển khai thì mở thông báo chưa khả dụng, không âm thầm đưa về Overview. Lối quay về Overview vẫn có ở logo/menu tài khoản. Các mục footer công khai chưa triển khai vẫn giữ nhãn và chú thích Not available yet; không tạo thông tin liên hệ, policy hoặc trang public đầy đủ trong task chỉ sửa navigation Customer.

Footer theo nội dung trang, không fixed vào đáy viewport. Trang ngắn dùng shell `min-height: 100dvh` và vùng chính flex để footer nằm cuối trang. Không thêm khoảng trắng tùy ý bằng `margin-top` riêng từng trang.

## 6. Responsive và khả năng tiếp cận

Các quy tắc sau là đề xuất kỹ thuật cho việc dựng UI, không phải breakpoint đã tồn tại đầy đủ trong ứng dụng.

| Viewport | Hành vi |
|---|---|
| Từ 1024px | Header desktop; sidebar portal 248px; grid co theo cột nội dung thực tế |
| 768–1023px | Thu gọn header/menu; portal nav chuyển thành drawer hoặc panel mở bằng nút; nội dung chính toàn chiều rộng |
| Dưới 768px | Padding ngang 16px; section xếp dọc; header 64px; menu drawer; form 1–2 cột tùy không gian |

- Breakpoint của card/form phụ thuộc chiều rộng cột, không chỉ viewport. Portal có sidebar nên đổi grid sớm hơn Public Layout.
- Portal sidebar drawer chỉ xuất hiện trong khu vực quản lý; public menu vẫn truy cập được.
- Nút menu có tên dễ hiểu; hiển thị trang hiện tại; drawer đóng khi chọn route.
- Footer hai cột khi đủ chỗ và một cột ở màn hình hẹp.
- Modal: rộng tối đa khoảng 600–640px; chừa ít nhất 16px hai bên; chiều cao tối đa theo viewport; nội dung cuộn; nút xác nhận luôn tiếp cận được bằng cuộn/keyboard.
- Bảng có nhiều cột được cuộn ngang trong vùng bảng hoặc đổi thành card; toàn trang không tràn ngang.
- Vùng chạm tối thiểu 44 × 44px; link nhỏ có padding đủ dùng.
- Có label thật cho input, focus nhìn thấy, heading theo thứ tự, skip link tới nội dung chính.
- Modal có tên, focus được giữ trong modal và trả về nút mở khi đóng; Escape đóng khi không có tác vụ đang gửi.
- Hover không phải cách duy nhất để mở thông tin/hành động.
- Nghiệm thu đợt desktop ở 1024, 1280, 1440 và 1920 CSS px; đồng thời kiểm tra zoom 200% và cửa sổ thấp/thu nhỏ. Các viewport 360, 390 và 768px là kiểm tra hồi quy hỗ trợ hiện có, không phải điều kiện hoàn tất đợt desktop.

## 7. Component dùng chung và quy tắc tái sử dụng

Tên dưới đây mô tả trách nhiệm; không bắt buộc tạo mới nếu repository đã có component tương đương.

| Nhóm | Component/trách nhiệm |
|---|---|
| Shell | Shared Header, Shared Footer, Public Layout, Customer Portal Layout, Sidebar |
| Navigation | Breadcrumb, Account Menu, Mobile Drawer, Tabs |
| Form | Field, Select, Month Field, Date Field, Password Field, validation/helper text |
| Actions | Primary, Outline, Ghost và Destructive Button |
| Feedback | Status Badge, Info Banner, Api Error Alert, Loading, Empty State |
| Catalog | Storage Search Form, Facility Card, Unit Type Card, Size Guide Card |
| Business summaries | Reservation Summary, Contract Card, Visit Card, Invoice/Payment Summary |
| Dialog | Sign-in Gate, Confirm Reservation, Renewal, Return |
| Portal | Metric Card, Notification Item, Transaction Item, Support Ticket Card |

Mỗi button/control dùng một component/style chuẩn; trạng thái disabled/loading không định nghĩa lại ở từng trang. Cùng trạng thái nghiệp vụ dùng cùng nhãn và badge trên dashboard, danh sách và chi tiết.

Code đã có cần kiểm tra để tái sử dụng:

- `frontend/src/layouts/CustomerLayout.tsx`, `RootLayout.tsx`.
- `frontend/src/styles/customer.css`.
- `frontend/src/components/BrandLogo.tsx`, `FlowIcon.tsx`, `FlowDialog.tsx`.
- `StatusBadge.tsx`, `ApiErrorAlert.tsx`, `PageStates.tsx`, `CalendarDateField.tsx`.
- `frontend/src/utils/formatters.ts`.
- Auth context, `RequireAuth.tsx`, `RequireRole.tsx` và các page hiện có.

Frontend hiện dùng React + TypeScript và CSS cùng các icon có sẵn. HTML mẫu dùng Tailwind CDN chỉ là tài liệu tham chiếu; không tự cài Tailwind, framework UI hoặc copy các theme CDN riêng vào production.

## 8. Tìm kho liên tục trước và sau đăng nhập

### 8.1 Một Search Form dùng chung

Guest Home, Customer Dashboard và Search Results dùng cùng mô hình lựa chọn:

1. Location/Facility.
2. Storage Type: Private hoặc Public theo dữ liệu được phê duyệt.
3. Storage Size/Unit Type.
4. Check-in Month.
5. Check-out Month.
6. Find Storage.

Dashboard có thể bố trí form hai hàng để đủ chỗ; không bỏ hai trường tháng chỉ để giữ form một hàng.

Điều chỉnh cách chia cột theo diện tích hiện có. Nhãn luôn nằm trên input. Hai trường tháng dùng month picker phù hợp; điều kiện hợp lệ lấy từ SRS/API, không hard-code từ ảnh mẫu.

Filters có thể lưu trong URL để quay lại, tải lại hoặc chia sẻ kết quả. URL chỉ chứa bộ lọc công khai như facility/unit type/tháng/sort/page, không chứa dữ liệu định danh hay thông tin thanh toán.

### 8.2 Search Results

- Mở Find Storage khi chưa có kỳ thuê đã gửi: chỉ hiện form và hướng dẫn bắt đầu; chưa hiện Facility/UnitType results, số lượng, applied-search summary hoặc sort. Tháng mặc định trong form không có nghĩa đã chạy tìm kiếm.
- Bấm Find Storage với kỳ thuê hợp lệ hoặc mở URL có kỳ thuê hợp lệ mới tải kết quả. Link từ Home/Size Guide, reload, Back/Forward và phân trang giữ bộ lọc đã gửi; link chỉ có mode/size từ Size Guide vẫn chờ khách xác nhận kỳ thuê và tìm kiếm.
- Location/type/size vẫn tùy chọn theo SRS/API; không ép đủ năm trường chỉ vì đổi UX. API đọc gợi ý location/size vẫn độc lập và không phải kết quả available. Không thay quyền general browsing của BE hoặc bỏ featured facilities trên Home.
- Có tóm tắt địa điểm và kỳ thuê đang xem.
- Giữ giá trị filter khi chỉnh hoặc quay lại.
- Phân nhóm unit type theo facility theo ảnh mẫu.
- Card hiển thị loại kho, kích thước, mô tả ngắn, giá tháng và khả năng đáp ứng kỳ thuê khi API cho phép.
- Private và Public có cách diễn đạt rõ: Public là không gian/dụng cụ lưu trữ trong kho chung theo scope; không tự thêm loại Vehicle Parking hoặc Climate Controlled từ option mẫu.
- Reservation chọn Facility + UnitType + kỳ thuê. Không hiển thị rằng một physical unit đã được cấp trước handover.
- Khi nhận conflict do capacity thay đổi, giữ lựa chọn, báo dễ hiểu và cho người dùng cập nhật kết quả.
- Có trạng thái không có kết quả và hành động chỉnh bộ lọc; không hiển thị facility/giá giả như dữ liệu thật.

### 8.3 Facility Detail

Hai bộ mẫu chưa cung cấp đầy đủ trang chi tiết facility riêng. Khi dựng, dùng shell/style chung và dữ liệu catalog được SRS/API cung cấp. Thông tin bổ sung như bản đồ, ảnh, tiện ích chỉ hiển thị khi có nguồn xác thực; không suy chúng từ ảnh placeholder.

## 9. Đăng nhập và khôi phục thao tác

### 9.1 Sign-in Gate

Guest chọn Create Reservation thì hiển thị modal có:

- Tiêu đề Sign in to continue.
- Tóm tắt facility, unit type và kỳ thuê đã chọn.
- Sign In, Create Account và quay lại kết quả.

Chưa gọi lệnh tạo reservation chỉ để hiện gate; gate không giữ capacity và không phát sinh deposit.

### 9.2 Khôi phục ngữ cảnh

- Giữ định danh facility/unit type, StartMonth/EndMonth và đích nội bộ cần tiếp tục.
- Cơ chế lưu tạm phải giới hạn theo phiên/tab và dọn khi hoàn tất, hủy hoặc logout; không chứa password, token hay CCCD.
- Luồng login → register → login, nếu có, giữ cùng ý định đang chờ.
- Sau auth, kiểm tra role và đích nội bộ hợp lệ; không redirect sang URL bên ngoài do người dùng truyền vào.
- Đọc lại dữ liệu hiện tại để kiểm tra giá, capacity, tình trạng facility và bước được phép thực hiện.
- Không tự tạo reservation, xác nhận hay thanh toán chỉ vì login thành công; người dùng vẫn thực hiện hành động tương ứng.
- Nếu đích/lựa chọn không còn hợp lệ, đưa về kết quả phù hợp, giữ bộ lọc còn dùng được và giải thích nguyên nhân.
- Mở URL chi tiết trực tiếp phải có page hoàn chỉnh; modal overlay không được là cách duy nhất để truy cập bước đó.

**Hiện trạng sau các task Guest:** `CustomerLoginPage` dùng continuation đã kiểm tra để trở lại thao tác bị gián đoạn; login thông thường về `/customer`. Sau login vẫn đọc lại catalog và chờ người dùng xác nhận, không tự tạo Reservation. Kiểm thử UI giả lập không thay thế kiểm thử tích hợp auth/catalog/backend thật.

### 9.3 Identity và bước xác nhận

Không gắn một màn hình identity verification riêng như điều kiện toàn cục sau login. Trường định danh, trạng thái và quyền sửa lấy theo SRS/API của bước đang thực hiện. Nếu dữ liệu đã đủ, không buộc nhập lại; nếu thiếu, hiển thị ngay tại bước phù hợp.

Customer đăng nhập bằng Phone Number + Password theo SRS hiện tại. Email trong mockup không được đổi authentication identifier. Google login, OTP, employee activation và password-reset flow trong ảnh chỉ là mẫu UX cho tới khi có yêu cầu/API được phê duyệt.

## 10. Nội dung từng trang và nguồn mẫu

### 10.1 Guest

| Trang | Nguồn | Nội dung cần giữ |
|---|---|---|
| Home | Figma node `214:133299` | Hero, search, lợi ích, Public/Private, quy trình, nhu cầu lưu trữ, featured facility, CTA, hỗ trợ |
| Size Guide — Private | `Screenshot 2026-10-13 155047.png` | Các size card, gợi ý vật dụng, Find Storage, khu vực AI |
| Size Guide — Public | `Screenshot 2026-10-13 155055.png` | Tab Public, size card tương ứng, tư vấn |
| How It Works | `155110.png`, `155115.png` trong cùng bộ Screenshot | Quy trình, thông tin cần chuẩn bị, giải thích handover |
| Help Center | `155124.png` | Sáu nhóm policy và khu vực hỗ trợ |
| Search Results | `155131.png` | Filter, facility group, unit type card và Create Reservation |
| Sign-in Options | `155136.png` | Customer/Employee entry |
| Sign-in Gate | `155157.png` | Giữ lựa chọn kho, Sign In/Create Account |
| Customer auth | `155208.png` | Hình thức login/register; điều chỉnh trường theo SRS |
| Password/activation | `155217.png` | Tham khảo hình thức; không tự thêm flow |
| Email verification | `155224.png` | Tham khảo hình thức; không tự thêm OTP |
| Identity gate | `155231.png` | Tham khảo bố cục bổ sung dữ liệu khi nghiệp vụ cần |
| Confirm Reservation | `155237.png` | Tóm tắt lựa chọn và deposit; đối chiếu tiền/trạng thái với SRS |

Các tên rút gọn `155xxx.png` ở bảng trên đều chỉ file `Screenshot 2026-10-13 155xxx.png` trong thư mục Guest.

Size Guide là hai trạng thái của cùng trang, không phải hai bản layout khác nhau. Chọn Find Storage từ size card truyền lựa chọn storage type/size sang tìm kho.

How It Works phải diễn đạt rõ reservation, deposit, hẹn handover và contract theo SRS. Không dùng bốn bước marketing để bỏ mất yêu cầu nghiệp vụ.

Help Center chứa policy dễ đọc, có mục lục/quay lại và liên kết ngữ cảnh. Các card đang ghi SAMPLE POLICY là placeholder, không phải chính sách chính thức. Không công bố raw policy quản trị hoặc lịch sử khách khác.

AI nằm trong Size Guide như chatbot đã thống nhất. Khu vực input/recommendation trong mẫu là điểm tham chiếu hình thức; hướng triển khai cụ thể và quyền gọi AI phải khớp SRS. AI không tự đặt kho; lỗi AI vẫn cho phép chọn size/tìm kho thủ công.

### 10.2 Customer Portal

| File mẫu | Trang thực tế | Nội dung cần giữ |
|---|---|---|
| `customer_dashboard` | Overview | Tìm kho, metric, contract, notification, lịch handover, deposit, support |
| `my_bookings` | My Reservations | Filter/status tabs, deposit invoice, handover visit, contract sau hoàn thành |
| `my_reservations` | My Visits | Reservation/Access/Return Visit; lịch và trạng thái |
| `my_contracts` | My Contracts | Contract đang thuê, lịch sử, rental terms, hành động |
| `contract_details_request_extension_modal` | Contract Detail + Renewal | Thông tin contract và chọn kỳ gia hạn |
| `contract_details_request_return_modal` | Contract Detail + Return | Chọn ngày, ghi chú, giải thích tác động và xác nhận |
| `deposit_charges` | Deposit & Charges | Chọn contract, số liệu và lịch sử liên quan |
| `identity_verification` | Tham khảo phần hồ sơ trong Account Settings | Không còn trang/menu riêng; chỉ dùng field hợp lệ, không hiển thị Verified hay workflow xét duyệt |
| `support_tickets` | Support Tickets | Search/filter, ticket card, phản hồi gần nhất, tạo/xem ticket |
| `notifications_history_notifications` | Notifications tab | Thông báo, thời điểm, hành động về tài nguyên liên quan |
| `notifications_history` | Transaction History tab | Giao dịch/điều chỉnh được hệ thống ghi nhận |
| `account_settings` | Account Settings | Profile/password theo API được hỗ trợ |

**Media Size Guide:** `imageUrl`/`videoUrl` thuộc UnitType và được đọc từ public catalog; không suy từ Facility/name hoặc ảnh mẫu. Chấp nhận URL HTTPS không chứa credentials; missing/unsupported/broken image có fallback rõ. Ảnh giữ tỷ lệ nội dung; video chỉ tải player khi bấm Watch Video, không autoplay. Video trực tiếp dùng native controls; YouTube/Vimeo hợp lệ dùng URL embed đã chuẩn hóa thay vì nhúng HTML/iframe tùy ý. Có Close/Escape/focus return và Open Video in New Tab khi provider hạn chế phát nhúng. Không bịa dimensions, vật dụng chứa vừa, capacity hoặc unit assignment từ media. Kiểm thử mock không xác nhận URL thật/provider/CORS/codec đang hoạt động.

Notifications và Transaction History là hai tab của một khu vực trong mẫu, không nhân bản hai layout. Hành động ở dashboard dẫn tới đúng tài nguyên/nhóm quản lý, không dùng mọi link về một trang chung.

Customer có thể có nhiều contract. Các thông tin deposit/fees/return phải chỉ rõ contract đang xem; khi đổi contract thì cập nhật dữ liệu và trạng thái loading tương ứng.

## 11. Điều hướng và routes

Giữ các route hiện có tương thích khi thay hình thức. Tên dưới đây cho Public là đề xuất cấu trúc UX, không phải endpoint mới được phê duyệt.

| Khu vực | Route |
|---|---|
| Public Home | `/` |
| Public Search — đề xuất | `/facilities` |
| Public Facility Detail — đề xuất | `/facilities/:facilityId` |
| How It Works — đề xuất | `/how-it-works` |
| Size Guide — đề xuất | `/size-guide` |
| Help/Policy — đề xuất | `/help`, `/help/policies/:policyKey` |
| Customer login — đang có | `/auth/customer/login` |
| Customer register — đang có | `/auth/customer/register` |
| Employee login — đang có | `/auth/employee/login` |
| Customer dashboard — đang có | `/customer` |
| Customer search — đang có | `/customer/storage-search` |
| Reservations — đang có | `/customer/reservations` |
| Visits — đang có | `/customer/visits` |
| Contracts — đang có | `/customer/contracts` |
| Invoices — đang có | `/customer/invoices` |
| Support — đang có | `/customer/support-tickets` |
| Identity Verification — legacy redirect, không menu riêng | `/customer/identity-verification` → `/customer/account-settings` |
| Account Settings — own profile | `/customer/account-settings` |
| Notifications / Transaction History — UI scaffold | `/customer/notifications`, tab lịch sử dùng `?tab=history` |

Các route Customer ở trên giữ Customer Layout và RequireAuth/RequireRole CUSTOMER. Account Settings đọc/cập nhật own profile theo AUTH-004/005; không có workflow xác minh danh tính riêng. Mục/tab chưa triển khai hiển thị thông báo rõ, không bổ sung API, dữ liệu mẫu, thao tác nghiệp vụ hoặc quyền đọc dữ liệu. Hai tab Notifications/Transaction History giữ cùng shell và hoạt động với keyboard, back/refresh/direct URL; quyền và phạm vi dữ liệu luôn theo SRS/API hiện hành.

Nếu bổ sung public search, tái sử dụng page/catalog logic và giữ route Customer cũ hoạt động bằng alias hoặc redirect giữ filters. Không đổi toàn bộ route chỉ để giống tên folder UX.

Logo ở Guest dẫn Home; logo ở portal có thể dẫn Overview theo navigation hiện có. Menu tài khoản có lối quay về Overview khi Customer đang ở trang công khai.

Contact Us là thông tin liên hệ công khai. Việc gửi Support Ticket là hành động được xác thực và gắn Contract theo SRS hiện tại; không biến một form contact công khai thành nghiệp vụ ticket mới.

## 12. Feedback, form và dữ liệu thật

### 12.1 Trạng thái bắt buộc

Mỗi page/component tải dữ liệu cần có loading, empty, validation, forbidden, not found, conflict và unexpected error khi áp dụng.

- Loading: skeleton giữ chiều cao tương đối để hạn chế nhảy bố cục; button đang gửi có nhãn rõ và chặn gửi lặp.
- Empty: nói rõ chưa có dữ liệu, có hành động phù hợp như Find Storage hoặc chỉnh filters.
- Validation: lỗi đặt cạnh field; không xóa các giá trị hợp lệ.
- Forbidden: giải thích không có quyền; không giả thành danh sách trống.
- Not found: có lối quay lại danh sách/kết quả.
- Conflict: giải thích dữ liệu/trạng thái thay đổi, giữ thông tin nhập còn hợp lệ và đọc lại dữ liệu.
- Unexpected error: thông báo ngắn, có retry khi an toàn; trace ID có thể để trong vùng chi tiết hỗ trợ.
- Chức năng chưa triển khai: giữ navigation và shell đầy đủ; hiển thị **This feature is not available yet. Please try again later.** tại trang tương ứng. Đây là tình trạng khả dụng đã biết, không giả một lỗi HTTP, không bịa trace ID và không gọi API chưa có chỉ để nhận lỗi. Không có submit/retry nghiệp vụ khi chức năng chưa được triển khai.

### 12.2 Form và hành động

- Mỗi màn hình có một primary action nổi bật; các action phụ dùng outline/ghost.
- Hành động hủy/return có diễn đạt rõ tác động và xác nhận phù hợp mẫu, không tự thêm approval nghiệp vụ.
- Không dùng `alert()` thay cho giao diện lỗi hoặc modal chuẩn.
- Submit thành công dựa trên phản hồi server; không báo đã trả tiền từ browser return URL.
- Luồng chưa có API không được dùng dữ liệu mẫu để báo thành công.
- Field dài, ghi chú và mã tham chiếu không phá layout; chỉ truncation khi có cách xem đầy đủ.

### 12.3 Hiển thị tiền, tháng và ngày

- Dùng formatter hiện có để thống nhất VND và múi giờ `Asia/Ho_Chi_Minh`.
- API timestamp là UTC; màn hình hiển thị giờ Việt Nam.
- `YYYY-MM` là giá trị tháng; không chuyển qua timezone như timestamp.
- Dữ liệu tiền/settlement/capacity lấy từ server; frontend không là nguồn quyết định cuối cùng.
- Số dư deposit dự kiến phải ghi rõ là dự kiến; kết quả refund cuối cùng theo settlement.
- Empty/null là chưa có dữ liệu, không tự hiển thị thành 0 hoặc Paid.
- Status hiển thị được dịch sang nhãn dễ hiểu nhưng giữ đúng trạng thái từ API; không thêm trạng thái lưu trữ từ mockup.
- Không dùng UUID của Contract/Reservation/Invoice/Staff làm nhãn chính cho Customer. Dùng tên UnitType, mã StorageUnit, kỳ thuê và liên kết về tài nguyên liên quan khi dữ liệu có sẵn. Không rút gọn UUID để giả thành mã hợp đồng, tự đặt tên/mã hoặc suy diễn dữ liệu thiếu; hiển thị chưa có thông tin khi API chưa cung cấp.
- Invoice phải nêu kỳ thuê khi có và có liên kết về đúng Reservation (DEPOSIT) hoặc Contract (RENTAL_FEE) theo `entityId`; không tính lại tiền hoặc gán tên kho không được API xác nhận. Các khoản phí trong trang tracking giữ ngữ cảnh Contract đang xem.

## 13. Những chỗ trong mẫu phải đối chiếu SRS trước khi dựng chức năng

| Chi tiết mẫu | Cách xử lý |
|---|---|
| Customer login bằng Email | Giữ hình thức, dùng PhoneNumber theo AUTH-002 |
| Google Employee login, OTP/email activation/reset | Không tự triển khai từ ảnh; cần yêu cầu/API được phê duyệt |
| Option Personal/Business/Vehicle/Climate trong dashboard | Chỉ dùng storage mode/unit type thực tế được dự án phê duyệt |
| Ngày handover mẫu 15/10 | Không dùng làm rule; ngày hợp lệ theo captured Policy |
| Giá/deposit/duration trong ảnh | Là dữ liệu minh họa; không dùng làm công thức hoặc seed mới |
| MoMo trong My Reservations | SRS hiện có payOS; nhãn/provider theo cấu hình được phê duyệt |
| Màn hình “Deposit and monthly rental” | First-month rent thu offline trước handover; không tạo khoản thanh toán tháng đầu online từ mockup |
| Transaction History có payment tháng đầu | SRS không tạo Invoice/Payment tháng đầu; nếu trình bày settlement ban đầu phải mô tả đúng nguồn contract, không tạo bản ghi payment giả |
| Extension “Pay & Extend”, tính ngay giá × tháng | Đối chiếu CON-003, billing và giá renewal hiện hành; không tự thêm nghĩa vụ trả trước hoặc dùng công thức mẫu |
| Deposit auto-deducted, Escrow Hold, monthly locker maintenance | Không suy ra ví/escrow/deduction theo thời gian thực hoặc phí định kỳ mới; chỉ hiển thị dữ liệu và phép tính SRS cho phép |
| Return type xác định từ ngày khách chọn | Có thể giải thích dự kiến; ActualReturnDate quyết định kết quả theo SRS |
| Identity APPROVED/Locked/verification reference | Không thêm entity, state hay approval process từ mockup |
| AES-256, Tier-1 Verified, hỗ trợ 24/7/2 giờ | Chỉ công bố khi có cơ sở được phê duyệt; bỏ claim minh họa chưa xác thực |
| View Facility Map, Download Summary | Chỉ nối hành động khi có dữ liệu/chức năng được phê duyệt |
| “SRS V9.1 COMPLIANT”, “BR-RES-04”, “atomic handover” | Bỏ khỏi giao diện người dùng; dùng lời giải thích nghiệp vụ dễ hiểu |
| Policy SAMPLE POLICY | Không trình bày như chính sách chính thức; thay bằng nội dung được phê duyệt |
| Bluetooth gate, QR/access pass trong DESIGN.md | Không thêm phần cứng hoặc nghiệp vụ ngoài SRS |

## 14. Phân biệt thiết kế đã thống nhất và quyền triển khai

Các quyết định UX trong tài liệu đã được chủ dự án đồng ý: cùng thương hiệu/tokens, header/footer, public pages dùng chung, portal có sidebar, đủ bộ lọc tháng, hai đích sau login và dashboard làm chuẩn hình thức.

SRS V10 hiện hành đã phê duyệt revision Guest ngày 13/10/2026, gồm public catalog/policy, optional AI và login continuation. Phê duyệt yêu cầu không có nghĩa backend/frontend đã triển khai và kiểm thử đầy đủ; khi nối API phải kiểm tra contract và implementation theo baseline hiện hành. Không dùng guide UX để tự bỏ authorization hoặc mở dữ liệu cá nhân.

Việc giữ đủ navigation Customer theo bảy mục hiện hành không xác nhận mọi chức năng đã hoàn tất. Identity Verification đã được gộp vào Account Settings theo UX 1.3, không phải một workflow còn chờ dựng. Account Settings/Notifications và các task public/login continuation được triển khai theo phạm vi phê duyệt riêng; menu/placeholder hoặc ảnh đẹp không chứng minh API/nghiệp vụ đã được kiểm thử.

Các trang auth/profile/identity chưa có contract đầy đủ phải được xác định theo SRS/API. Việc có screenshot không đồng nghĩa chức năng thuộc Release 1.

## 15. Trình tự triển khai đề xuất

1. Chốt revision SRS cho những thay đổi quyền/luồng cần thiết; xác định phạm vi task frontend và các API đã có.
2. Kiểm kê component/route hiện tại; xác định file cần sửa; giữ nguyên code của các portal nhân viên ngoài phạm vi.
3. Thiết lập tokens và logo chung theo dashboard.
4. Chuẩn hóa Shared Header/Footer, Public Layout và Customer Portal Layout.
5. Chuẩn hóa form tìm kho và các catalog card; Guest/Customer tái sử dụng cùng phần này.
6. Dựng Guest Home, How It Works, Size Guide và Help/Policy theo bộ mẫu và yêu cầu đã phê duyệt.
7. Dựng login gate và tiếp tục thao tác sau auth; kiểm tra hai tình huống đăng nhập.
8. Áp dụng shell/tokens cho Reservations, Visits, Contracts, Deposit/Invoices và Support; giữ logic API/nghiệp vụ hiện có.
9. Dựng modal renewal/return theo bố cục mẫu và semantics SRS.
10. Kiểm tra responsive, keyboard, trạng thái lỗi và các hành trình hoàn chỉnh.

Không sửa đồng loạt HTML nguồn chỉ để có cảm giác thống nhất; trong ứng dụng dùng shared component để đảm bảo mọi trang thực sự nhận cùng một thay đổi. Giữ các file UX nguồn làm tài liệu đối chiếu.

## 16. Checklist nghiệm thu UX

### Hình thức

- [ ] Guest và Customer cùng logo, font, tokens, card/control.
- [ ] Header cùng vị trí logo/menu và chiều cao theo từng breakpoint.
- [ ] Chỉ khu vực tài khoản thay đổi khi đăng nhập.
- [ ] Footer cùng cấu trúc, padding và kích thước; link Account đúng phiên.
- [ ] Logo header/footer cùng icon, ô xanh, font/cỡ/gap tại cùng breakpoint; Rent cùng primary, FSto có màu phù hợp nền.
- [ ] Public pages giữ cùng chiều rộng/bố cục khi Customer mở.
- [ ] Portal pages có sidebar và active item đúng nhóm chức năng.
- [ ] Sidebar và drawer có đủ 7 mục hiện hành, không có Identity Verification riêng; icon inactive trung tính, active primary; link identity cũ chuyển về Account Settings.
- [ ] Không còn nền tím/theme khác ở component dùng chung.
- [ ] Không tràn ngang toàn trang ở các viewport kiểm tra.

### Điều hướng

- [ ] Login thông thường tới Overview.
- [ ] Login giữa thao tác giữ đúng facility/unit type/tháng và quay lại đúng bước.
- [ ] Login không tự buộc identity/confirmation/payment và không tự submit.
- [ ] Chuyển login/register vẫn giữ ý định đang chờ khi được phép.
- [ ] Back/refresh/direct URL hoạt động; không phụ thuộc overlay đang mở.
- [ ] Đích login được giới hạn nội bộ; role/ownership được server kiểm tra.
- [ ] Logout dọn draft nhạy cảm; không còn dữ liệu cá nhân khi mở lại màn hình.

### Nội dung và trạng thái

- [ ] Guest/Customer Search Form cùng đủ năm nhóm lựa chọn.
- [ ] Không hiển thị physical unit trước handover như đã được cấp.
- [ ] Provider, giá, deposit, renewal và return theo SRS/API.
- [ ] Các khoản tiền luôn chỉ rõ reservation/contract liên quan.
- [ ] Không giả Payment/Invoice tháng đầu.
- [ ] Support tạo theo contract và quyền được phê duyệt.
- [ ] AI lỗi vẫn tìm kho thủ công được.
- [ ] Loading/empty/validation/403/404/conflict/unexpected error được kiểm tra.
- [ ] Không có link `#` chết, thành công giả hoặc lời quảng cáo chưa xác thực.
- [ ] Các mục chưa triển khai mở trang thông báo rõ trong shell; không 404, không gọi API chưa có, không bịa trace ID hoặc dữ liệu.
- [ ] Notifications/Transaction History là hai tab cùng khu vực; chuông và My Account vẫn có đích đúng khi trang chưa khả dụng.

### Accessibility và kiểm tra implementation

- [ ] Keyboard dùng được header, drawer, form, tab và modal.
- [ ] Focus rõ, label đủ, contrast đạt mức AA khi đo, status có chữ/icon.
- [ ] Desktop control đúng kích thước guide; modal và bảng không che mất hành động. Giữ khả năng truy cập mobile đã có, không lấy mobile làm release gate của đợt desktop.
- [ ] Đối chiếu screenshot ở cùng viewport; kiểm tra thêm tên dài/nhiều contract/không có dữ liệu.
- [ ] Khi sửa code, chạy scripts typecheck/build/lint và kiểm tra hành trình liên quan theo repository; ghi rõ kết quả thật.
- [ ] Không coi screenshot đẹp hoặc build pass là bằng chứng nghiệp vụ đã hoàn tất.

## 17. Cách sử dụng tài liệu cùng hai bộ UI

Khi dựng mỗi trang:

1. Mở ảnh/HTML trang tương ứng để lấy thứ tự nội dung và hành động.
2. Áp dụng layout, tokens và component từ dashboard theo tài liệu này.
3. Đối chiếu từng field/status/action với SRS và API đang có.
4. Tái sử dụng logic/component tương thích trong frontend, không nhân bản một phiên bản cho Guest và một phiên bản cho Customer.
5. Kiểm tra trang trong cả hai trạng thái đăng nhập khi trang thuộc nhóm công khai.
6. So sánh hình ảnh và kiểm tra thao tác thực tế; ghi lại phần API/SRS chưa đủ trước khi nối chức năng.

**Kết quả mong muốn:** Người dùng tìm kho trên Guest, đăng nhập và tiếp tục thao tác với cùng lựa chọn, cùng bố cục quen thuộc. Khi vào khu vực quản lý, họ nhận thêm sidebar và dữ liệu cá nhân trong cùng một hệ thống giao diện.
