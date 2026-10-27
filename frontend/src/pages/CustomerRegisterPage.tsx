import { useRef, useState, type FormEvent } from 'react'
import { Link, useLocation, useNavigate } from 'react-router-dom'
import { presentApiError, presentValidationError, type ApiErrorPresentation } from '../api/apiErrorPresentation'
import { authApi } from '../api/authApi'
import { ApiRequestError } from '../api/httpClient'
import { CCCD_INPUT_PATTERN, cccdValidationMessage } from '../features/customer-profile/customerProfile'
import { readCustomerContinuation } from '../auth/customerContinuation'
import { getPasswordValidationMessage } from '../auth/passwordValidation'
import { isUuid } from '../features/handover/handoverValidation'
import { ApiErrorAlert } from '../components/ApiErrorAlert'
import { CustomerAuthFrame } from '../components/CustomerAuthFrame'
import { CustomerPasswordField } from '../components/CustomerPasswordField'
import { CustomerContinuationNotice } from '../components/CustomerContinuationNotice'
import { CustomerFieldError } from '../components/CustomerFieldError'

export function CustomerRegisterPage() {
  const navigate = useNavigate()
  const { state } = useLocation()
  const pending = readCustomerContinuation(state)
  const authState = pending ? { continuationId: pending.id } : null
  const [error, setError] = useState<ApiErrorPresentation | null>(null)
  const [isSubmitting, setIsSubmitting] = useState(false)
  const [fieldErrors, setFieldErrors] = useState<Record<string, string>>({})
  const busy = useRef(false)

  async function handleSubmit(event: FormEvent<HTMLFormElement>) {
    event.preventDefault()
    if (busy.current) return
    const form = new FormData(event.currentTarget)
    const password = String(form.get('password') ?? '')
    const fullName = String(form.get('fullName') ?? '').trim()
    const phoneNumber = String(form.get('phoneNumber') ?? '').trim()
    const email = String(form.get('email') ?? '').trim()
    const cccd = String(form.get('cccd') ?? '').trim()
    const nextErrors: Record<string, string> = {}
    if (!fullName) nextErrors.fullName = 'Enter your full name.'
    if (!phoneNumber) nextErrors.phoneNumber = 'Enter your phone number.'
    const emailInput = event.currentTarget.elements.namedItem('email') as HTMLInputElement
    if (!email || emailInput.validity.typeMismatch) nextErrors.email = 'Enter a valid email address.'
    const passwordError = getPasswordValidationMessage(password, true)
    if (passwordError) nextErrors.password = passwordError
    if (!form.get('confirmPassword')) nextErrors.confirmPassword = 'Re-enter your password.'
    else if (password !== form.get('confirmPassword')) nextErrors.confirmPassword = 'The passwords do not match.'
    const cccdError = cccdValidationMessage(cccd)
    if (cccdError) nextErrors.cccd = cccdError
    setFieldErrors(nextErrors)
    if (Object.keys(nextErrors).length) {
      setError(null)
      const input = event.currentTarget.elements.namedItem(Object.keys(nextErrors)[0])
      if (input instanceof HTMLElement) input.focus()
      return
    }
    busy.current = true; setIsSubmitting(true); setError(null)
    try {
      const response = await authApi.registerCustomer({
        fullName, phoneNumber, email, password,
        cccd: cccd || null,
      })
      if (!isUuid(response?.data?.userAccountId) || !isUuid(response?.data?.customerId)) {
        setError(presentValidationError('We could not confirm that your account was created. Try signing in or contact support before registering again.')); return
      }
      navigate('/auth/customer/login', { replace: true, state: { ...authState, registrationComplete: true } })
    } catch (caughtError) {
      setError(presentApiError(caughtError))
      if (caughtError instanceof ApiRequestError && caughtError.errors?.cccd?.length) setFieldErrors(current => ({ ...current, cccd: caughtError.errors!.cccd.join(' ') }))
    }
    finally { busy.current = false; setIsSubmitting(false) }
  }
  return <CustomerAuthFrame register>
    <CustomerContinuationNotice />
    <form className="form-grid" onSubmit={handleSubmit} noValidate aria-busy={isSubmitting} onInput={event => { if (event.target instanceof HTMLInputElement) { const name = event.target.name; setFieldErrors(current => ({ ...current, [name]: '' })) } }}>
      <div className="form-field"><label htmlFor="customerFullName">Full Name</label><input id="customerFullName" name="fullName" autoComplete="name" placeholder="Enter your full name" required aria-invalid={Boolean(fieldErrors.fullName)} aria-describedby={fieldErrors.fullName ? 'fullNameError' : undefined} /><CustomerFieldError id="fullNameError" message={fieldErrors.fullName} /></div>
      <div className="form-field"><label htmlFor="registerEmail">Email Address</label><input id="registerEmail" name="email" type="email" autoComplete="email" placeholder="you@example.com" required aria-invalid={Boolean(fieldErrors.email)} aria-describedby={fieldErrors.email ? 'emailError' : undefined} /><CustomerFieldError id="emailError" message={fieldErrors.email} /></div>
      <div className="form-field"><label htmlFor="registerPhoneNumber">Phone Number</label><input id="registerPhoneNumber" name="phoneNumber" type="tel" autoComplete="tel" placeholder="Enter your phone number" required aria-invalid={Boolean(fieldErrors.phoneNumber)} aria-describedby={fieldErrors.phoneNumber ? 'phoneError' : undefined} /><CustomerFieldError id="phoneError" message={fieldErrors.phoneNumber} /></div>
      <CustomerPasswordField name="password" autoComplete="new-password" describedBy="password-guidance" error={fieldErrors.password} />
      <p id="password-guidance" className="muted customer-auth-small">Use 8–64 characters. If a password with emoji or accented letters is too long, use fewer characters.</p>
      <CustomerPasswordField name="confirmPassword" label="Confirm Password" autoComplete="new-password" error={fieldErrors.confirmPassword} />
      <details className="customer-auth-optional"><summary>Additional profile information (optional)</summary><div className="form-grid"><div className="form-field"><label htmlFor="registerCccd">National ID / CCCD</label><input id="registerCccd" name="cccd" autoComplete="off" inputMode="numeric" pattern={CCCD_INPUT_PATTERN} maxLength={20} aria-invalid={Boolean(fieldErrors.cccd)} aria-describedby={`register-cccd-help${fieldErrors.cccd ? ' register-cccd-error' : ''}`} /><CustomerFieldError id="register-cccd-error" message={fieldErrors.cccd} /><small id="register-cccd-help">Optional. If provided, enter exactly 12 digits, including any leading zeroes.</small></div></div></details>
      <ApiErrorAlert error={error} />
      <button className="button" type="submit" disabled={isSubmitting}>{isSubmitting ? 'Creating your account…' : 'Create Customer Account'}</button>
      <div className="customer-auth-divider"><span>Already have an account?</span></div>
      <Link className="button button-secondary" to="/auth/customer/login" state={authState}>Sign In</Link>
      <p className="muted customer-auth-small">Registration does not reserve storage or start a payment.</p>
    </form>
  </CustomerAuthFrame>
}
