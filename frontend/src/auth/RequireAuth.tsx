import { useEffect } from 'react'
import { Outlet, useLocation, useNavigate } from 'react-router-dom'
import { useAuth } from './auth.context'
import { beginCustomerContinuation } from './customerContinuation'

const EMPLOYEE_PORTAL_PREFIXES = ['/staff', '/manager', '/business', '/admin']

function SignInRedirect() {
  const location = useLocation()
  const navigate = useNavigate()
  useEffect(() => {
    const employee = EMPLOYEE_PORTAL_PREFIXES.some(prefix => location.pathname === prefix || location.pathname.startsWith(`${prefix}/`))
    const state = employee ? null : beginCustomerContinuation(`${location.pathname}${location.search}`)
    navigate(employee ? '/auth/employee/login' : '/auth/customer/login', { replace: true, state })
  }, [location.pathname, location.search, navigate])
  return <main className="page-container" role="status">Opening sign in…</main>
}

export function RequireAuth() {
  const { isAuthenticated, isInitializing } = useAuth()

  if (isInitializing) {
    return <main className="page-container">Restoring your session...</main>
  }

  if (!isAuthenticated) {
    return <SignInRedirect />
  }

  return <Outlet />
}
