import { useEffect, useRef } from 'react'
import { Link, Outlet, useLocation, useNavigationType } from 'react-router-dom'
import { BrandLogo } from '../components/BrandLogo'
import { CustomerLayout } from './CustomerLayout'

export function RootLayout() {
  const location = useLocation()
  const navigationType = useNavigationType()
  const previousPath = useRef<string | null>(null)
  const publicPath = location.pathname === '/' || location.pathname === '/customer/storage-search' || location.pathname === '/facilities' || location.pathname.startsWith('/facilities/') ||
    ['/how-it-works', '/size-guide', '/help'].includes(location.pathname) || location.pathname.startsWith('/help/')

  useEffect(() => {
    const stayingInSizeGuide = previousPath.current === '/size-guide' && location.pathname === '/size-guide'
    previousPath.current = location.pathname
    // Size Guide moves between tabs/pages at its catalog heading, not the hero.
    if (publicPath && navigationType !== 'POP' && !location.hash && !stayingInSizeGuide) window.scrollTo({ top: 0, behavior: 'instant' })
  }, [publicPath, navigationType, location.pathname, location.search, location.hash])

  if (publicPath) {
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
          key={['/customer/notifications', '/customer/reservations', '/customer/visits', '/customer/contracts', '/customer/invoices'].includes(location.pathname) ? location.pathname : `${location.pathname}${location.search}`}
        />
      </CustomerLayout>
    )
  }

  return (
    <div className="app-shell">
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
