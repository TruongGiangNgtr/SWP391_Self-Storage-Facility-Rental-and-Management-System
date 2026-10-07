import { useState, type FormEvent } from 'react'
import { useNavigate } from 'react-router-dom'
import {
  presentApiError,
  presentValidationError,
  type ApiErrorPresentation,
} from '../api/apiErrorPresentation'
import { useAuth } from '../auth/auth.context'
import { getPasswordValidationMessage } from '../auth/passwordValidation'
import { getPortalPath } from '../auth/portalPath'
import { ApiErrorAlert } from '../components/ApiErrorAlert'

export function EmployeeLoginPage() {
  const navigate = useNavigate()
  const { loginEmployee } = useAuth()
  const [email, setEmail] = useState('')
  const [password, setPassword] = useState('')
  const [error, setError] = useState<ApiErrorPresentation | null>(null)
  const [isSubmitting, setIsSubmitting] = useState(false)

  async function handleSubmit(event: FormEvent<HTMLFormElement>) {
    event.preventDefault()
    setError(null)

    const normalizedEmail = email.trim()
    if (!normalizedEmail) {
      setError(presentValidationError('Please enter your email address.'))
      return
    }

    const passwordError = getPasswordValidationMessage(password)
    if (passwordError) {
      setError(presentValidationError(passwordError))
      return
    }

    setIsSubmitting(true)

    try {
      const user = await loginEmployee({ email: normalizedEmail, password })
      navigate(getPortalPath(user.role), { replace: true })
    } catch (caughtError) {
      setError(presentApiError(caughtError))
    } finally {
      setIsSubmitting(false)
    }
  }

  return (
    <main className="page-container">
      <section className="auth-card">
        <h1>Employee Sign In</h1>
        <p className="muted">For Staff, Manager, Business Operations and Administrator accounts.</p>
        <form className="form-grid" onSubmit={handleSubmit}>
          <div className="form-field">
            <label htmlFor="employeeEmail">Email</label>
            <input
              id="employeeEmail"
              type="email"
              autoComplete="email"
              maxLength={254}
              required
              value={email}
              onChange={(event) => setEmail(event.target.value)}
            />
          </div>
          <div className="form-field">
            <label htmlFor="employeePassword">Password</label>
            <input
              id="employeePassword"
              type="password"
              autoComplete="current-password"
              minLength={8}
              maxLength={64}
              required
              value={password}
              onChange={(event) => setPassword(event.target.value)}
            />
          </div>
          <ApiErrorAlert error={error} />
          <button className="button" type="submit" disabled={isSubmitting}>
            {isSubmitting ? 'Signing in...' : 'Sign In'}
          </button>
        </form>
      </section>
    </main>
  )
}
