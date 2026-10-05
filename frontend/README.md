# FRMS Frontend

Frontend chính thức của FRMS, xây dựng bằng React + TypeScript + Vite theo SRS V10.

## Yêu cầu môi trường

- Node.js đáp ứng yêu cầu của Vite hiện tại.
- pnpm 11.19.0 theo cấu hình tại repository root.
- Visual Studio Code hoặc IDE tương đương.

## Cài đặt

Chạy từ repository root:

```powershell
corepack enable
pnpm install --frozen-lockfile
Set-Location frontend
Copy-Item .env.example .env.development
```

Không commit file `.env.development`.

## Chạy Development

Từ repository root:

```powershell
pnpm --dir frontend dev
```

Mặc định Vite mở tại `http://localhost:5173`.

Trong development, Vite chuyển tiếp request bắt đầu bằng `/api` sang ASP.NET
Core tại `http://localhost:5164`. Hãy chạy backend bằng HTTP profile tương ứng.

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
```

Giá trị tương đối này đi qua Vite proxy khi phát triển. Khi triển khai, hạ tầng
cần định tuyến `/api` tới backend hoặc cung cấp URL API và CORS phù hợp cho môi
trường đó.

Không đặt JWT signing key, database password, MoMo secret hoặc bất kỳ secret nào
trong biến `VITE_*`, vì các biến này được đóng gói vào mã chạy trên trình duyệt.

## Nguyên tắc tích hợp

- JSON dùng `camelCase`.
- Enum dùng string đúng SRS.
- Timestamp từ API là UTC; giao diện hiển thị GMT+7.
- Frontend xử lý lỗi bằng `code`, không phân tích `message`.
- Backend là nguồn chính thức của capacity, tiền và lifecycle.
- Release 1 không có refresh-token subsystem.
