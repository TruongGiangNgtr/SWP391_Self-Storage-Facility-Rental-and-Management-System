import { useState, type FormEvent } from 'react'
import { useNavigate } from 'react-router-dom'
import { ApiRequestError } from '../api/httpClient'
import { useAuth } from '../auth/auth.context'
import { getPortalPath } from '../auth/portalPath'

export function EmployeeLoginPage() {
  const navigate = useNavigate()
  const { loginEmployee } = useAuth()
  const [email, setEmail] = useState('')
  const [password, setPassword] = useState('')
  const [error, setError] = useState<string | null>(null)
  const [isSubmitting, setIsSubmitting] = useState(false)

  async function handleSubmit(event: FormEvent<HTMLFormElement>) {
    event.preventDefault()
    setError(null)
    setIsSubmitting(true)

    try {
      const user = await loginEmployee({ email, password })
      navigate(getPortalPath(user.role), { replace: true })
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
        <h1>Đăng nhập nhân viên</h1>
        <p className="muted">Dành cho Staff, Manager, Business và Administrator.</p>
        <form className="form-grid" onSubmit={handleSubmit}>
          <div className="form-field">
            <label htmlFor="employeeEmail">Email</label>
            <input
              id="employeeEmail"
              type="email"
              autoComplete="email"
              required
              value={email}
              onChange={(event) => setEmail(event.target.value)}
            />
          </div>
          <div className="form-field">
            <label htmlFor="employeePassword">Mật khẩu</label>
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
          {error && <div className="form-error">{error}</div>}
          <button className="button" type="submit" disabled={isSubmitting}>
            {isSubmitting ? 'Đang đăng nhập...' : 'Đăng nhập'}
          </button>
        </form>
      </section>
    </main>
  )
}
