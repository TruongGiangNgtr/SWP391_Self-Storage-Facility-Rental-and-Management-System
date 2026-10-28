import { useAuth } from '../auth/auth.context'
import { Link } from 'react-router-dom'
import { ShieldAlert } from 'lucide-react'

export function ForbiddenPage() {
  const { user } = useAuth()
  const customerSurface = !user || user.role === 'CUSTOMER'
  return (
    <main className={`page-container ${customerSurface ? 'support-error-page' : ''}`}>
      <section className={`panel ${customerSurface ? 'support-error-panel' : ''}`}>
        {customerSurface && <ShieldAlert size={40} strokeWidth={1.5} aria-hidden="true" />}
        <h1>403 — Access Denied</h1>
        <p>Your account does not have access to this area.</p>
        <Link className="button" to="/">
          Back to Home
        </Link>
      </section>
    </main>
  )
}
