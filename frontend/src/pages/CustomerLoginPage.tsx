import { useRef, useState, type FormEvent } from 'react'
import { Link, useLocation, useNavigate } from 'react-router-dom'
import { Phone } from 'lucide-react'
import { presentApiError, presentValidationError, type ApiErrorPresentation } from '../api/apiErrorPresentation'
import { useAuth } from '../auth/auth.context'
import { consumeCustomerContinuation, clearCustomerContinuation, readCustomerContinuation } from '../auth/customerContinuation'
import { getPasswordValidationMessage } from '../auth/passwordValidation'
import { ApiErrorAlert } from '../components/ApiErrorAlert'
import { CustomerAuthFrame } from '../components/CustomerAuthFrame'
import { CustomerPasswordField } from '../components/CustomerPasswordField'
import { CustomerContinuationNotice } from '../components/CustomerContinuationNotice'

export function CustomerLoginPage() {
  const navigate = useNavigate()
  const { state } = useLocation()
  const { loginCustomer, logout, isInitializing } = useAuth()
  const [error, setError] = useState<ApiErrorPresentation | null>(null)
  const [isSubmitting, setIsSubmitting] = useState(false)
  const busy = useRef(false)
  const pending = readCustomerContinuation(state)
  const authState = pending ? { continuationId: pending.id } : null

  async function handleSubmit(event: FormEvent<HTMLFormElement>) {
    event.preventDefault()
    if (busy.current || isInitializing) return
    const form = new FormData(event.currentTarget)
    const phoneNumber = String(form.get('phoneNumber') ?? '').trim()
    const password = String(form.get('password') ?? '')
    const invalid = !phoneNumber ? 'Please enter your phone number.' : getPasswordValidationMessage(password)
    if (invalid) { setError(presentValidationError(invalid)); return }
    busy.current = true; setIsSubmitting(true); setError(null)
    try {
      const account = await loginCustomer({ phoneNumber, password })
      if (account.role !== 'CUSTOMER') {
        clearCustomerContinuation(); logout(); navigate('/forbidden', { replace: true }); return
      }
      const destination = consumeCustomerContinuation(state)
      navigate(destination ?? '/customer', { replace: true, state: destination ? { authResumed: true } : null })
    } catch (caughtError) { setError(presentApiError(caughtError)) }
    finally { busy.current = false; setIsSubmitting(false) }
  }
  return <CustomerAuthFrame>
    {state && typeof state === 'object' && (state as { registrationComplete?: unknown }).registrationComplete === true &&
      <p className="notice" role="status">Your account was created. Sign in with your phone number and password.</p>}
    <CustomerContinuationNotice />
    <form className="form-grid" onSubmit={handleSubmit} noValidate aria-busy={isSubmitting}>
      <div className="form-field"><label htmlFor="customerPhoneNumber">Phone Number</label><div className="customer-auth-input"><Phone size={16} aria-hidden="true" /><input id="customerPhoneNumber" name="phoneNumber" type="tel" autoComplete="username" inputMode="tel" maxLength={30} required placeholder="Enter your phone number" /></div></div>
      <CustomerPasswordField name="password" />
      <ApiErrorAlert error={error} />
      <button className="button" type="submit" disabled={isSubmitting || isInitializing}>{isSubmitting ? 'Signing in…' : isInitializing ? 'Restoring your session…' : 'Sign In'}</button>
      <div className="customer-auth-divider"><span>New to FStoRent?</span></div>
      <Link className="button button-secondary" to="/auth/customer/register" state={authState}>Create Customer Account</Link>
      <p className="muted customer-auth-small">Customer accounts use a phone number and password.</p>
      <Link className="auth-switch" to="/auth/employee/login" onClick={clearCustomerContinuation}>Employee Sign In</Link>
    </form>
  </CustomerAuthFrame>
}
