import { useState, type FormEvent } from 'react'
import { Link, useNavigate } from 'react-router-dom'
import {
  presentApiError,
  presentValidationError,
  type ApiErrorPresentation,
} from '../api/apiErrorPresentation'
import { useAuth } from '../auth/auth.context'
import { getPasswordValidationMessage } from '../auth/passwordValidation'
import { ApiErrorAlert } from '../components/ApiErrorAlert'

export function CustomerLoginPage() {
  const navigate = useNavigate()
  const { loginCustomer } = useAuth()
  const [phoneNumber, setPhoneNumber] = useState('')
  const [password, setPassword] = useState('')
  const [error, setError] = useState<ApiErrorPresentation | null>(null)
  const [isSubmitting, setIsSubmitting] = useState(false)

  async function handleSubmit(event: FormEvent<HTMLFormElement>) {
    event.preventDefault()
    setError(null)

    const normalizedPhoneNumber = phoneNumber.trim()
    if (!normalizedPhoneNumber) {
      setError(presentValidationError('Vui lòng nhập số điện thoại.'))
      return
    }

    const passwordError = getPasswordValidationMessage(password)
    if (passwordError) {
      setError(presentValidationError(passwordError))
      return
    }

    setIsSubmitting(true)

    try {
      const user = await loginCustomer({
        phoneNumber: normalizedPhoneNumber,
        password,
      })
      navigate(user.role === 'CUSTOMER' ? '/customer' : '/forbidden', { replace: true })
    } catch (caughtError) {
      setError(presentApiError(caughtError))
    } finally {
      setIsSubmitting(false)
    }
  }

  return (
    <main className="page-container">
      <section className="auth-card">
        <h1>Đăng nhập khách hàng</h1>
        <p className="muted">Sử dụng số điện thoại và mật khẩu của bạn.</p>
        <form className="form-grid" onSubmit={handleSubmit}>
          <div className="form-field">
            <label htmlFor="phoneNumber">Số điện thoại</label>
            <input
              id="phoneNumber"
              autoComplete="tel"
              inputMode="tel"
              maxLength={30}
              required
              value={phoneNumber}
              onChange={(event) => setPhoneNumber(event.target.value)}
            />
          </div>
          <div className="form-field">
            <label htmlFor="customerPassword">Mật khẩu</label>
            <input
              id="customerPassword"
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
            {isSubmitting ? 'Đang đăng nhập...' : 'Đăng nhập'}
          </button>
          <Link to="/auth/customer/register">Chưa có tài khoản? Đăng ký</Link>
        </form>
      </section>
    </main>
  )
}
