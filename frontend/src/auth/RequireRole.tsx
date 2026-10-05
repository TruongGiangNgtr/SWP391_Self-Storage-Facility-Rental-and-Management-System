import { Navigate, Outlet } from 'react-router-dom'
import { useAuth } from './auth.context'
import type { UserRole } from './auth.types'

export function RequireRole({ allowedRoles }: { allowedRoles: UserRole[] }) {
  const { user } = useAuth()

  if (!user || !allowedRoles.includes(user.role)) {
    return <Navigate to="/forbidden" replace />
  }

  return <Outlet />
}
