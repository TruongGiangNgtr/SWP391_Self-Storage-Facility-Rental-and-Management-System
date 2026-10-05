import { useState, type FormEvent } from 'react'
import { Link, useNavigate } from 'react-router-dom'
import { authApi } from '../api/authApi'
import { ApiRequestError } from '../api/httpClient'

export function CustomerRegisterPage() {
  const navigate = useNavigate()
  const [error, setError] = useState<string | null>(null)
  const [isSubmitting, setIsSubmitting] = useState(false)

  async function handleSubmit(event: FormEvent<HTMLFormElement>) {
    event.preventDefault()
    setError(null)
    setIsSubmitting(true)
    const form = new FormData(event.currentTarget)

    try {
      await authApi.registerCustomer({
        fullName: String(form.get('fullName') ?? ''),
        phoneNumber: String(form.get('phoneNumber') ?? ''),
        email: String(form.get('email') ?? ''),
        password: String(form.get('password') ?? ''),
        address: String(form.get('address') ?? '') || null,
        cccd: String(form.get('cccd') ?? '') || null,
      })
      navigate('/auth/customer/login', { replace: true })
    } catch (caughtError) {
      setError(
        caughtError instanceof ApiRequestError
          ? caughtError.message
          : 'Không thể kết nối đến máy chủ.',
      )
    } finally {
      setIsSubmitting(false)
    }
  }

  return (
    <main className="page-container">
      <section className="auth-card">
        <h1>Đăng ký khách hàng</h1>
        <form className="form-grid" onSubmit={handleSubmit}>
          <div className="form-field">
            <label htmlFor="fullName">Họ và tên</label>
            <input id="fullName" name="fullName" autoComplete="name" required />
          </div>
          <div className="form-field">
            <label htmlFor="registerPhoneNumber">Số điện thoại</label>
            <input
              id="registerPhoneNumber"
              name="phoneNumber"
              autoComplete="tel"
              required
            />
          </div>
          <div className="form-field">
            <label htmlFor="registerEmail">Email</label>
            <input id="registerEmail" name="email" type="email" autoComplete="email" required />
          </div>
          <div className="form-field">
            <label htmlFor="registerPassword">Mật khẩu</label>
            <input
              id="registerPassword"
              name="password"
              type="password"
              autoComplete="new-password"
              minLength={8}
              maxLength={64}
              required
            />
          </div>
          <div className="form-field">
            <label htmlFor="address">Địa chỉ</label>
            <input id="address" name="address" autoComplete="street-address" />
          </div>
          <div className="form-field">
            <label htmlFor="cccd">CCCD</label>
            <input id="cccd" name="cccd" />
          </div>
          {error && <div className="form-error">{error}</div>}
          <button className="button" type="submit" disabled={isSubmitting}>
            {isSubmitting ? 'Đang đăng ký...' : 'Đăng ký'}
          </button>
          <Link to="/auth/customer/login">Đã có tài khoản? Đăng nhập</Link>
        </form>
      </section>
    </main>
  )
}
