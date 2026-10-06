import { NavLink, Outlet } from 'react-router-dom'
import { useAuth } from '../auth/auth.context'
import { BrandLogo } from '../components/BrandLogo'
interface BusinessOperationsLayoutProps {
  basePath?: string
}

export function BusinessOperationsLayout({
  basePath = '/business',
}: BusinessOperationsLayoutProps) {
  const { user, logout } = useAuth()

  return (
  <div className="bo-shell">
    <header className="bo-header">
      <div className="bo-brand">
        <BrandLogo size="small" />

        <span className="bo-portal-badge">
          BUSINESS OPERATIONS PORTAL
        </span>
      </div>

      <div className="bo-header-user">
        <div className="bo-user-avatar">BO</div>

        <div className="bo-user-text">
          <strong>Business Operations Manager</strong>
          <span>
            {user?.role ?? 'Preview mode'}
          </span>
        </div>

        {user && (
          <button
            type="button"
            className="bo-logout-button"
            onClick={logout}
          >
            Sign Out
          </button>
        )}
      </div>
    </header>

    <div className="bo-body">
      <aside className="bo-sidebar">
        <div className="bo-profile-card">
          <div className="bo-profile-avatar">BO</div>

          <strong>Business Operations</strong>
          <span>Manager Portal</span>
        </div>

        <nav className="bo-nav">
          <NavLink
            to={basePath}
            end
            className={({ isActive }) =>
              isActive
                ? 'bo-nav-link bo-nav-link-active'
                : 'bo-nav-link'
            }
          >
            Overview
          </NavLink>

          <NavLink
            to={`${basePath}/facilities`}
            className={({ isActive }) =>
              isActive
                ? 'bo-nav-link bo-nav-link-active'
                : 'bo-nav-link'
            }
          >
            Facilities
          </NavLink>

          <NavLink
            to={`${basePath}/unit-types`}
            className="bo-nav-link"
          >
            Unit Types &amp; Pricing
          </NavLink>

          <NavLink
            to={`${basePath}/policies`}
            className="bo-nav-link"
          >
            Policies
          </NavLink>

          <NavLink
            to={`${basePath}/discounts`}
            className="bo-nav-link"
          >
            Discounts
          </NavLink>

          <NavLink
            to={`${basePath}/extra-fees`}
            className="bo-nav-link"
          >
            Extra Fees
          </NavLink>

          <NavLink
            to={`${basePath}/reports`}
            className="bo-nav-link"
          >
            Reports
          </NavLink>
        </nav>

        <div className="bo-sidebar-bottom">
          <span className="bo-nav-link">
            Account Settings
          </span>

          <button
            type="button"
            className="bo-signout-link"
            onClick={() => {
              if (user) {
                logout()
              }
            }}
          >
            Sign Out
          </button>
        </div>
      </aside>

      <main className="bo-main">
        <Outlet />
      </main>
    </div>

    <footer className="bo-footer">
      <div className="bo-footer__main">
        <div className="bo-footer__brand">
          <BrandLogo inverse />

          <p>
            Enterprise-grade self-storage facility management,
            real-time access, inventory, and operational
            intelligence.
          </p>

          <span className="bo-footer__trust">
            Secure · Reliable · Role-based access
          </span>
        </div>

        <div className="bo-footer__column">
          <strong>STORAGE</strong>
          <span>Facilities Overview</span>
          <span>Capacity Planning</span>
          <span>Unit Notes</span>
        </div>

        <div className="bo-footer__column">
          <strong>SUPPORT</strong>
          <span>Help Center</span>
          <span>Operations Guide</span>
          <span>Contact Support</span>
        </div>

        <div className="bo-footer__column">
          <strong>ACCOUNT</strong>
          <span>My Account</span>
          <span>System Settings</span>

          <button
            type="button"
            className="bo-footer__logout"
            onClick={() => {
              if (user) {
                logout()
              }
            }}
          >
            Sign Out
          </button>
        </div>
      </div>

      <div className="bo-footer__bottom">
        <span>
          © 2026 FStoRent. All rights reserved. Built with
          institutional-grade security.
        </span>

        <div>
          <span>Privacy Policy</span>
          <span>Terms of Service</span>
          <span>Build 2026.04.12</span>
        </div>
      </div>
    </footer>
  </div>
)
}