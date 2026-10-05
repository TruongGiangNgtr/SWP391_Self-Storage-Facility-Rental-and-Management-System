# FRMS Frontend

Frontend chính thức của FRMS, xây dựng bằng React + TypeScript + Vite theo SRS V10.

## Yêu cầu môi trường

- Node.js đáp ứng yêu cầu của Vite hiện tại.
- npm.
- Visual Studio Code hoặc IDE tương đương.

Máy hiện tại đã có Node.js 24 và npm 11.

## Cài đặt

```powershell
npm install
Copy-Item .env.example .env.development
```

Kiểm tra `VITE_API_BASE_URL` trong `.env.development` và chỉnh theo URL thật của
ASP.NET Core Web API.

## Chạy Development

```powershell
npm run dev
```

Mặc định Vite mở tại `http://localhost:5173`.

## Kiểm tra chất lượng

```powershell
npm run typecheck
npm run lint
npm run build
npm run test:e2e
```

Nếu Playwright chưa có Chromium trên máy:

```powershell
npx playwright install chromium
```

## Biến môi trường

```text
VITE_API_BASE_URL=https://localhost:7001/api/v1
```

Không đặt JWT signing key, database password, MoMo secret hoặc bất kỳ secret nào
trong biến `VITE_*`, vì các biến này được đóng gói vào mã chạy trên trình duyệt.

## Nguyên tắc tích hợp

- JSON dùng `camelCase`.
- Enum dùng string đúng SRS.
- Timestamp từ API là UTC; giao diện hiển thị GMT+7.
- Frontend xử lý lỗi bằng `code`, không phân tích `message`.
- Backend là nguồn chính thức của capacity, tiền và lifecycle.
- Release 1 không có refresh-token subsystem.
