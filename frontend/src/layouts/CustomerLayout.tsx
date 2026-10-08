import { useState, type ReactNode } from 'react'
import { Link, NavLink, useLocation } from 'react-router-dom'
import { useAuth } from '../auth/auth.context'
import { FlowIcon, type FlowIconName } from '../components/FlowIcon'
import '../styles/customer.css'

const NAV_ITEMS: { to: string; label: string; icon: FlowIconName; end?: boolean }[] = [
  { to: '/customer', label: 'Overview', icon: 'overview', end: true },
  { to: '/customer/visits', label: 'My Visits', icon: 'visits' },
  { to: '/customer/reservations', label: 'My Reservations', icon: 'reservations' },
  { to: '/customer/contracts', label: 'My Contracts', icon: 'unit' },
  { to: '/customer/invoices', label: 'Deposit & Invoices', icon: 'invoices' },
]

function Brand({ footer = false }: { footer?: boolean }) {
  return (
    <Link className={`flow-brand ${footer ? 'flow-brand-footer' : ''}`} to="/customer">
      <span className="flow-brand-mark"><FlowIcon name={footer ? 'logo-footer' : 'logo'} /></span>
      <span>FSto<span className="brand-accent">Rent</span></span>
    </Link>
  )
}

export function CustomerLayout({ children }: { children: ReactNode }) {
  const [copyrightYear] = useState(() => new Date().getFullYear())
  const { user, logout } = useAuth()
  const { pathname } = useLocation()
  const withSidebar = !pathname.startsWith('/customer/storage-search') &&
    pathname !== '/customer/reservations/new' &&
    pathname !== '/customer/payments/result'
  const name = user?.fullName || 'Customer'
  const initials = name.split(/\s+/).filter(Boolean).slice(-2).map((word) => word[0]).join('')

  return (
    <div className={`frms-customer ${withSidebar ? 'customer-portal' : 'customer-catalog'}`}>
      <header className="flow-header">
        <div className="flow-header-inner">
          <Brand />
          <nav className="flow-top-nav" aria-label="Customer navigation">
            <NavLink to="/customer/storage-search">Find Storage</NavLink>
            <NavLink to="/customer/reservations">My Reservations</NavLink>
            <NavLink to="/customer/visits">Visits</NavLink>
          </nav>
          <Link className="flow-account" to="/customer">
            <span className="flow-avatar">{initials}</span>
            <span><strong>{name}</strong><small>Customer</small></span>
          </Link>
        </div>
      </header>

      <div className={`customer-body ${withSidebar ? 'customer-body-sidebar' : ''}`}>
        {withSidebar && (
          <aside className="customer-sidebar">
            <div className="sidebar-profile">
              <span className="flow-avatar">{initials}</span>
              <strong>{name}</strong>
              {user?.email && <small>{user.email}</small>}
              <span className="profile-role">Customer</span>
            </div>
            <nav aria-label="Customer Portal">
              {NAV_ITEMS.map((item) => (
                <NavLink key={item.to} to={item.to} end={item.end}>
                  <FlowIcon name={item.icon} />{item.label}
                </NavLink>
              ))}
            </nav>
            <button className="sidebar-logout" type="button" onClick={logout}>
              <FlowIcon name="logout" />Sign Out
            </button>
          </aside>
        )}
        <div className="customer-content">{children}</div>
      </div>

      <footer className="flow-footer">
        <div className="flow-footer-inner">
          <div className="flow-footer-about">
            <Brand footer />
            <p>Find, reserve and manage your self-storage with FStoRent.</p>
          </div>
          <div><h2>Storage</h2><Link to="/customer/storage-search">Find Storage</Link><Link to="/customer/reservations">My Reservations</Link></div>
          <div><h2>Rental Management</h2><Link to="/customer/visits">Visits</Link><Link to="/customer/invoices">Deposit & Invoices</Link></div>
          <div><h2>Account</h2><Link to="/customer">Customer Portal</Link><button type="button" onClick={logout}>Sign Out</button></div>
          <p className="flow-copyright">© {copyrightYear} FStoRent.</p>
        </div>
      </footer>
    </div>
  )
}
