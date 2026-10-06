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
