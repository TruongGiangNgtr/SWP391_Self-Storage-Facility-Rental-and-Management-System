import { Link, Outlet, useLocation } from 'react-router-dom'
import { useAuth } from '../auth/auth.context'
import { getPortalPath } from '../auth/portalPath'
import { CustomerLayout } from './CustomerLayout'

export function RootLayout() {
  const { user, logout } = useAuth()
  const location = useLocation()

  if (location.pathname === '/customer' || location.pathname.startsWith('/customer/')) {
    return <CustomerLayout><Outlet key={`${location.pathname}${location.search}`} /></CustomerLayout>
  }

  return (
    <div className="app-shell">
      <header className="app-header">
        <Link className="brand" to="/">
          FRMS
        </Link>
        <nav className="app-nav" aria-label="Main navigation">
          {user ? (
            <>
              <Link to={getPortalPath(user.role)}>My Portal</Link>
              <button className="link-button" type="button" onClick={logout}>
                Sign Out
              </button>
            </>
          ) : (
            <>
              <Link to="/auth/customer/login">Customer</Link>
              <Link to="/auth/employee/login">Employees</Link>
            </>
          )}
        </nav>
      </header>
      <Outlet />
    </div>
  )
}
