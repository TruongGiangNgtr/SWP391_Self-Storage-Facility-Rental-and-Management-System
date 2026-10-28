import { useAuth } from '../auth/auth.context'
import { Link } from 'react-router-dom'
import { FileQuestion } from 'lucide-react'

export function NotFoundPage() {
  const { user } = useAuth()
  const customerSurface = !user || user.role === 'CUSTOMER'
  return (
    <main className={`page-container ${customerSurface ? 'support-error-page' : ''}`}>
      <section className={`panel ${customerSurface ? 'support-error-panel' : ''}`}>
        {customerSurface && <FileQuestion size={40} strokeWidth={1.5} aria-hidden="true" />}
        <h1>404 — Page Not Found</h1>
        {customerSurface && <p>The page you are looking for could not be found.</p>}
        <Link className="button" to="/">
          Back to Home
        </Link>
      </section>
    </main>
  )
}
