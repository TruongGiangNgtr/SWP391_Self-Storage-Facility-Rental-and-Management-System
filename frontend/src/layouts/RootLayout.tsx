import { Link, Outlet, useLocation } from 'react-router-dom'
import { BrandLogo } from '../components/BrandLogo'
import { CustomerLayout } from './CustomerLayout'

export function RootLayout() {
  const location = useLocation()

  if (
    location.pathname === '/customer' ||
    location.pathname.startsWith('/customer/')
  ) {
    return (
      <CustomerLayout>
        <Outlet
          key={`${location.pathname}${location.search}`}
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