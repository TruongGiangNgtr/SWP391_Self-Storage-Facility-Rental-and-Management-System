import { Link } from 'react-router-dom'

export function NotFoundPage() {
  return (
    <main className="page-container">
      <section className="panel">
        <h1>404 — Page Not Found</h1>
        <Link className="button" to="/">
          Back to Home
        </Link>
      </section>
    </main>
  )
}
