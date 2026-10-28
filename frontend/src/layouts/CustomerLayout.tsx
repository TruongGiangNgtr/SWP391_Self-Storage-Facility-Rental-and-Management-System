import { useState, type ReactNode } from 'react'
import { Link, NavLink, useLocation, useNavigate } from 'react-router-dom'
import { Bell, Menu } from 'lucide-react'
import { useAuth } from '../auth/auth.context'
import { FlowIcon } from '../components/FlowIcon'
import { FlowDialog } from '../components/FlowDialog'
import { CUSTOMER_NAV_ITEMS, CUSTOMER_NOTIFICATIONS, getCustomerNavigationItem } from '../features/customer-navigation/customerNavigation'
import '../styles/customer.css'
import '../styles/customer-design.css'
import '../styles/public.css'
import '../styles/customer-auth.css'
import '../styles/support-pages.css'
import '../styles/rental-flow.css'
import '../styles/finance-flow.css'

function Brand({ footer = false }: { footer?: boolean }) {
  return <Link className={`flow-brand ${footer ? 'flow-brand-footer' : ''}`} to="/" aria-label="FStoRent Home">
    <span className="flow-brand-mark"><FlowIcon name="logo" /></span>
    <span>FSto<span className="brand-accent">Rent</span></span>
  </Link>
}

function StorageNavigation() {
  return <><NavLink to="/facilities">Find Storage</NavLink><NavLink to="/how-it-works">How It Works</NavLink><NavLink to="/size-guide">Size Guide</NavLink><NavLink to="/help">Help</NavLink></>
}

export function CustomerLayout({ children, publicPage = false }: { children: ReactNode; publicPage?: boolean }) {
  const { user, logout: endSession } = useAuth()
  const navigate = useNavigate()
  function logout() {
    endSession()
    // Explicit sign-out is abandonment, not a newly interrupted operation.
    navigate('/auth/customer/login', { replace: true, state: null })
  }
  const { pathname } = useLocation()
  const supportSurface = ['/', '/how-it-works', '/size-guide', '/help', '/customer', '/customer/account-settings', '/customer/notifications'].includes(pathname) || pathname.startsWith('/help/') || pathname.startsWith('/auth/customer/')
  const rentalSurface = pathname === '/customer/contracts' || /^\/customer\/contracts\/[^/]+(?:\/access-visits\/new)?$/.test(pathname) || pathname === '/customer/visits' || /^\/customer\/visits\/[^/]+$/.test(pathname)
  const financeSurface = pathname === '/customer/invoices' || /^\/customer\/invoices\/[^/]+$/.test(pathname)
  const signedIn = user?.role === 'CUSTOMER'
  const [drawerOpen, setDrawerOpen] = useState(false)
  const [copyrightYear] = useState(() => new Date().getFullYear())
  const withSidebar = !publicPage && !pathname.startsWith('/customer/storage-search') &&
    pathname !== '/customer/reservations/new' && pathname !== '/customer/payments/result'
  const name = user?.fullName || 'Customer'
  const initials = name.split(/\s+/).filter(Boolean).slice(-2).map((word) => word[0]).join('')
  const currentItem = getCustomerNavigationItem(pathname)


  function portalNavigation(onNavigate?: () => void) {
    return <nav aria-label="Customer Portal">
      {CUSTOMER_NAV_ITEMS.map((item) => <NavLink key={item.to} to={item.to} end={item.end} onClick={onNavigate}>
        <item.icon size={18} aria-hidden="true" /><span className="customer-nav-label"><span>{item.label}</span>{item.unavailable && <small>Not available yet</small>}</span>
      </NavLink>)}
      <button className="sidebar-logout" type="button" onClick={() => { onNavigate?.(); logout() }}><FlowIcon name="logout" />Sign Out</button>
    </nav>
  }

  return <div className={`frms-customer customer-shell ${supportSurface || rentalSurface || financeSurface ? 'support-surface' : ''} ${rentalSurface ? 'rental-surface' : ''} ${financeSurface ? 'finance-surface' : ''} ${withSidebar ? 'customer-portal' : 'customer-catalog'} ${publicPage ? pathname.startsWith('/auth/customer/') ? 'customer-auth' : 'customer-public' : ''}`}>
    <a className="customer-skip-link" href="#customer-main">Skip to content</a>
    <header className="flow-header">
      <div className="flow-header-inner">
        <Brand />
        <nav className="flow-top-nav" aria-label="Storage navigation"><StorageNavigation /></nav>
        <div className="customer-header-actions">
          {signedIn ? <>
            <NavLink className="customer-icon-button customer-notifications" to={CUSTOMER_NOTIFICATIONS.to} aria-label="Notifications" title="Notifications & Transaction History"><Bell size={20} aria-hidden="true" /></NavLink>
            <Link className="flow-account" to="/customer" aria-label={`Customer dashboard: ${name}`} title="Customer dashboard"><span className="flow-avatar">{initials}</span><span className="customer-account-name"><strong>{name}</strong><small>Customer</small></span></Link>
          </> : <Link className="button" to="/auth/customer/login">Sign In</Link>}
          <button className="customer-icon-button customer-menu-trigger" type="button" onClick={() => setDrawerOpen(true)} aria-label="Open navigation" aria-haspopup="dialog"><Menu size={22} /></button>
        </div>
      </div>
    </header>
    <div className={`customer-body ${withSidebar ? 'customer-body-sidebar' : ''}`}>
      {withSidebar && <aside className="customer-sidebar">
        <div className="sidebar-profile"><span className="flow-avatar">{initials}</span><strong>{name}</strong>{user?.email && <small>{user.email}</small>}<span className="profile-role">Customer</span></div>
        {portalNavigation()}
      </aside>}
      <div className="customer-content" id="customer-main" tabIndex={-1}>
        {withSidebar && <nav className="flow-breadcrumb" aria-label="Breadcrumb">
          <Link to="/customer">Customer Portal</Link><span aria-hidden="true">/</span>
          {currentItem && pathname !== currentItem.to ? <><Link to={currentItem.to}>{currentItem.label}</Link><span aria-hidden="true">/</span><span aria-current="page">Details & Actions</span></> : <span aria-current="page">{currentItem?.label ?? 'Overview'}</span>}
        </nav>}
        {children}
      </div>
    </div>
    <footer className="flow-footer"><div className="flow-footer-inner">
      <div className="flow-footer-about"><Brand footer /><p>Find, reserve and manage your self-storage with FStoRent.</p></div>
      <div><h2>Storage</h2><Link to="/facilities">Find Storage</Link><Link to="/size-guide">Size Guide</Link><Link to="/how-it-works">How It Works</Link></div>
      <div><h2>Support</h2>{signedIn && <Link to="/customer/support-tickets">Support Tickets</Link>}<Link to="/help">Help Center</Link><Link to="/facilities">Facility Contacts</Link></div>
      <div><h2>Account</h2>{signedIn ? <><Link to="/customer/account-settings">My Account</Link><button type="button" onClick={logout}>Sign Out</button></> : <><Link to="/auth/customer/login">Sign In</Link><Link to="/auth/customer/register">Create Account</Link><Link to="/auth/employee/login">Employee Sign In</Link></>}</div>
      <p className="flow-copyright">© {copyrightYear} FStoRent. All rights reserved.</p>
    </div></footer>
    {drawerOpen && <FlowDialog trapFocus title="Navigation" subtitle="Choose where you would like to go." category="FStoRent" onClose={() => setDrawerOpen(false)}>
      <div className="customer-drawer-nav"><nav aria-label="Storage navigation" onClick={() => setDrawerOpen(false)}><StorageNavigation /></nav>{signedIn && withSidebar ? portalNavigation(() => setDrawerOpen(false)) : signedIn ? <Link className="button button-secondary" to="/customer" onClick={() => setDrawerOpen(false)}>Customer Portal</Link> : <Link className="button" to="/auth/customer/register" onClick={() => setDrawerOpen(false)}>Create Account</Link>}</div>
    </FlowDialog>}
  </div>
}
