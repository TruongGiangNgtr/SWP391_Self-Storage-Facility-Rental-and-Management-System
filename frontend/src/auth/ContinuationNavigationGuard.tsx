import { useEffect } from 'react'
import { useLocation } from 'react-router-dom'
import { useAuth } from './auth.context'
import { clearCustomerContinuation, finishCustomerSignOutNavigation, isCustomerAuthPath, readCustomerContinuation } from './customerContinuation'

export function ContinuationNavigationGuard() {
  const { pathname, state } = useLocation()
  const { isAuthenticated } = useAuth()
  useEffect(() => {
    // RequireAuth is about to replace this route with auth + a fresh intent.
    if (pathname.startsWith('/customer') && !isAuthenticated) return
    if (!isCustomerAuthPath(pathname) || !readCustomerContinuation(state)) clearCustomerContinuation()
    if (isCustomerAuthPath(pathname)) finishCustomerSignOutNavigation()
  }, [pathname, state, isAuthenticated])
  return null
}
