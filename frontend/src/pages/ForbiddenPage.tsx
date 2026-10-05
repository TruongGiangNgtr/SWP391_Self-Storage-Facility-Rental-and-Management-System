import { Link } from 'react-router-dom'

export function ForbiddenPage() {
  return (
    <main className="page-container">
      <section className="panel">
        <h1>403 — Không có quyền truy cập</h1>
        <p>Tài khoản hiện tại không được phép mở khu vực này.</p>
        <Link className="button" to="/">
          Về trang chủ
        </Link>
      </section>
    </main>
  )
}
