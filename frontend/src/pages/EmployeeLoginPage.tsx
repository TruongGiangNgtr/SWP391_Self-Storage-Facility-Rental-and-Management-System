import { useState, type FormEvent } from 'react'
import { Link, useNavigate } from 'react-router-dom'
import { Mail } from 'lucide-react'
import {
  presentApiError,
  presentValidationError,
  type ApiErrorPresentation,
} from '../api/apiErrorPresentation'
import { useAuth } from '../auth/auth.context'
import { getPasswordValidationMessage } from '../auth/passwordValidation'
import { getPortalPath } from '../auth/portalPath'
import { ApiErrorAlert } from '../components/ApiErrorAlert'
import { CustomerAuthFrame } from '../components/CustomerAuthFrame'
import { CustomerPasswordField } from '../components/CustomerPasswordField'

export function EmployeeLoginPage() {
  const navigate = useNavigate()
  const { loginEmployee } = useAuth()
  const [email, setEmail] = useState('')
  const [error, setError] = useState<ApiErrorPresentation | null>(null)
  const [isSubmitting, setIsSubmitting] = useState(false)

  async function handleSubmit(event: FormEvent<HTMLFormElement>) {
    event.preventDefault()
    setError(null)

    const password = String(new FormData(event.currentTarget).get('password') ?? '')
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
    <CustomerAuthFrame employee>
        <form className="form-grid" onSubmit={handleSubmit} aria-busy={isSubmitting}>
          <div className="form-field">
            <label htmlFor="employeeEmail">Email</label>
            <div className="customer-auth-input">
            <Mail size={16} aria-hidden="true" />
            <input
              id="employeeEmail"
              name="email"
              type="email"
              autoComplete="email"
              maxLength={254}
              required
              placeholder="Enter your email address"
              value={email}
              onChange={(event) => setEmail(event.target.value)}
            />
            </div>
          </div>
          <CustomerPasswordField name="password" />
          <ApiErrorAlert error={error} />
          <button className="button" type="submit" disabled={isSubmitting}>
            {isSubmitting ? 'Signing in...' : 'Sign In'}
          </button>
          <p className="muted customer-auth-small">Employee accounts use an email address and password.</p>
          <Link className="auth-switch" to="/auth/customer/login">Customer Sign In</Link>
        </form>
    </CustomerAuthFrame>
  )
}
