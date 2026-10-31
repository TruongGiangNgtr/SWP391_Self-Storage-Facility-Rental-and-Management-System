import { useAuth } from '../auth/auth.context'
import { useEffect, useLayoutEffect, useRef } from 'react'
import { Link, Outlet, useLocation } from 'react-router-dom'
import { BrandLogo } from '../components/BrandLogo'
import { CustomerLayout } from './CustomerLayout'

export function RootLayout() {
  const { user } = useAuth()
  const location = useLocation()
  const employeeSurface = Boolean(user && user.role !== 'CUSTOMER') || /^\/(?:staff|manager|business|admin|auth\/employee)(?:\/|$)/.test(location.pathname)
  const previousPage = useRef<{ pathname: string; hash: string } | null>(null)
  const customerErrorSurface = (!user || user.role === 'CUSTOMER') && ['/404', '/forbidden'].includes(location.pathname)
  const publicPath = location.pathname === '/' || location.pathname === '/customer/storage-search' || location.pathname === '/facilities' || location.pathname.startsWith('/facilities/') ||
    ['/how-it-works', '/size-guide', '/help'].includes(location.pathname) || location.pathname.startsWith('/help/')

  const customerPage = publicPath || customerErrorSurface || location.pathname === '/customer' || location.pathname.startsWith('/customer/') || location.pathname.startsWith('/auth/customer/')

  useEffect(() => {
    if (!customerPage) return
    const previous = window.history.scrollRestoration
    window.history.scrollRestoration = 'manual'
    return () => { window.history.scrollRestoration = previous }
  }, [customerPage])

  useLayoutEffect(() => {
    const previous = previousPage.current
    previousPage.current = { pathname: location.pathname, hash: location.hash }
    // New Guest/Customer pages start at the top, including after sign-in and
    // Back/Forward. Same-page filters/tabs and dialog backgrounds keep position;
    // explicit anchors continue to use their page's existing scroll behavior.
    if (customerPage && !location.hash && (previous?.pathname !== location.pathname || previous.hash)) {
      window.scrollTo({ top: 0, left: 0, behavior: 'instant' })
    }
  }, [customerPage, location.pathname, location.hash])

  if (publicPath || customerErrorSurface) {
    return <CustomerLayout publicPage><Outlet /></CustomerLayout>
  }

  if (location.pathname.startsWith('/auth/customer/') || location.pathname === '/auth/employee/login') {
    return <CustomerLayout publicPage><Outlet /></CustomerLayout>
  }

  if (
    location.pathname === '/customer' ||
    location.pathname.startsWith('/customer/')
  ) {
    return (
      <CustomerLayout>
        <Outlet
          key={['/customer/notifications', '/customer/reservations', '/customer/visits', '/customer/contracts', '/customer/invoices', '/customer/support-tickets'].includes(location.pathname) ? location.pathname : `${location.pathname}${location.search}`}
        />
      </CustomerLayout>
    )
  }

  return (
    <div className={`app-shell ${employeeSurface ? '' : 'support-app'}`}>
      <header className="app-header">
        <Link
          className="app-brand-link"
          to="/"
          aria-label="FStoRent Home"
        >
          <BrandLogo size="small" />
        </Link>

        <nav
          className="app-nav"
          aria-label="Main navigation"
        >
          <Link to="/auth/customer/login">
            Customer
          </Link>

          <Link to="/auth/employee/login">
            Employees
          </Link>
        </nav>
      </header>

      <Outlet />
    </div>
  )
}
