import { useEffect, useRef, useState, type ReactNode } from 'react'
import { Link, NavLink, useLocation } from 'react-router-dom'
import { Bell, ChevronDown, Menu, Ticket } from 'lucide-react'
import { useAuth } from '../auth/auth.context'
import { FlowIcon, type FlowIconName } from '../components/FlowIcon'
import { FlowDialog } from '../components/FlowDialog'
import '../styles/customer.css'
import '../styles/customer-design.css'

const NAV_ITEMS: { to: string; label: string; icon: FlowIconName; end?: boolean }[] = [
  { to: '/customer', label: 'Overview', icon: 'overview', end: true },
  { to: '/customer/visits', label: 'My Visits', icon: 'visits' },
  { to: '/customer/reservations', label: 'My Reservations', icon: 'reservations' },
  { to: '/customer/contracts', label: 'My Contracts', icon: 'unit' },
  { to: '/customer/invoices', label: 'Deposit & Invoices', icon: 'invoices' },
  { to: '/customer/support-tickets', label: 'Support Tickets', icon: 'visits' },
]

function Brand({ footer = false, signedIn = true }: { footer?: boolean; signedIn?: boolean }) {
  return <Link className={`flow-brand ${footer ? 'flow-brand-footer' : ''}`} to={signedIn ? '/customer' : '/'} aria-label="FStoRent Home">
    <span className="flow-brand-mark"><FlowIcon name={footer ? 'logo-footer' : 'logo'} /></span>
    <span>FSto<span className="brand-accent">Rent</span></span>
  </Link>
}

function StorageNavigation({ signedIn }: { signedIn: boolean }) {
  return <>
    {signedIn ? <NavLink to="/customer/storage-search">Find Storage</NavLink> : <span aria-disabled="true" title="Sign in to search storage; public search will be added in a later task">Find Storage</span>}
    {['How It Works', 'Size Guide', 'Help'].map((label) => <span key={label} aria-disabled="true" title="Not available yet">{label}</span>)}
  </>
}

export function CustomerLayout({ children, publicPage = false }: { children: ReactNode; publicPage?: boolean }) {
  const { user, logout } = useAuth()
  const { pathname } = useLocation()
  const signedIn = user?.role === 'CUSTOMER'
  const [drawerOpen, setDrawerOpen] = useState(false)
  const accountMenu = useRef<HTMLDetailsElement>(null)
  const [copyrightYear] = useState(() => new Date().getFullYear())
  const withSidebar = !publicPage && !pathname.startsWith('/customer/storage-search') &&
    pathname !== '/customer/reservations/new' && pathname !== '/customer/payments/result'
  const name = user?.fullName || 'Customer'
  const initials = name.split(/\s+/).filter(Boolean).slice(-2).map((word) => word[0]).join('')
  const currentItem = [...NAV_ITEMS].reverse().find((item) => item.end ? pathname === item.to : pathname.startsWith(item.to))

  useEffect(() => {
    function closeMenu(event: MouseEvent | KeyboardEvent) {
      if (event instanceof KeyboardEvent) {
        if (event.key === 'Escape' && accountMenu.current?.open) {
          accountMenu.current.open = false
          accountMenu.current.querySelector('summary')?.focus()
        }
      } else if (event.target instanceof Node && !accountMenu.current?.contains(event.target) && accountMenu.current) {
        accountMenu.current.open = false
      }
    }
    document.addEventListener('click', closeMenu)
    document.addEventListener('keydown', closeMenu)
    return () => { document.removeEventListener('click', closeMenu); document.removeEventListener('keydown', closeMenu) }
  }, [])

  function portalNavigation(onNavigate?: () => void) {
    return <nav aria-label="Customer Portal">
      {NAV_ITEMS.map((item) => <NavLink key={item.to} to={item.to} end={item.end} onClick={onNavigate}>
        {item.label === 'Support Tickets' ? <Ticket size={18} aria-hidden="true" /> : <FlowIcon name={item.icon} />}{item.label}
      </NavLink>)}
      <button className="sidebar-logout" type="button" onClick={() => { onNavigate?.(); logout() }}><FlowIcon name="logout" />Sign Out</button>
    </nav>
  }

  return <div className={`frms-customer customer-shell ${withSidebar ? 'customer-portal' : 'customer-catalog'} ${publicPage ? 'customer-auth' : ''}`}>
    <a className="customer-skip-link" href="#customer-main">Skip to content</a>
    <header className="flow-header">
      <div className="flow-header-inner">
        <Brand signedIn={signedIn} />
        <nav className="flow-top-nav" aria-label="Storage navigation"><StorageNavigation signedIn={signedIn} /></nav>
        <div className="customer-header-actions">
          {signedIn ? <>
            <button className="customer-icon-button customer-notifications" type="button" disabled aria-label="Notifications — not available yet" title="Notifications are not available yet"><Bell size={20} /></button>
            <details className="customer-account-menu" ref={accountMenu}>
              <summary className="flow-account" aria-label={`Account: ${name}`}>
                <span className="flow-avatar">{initials}</span><span className="customer-account-name"><strong>{name}</strong><small>Customer</small></span><ChevronDown size={16} aria-hidden="true" />
              </summary>
              <nav aria-label="Account navigation" onClick={() => { if (accountMenu.current) accountMenu.current.open = false }}>
                <Link to="/customer">Overview</Link><Link to="/customer/contracts">My Contracts</Link><Link to="/customer/support-tickets">Support Tickets</Link><button type="button" onClick={logout}>Sign Out</button>
              </nav>
            </details>
          </> : <Link className="button button-secondary" to="/auth/customer/login">Sign In</Link>}
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
      <div className="flow-footer-about"><Brand footer signedIn={signedIn} /><p>Find, reserve and manage your self-storage with FStoRent.</p></div>
      <div><h2>Storage</h2>{signedIn && <Link to="/customer/storage-search">Find Storage</Link>}<span className="customer-footer-unavailable">Size Guide · Not available yet</span><span className="customer-footer-unavailable">How It Works · Not available yet</span></div>
      <div><h2>Support</h2>{signedIn && <Link to="/customer/support-tickets">Support Tickets</Link>}<span className="customer-footer-unavailable">Help Center · Not available yet</span></div>
      <div><h2>Account</h2>{signedIn ? <><Link to="/customer">My Account</Link><button type="button" onClick={logout}>Sign Out</button></> : <><Link to="/auth/customer/login">Sign In</Link><Link to="/auth/customer/register">Create Account</Link></>}</div>
      <p className="flow-copyright">© {copyrightYear} FStoRent. All rights reserved.</p>
    </div></footer>
    {drawerOpen && <FlowDialog title="Navigation" subtitle="Choose where you would like to go." category="FStoRent" onClose={() => setDrawerOpen(false)}>
      <div className="customer-drawer-nav"><nav aria-label="Storage navigation" onClick={() => setDrawerOpen(false)}><StorageNavigation signedIn={signedIn} /></nav>{signedIn ? portalNavigation(() => setDrawerOpen(false)) : <Link className="button" to="/auth/customer/register" onClick={() => setDrawerOpen(false)}>Create Account</Link>}</div>
    </FlowDialog>}
  </div>
}
