import { Navigate, Outlet, useLocation } from 'react-router-dom'
import { useAuth } from './auth.context'

export function RequireAuth() {
  const { isAuthenticated, isInitializing } = useAuth()
  const location = useLocation()

  if (isInitializing) {
    return <main className="page-container">Đang khôi phục phiên đăng nhập...</main>
  }

  if (!isAuthenticated) {
    return <Navigate to="/auth/customer/login" replace state={{ from: location }} />
  }

  return <Outlet />
}
