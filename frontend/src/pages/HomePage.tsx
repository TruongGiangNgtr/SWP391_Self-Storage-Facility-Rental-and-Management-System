import { Link } from 'react-router-dom'

export function HomePage() {
  return (
    <main className="page-container">
      <section className="hero">
        <p className="muted">Self-Storage Facility Rental and Management System</p>
        <h1>FRMS Frontend Foundation</h1>
        <p>
          Nền tảng React + TypeScript cho Customer, Staff, Manager, Business và Admin
          Portal theo SRS V10.
        </p>
        <div className="hero-actions">
          <Link className="button" to="/auth/customer/login">
            Đăng nhập khách hàng
          </Link>
          <Link className="button button-secondary" to="/auth/employee/login">
            Đăng nhập nhân viên
          </Link>
        </div>
      </section>
    </main>
  )
}
