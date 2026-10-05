import { Link } from 'react-router-dom'

export function NotFoundPage() {
  return (
    <main className="page-container">
      <section className="panel">
        <h1>404 — Không tìm thấy trang</h1>
        <Link className="button" to="/">
          Về trang chủ
        </Link>
      </section>
    </main>
  )
}
