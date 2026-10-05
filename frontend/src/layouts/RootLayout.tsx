import { Link, Outlet } from 'react-router-dom'
import { useAuth } from '../auth/auth.context'
import { getPortalPath } from '../auth/portalPath'

export function RootLayout() {
  const { user, logout } = useAuth()

  return (
    <div className="app-shell">
      <header className="app-header">
        <Link className="brand" to="/">
          FRMS
        </Link>
        <nav className="app-nav" aria-label="Điều hướng chính">
          {user ? (
            <>
              <Link to={getPortalPath(user.role)}>Cổng làm việc</Link>
              <button className="link-button" type="button" onClick={logout}>
                Đăng xuất
              </button>
            </>
          ) : (
            <>
              <Link to="/auth/customer/login">Khách hàng</Link>
              <Link to="/auth/employee/login">Nhân viên</Link>
            </>
          )}
        </nav>
      </header>
      <Outlet />
    </div>
  )
}
