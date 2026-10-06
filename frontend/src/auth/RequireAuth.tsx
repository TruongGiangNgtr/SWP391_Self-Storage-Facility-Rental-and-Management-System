import { Navigate, Outlet, useLocation } from 'react-router-dom'
import { useAuth } from './auth.context'

const EMPLOYEE_PORTAL_PREFIXES = ['/staff', '/manager', '/business', '/admin']

export function RequireAuth() {
  const { isAuthenticated, isInitializing } = useAuth()
  const location = useLocation()

  if (isInitializing) {
    return <main className="page-container">Restoring your session...</main>
  }

  if (!isAuthenticated) {
    const loginPath = EMPLOYEE_PORTAL_PREFIXES.some((prefix) =>
      location.pathname.startsWith(prefix),
    )
      ? '/auth/employee/login'
      : '/auth/customer/login'

    return <Navigate to={loginPath} replace state={{ from: location }} />
  }

  return <Outlet />
}
