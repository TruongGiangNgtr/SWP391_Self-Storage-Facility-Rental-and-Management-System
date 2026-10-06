import { Link } from 'react-router-dom'

export function ForbiddenPage() {
  return (
    <main className="page-container">
      <section className="panel">
        <h1>403 — Access Denied</h1>
        <p>Your account does not have access to this area.</p>
        <Link className="button" to="/">
          Back to Home
        </Link>
      </section>
    </main>
  )
}
