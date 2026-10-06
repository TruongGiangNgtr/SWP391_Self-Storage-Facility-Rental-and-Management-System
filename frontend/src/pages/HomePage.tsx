import { Link } from 'react-router-dom'

export function HomePage() {
  return (
    <main className="page-container">
      <section className="hero">
        <p className="muted">Self-Storage Facility Rental and Management System</p>
        <h1>FRMS Frontend Foundation</h1>
        <p>
          A React + TypeScript foundation for Customer, Staff, Manager, Business and Admin
          portals, following SRS V10.
        </p>
        <div className="hero-actions">
          <Link className="button" to="/auth/customer/login">
            Customer Sign In
          </Link>
          <Link className="button button-secondary" to="/auth/employee/login">
            Employee Sign In
          </Link>
        </div>
      </section>
    </main>
  )
}
