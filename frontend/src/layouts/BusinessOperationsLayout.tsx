import { EmployeePageTransition } from '../components/EmployeePageTransition'
import { Link, NavLink, useLocation } from 'react-router-dom'
import {
  AtSign,
  Building2,
  ChartNoAxesCombined,
  Activity,
  LogOut,
  Percent,
  Tag,
} from 'lucide-react'
import { useAuth } from '../auth/auth.context'
import { BrandLogo } from '../components/BrandLogo'
import '../styles/manager.css'
import '../styles/business-sidebar.css'
import '../styles/employeeWorkspace.css'
interface BusinessOperationsLayoutProps {
  basePath?: string
}

export function BusinessOperationsLayout({
  basePath = '/business',
}: BusinessOperationsLayoutProps) {
  const { user, logout } = useAuth()
  const { pathname } = useLocation()
  const displayName = user?.fullName?.trim() || 'Business Operations Manager'
  const initials = user?.fullName?.trim().split(/\s+/).slice(-2)
    .map(part => part[0]?.toUpperCase() ?? '').join('') || 'BO'

  return (
  <div className="bo-shell employee-identity employee-workspace">
      <a className="portal-skip-link" href="#business-main">Skip to content</a>
    <header className="bo-header">
      <div className="bo-brand">
        <BrandLogo size="small" />

        <span className="bo-portal-badge">
          BUSINESS OPERATIONS PORTAL
        </span>
      </div>

      <div className="bo-header-user">
        <div className="bo-user-avatar">{initials}</div>

        <div className="bo-user-text">
          <strong>{displayName}</strong>
          <span>
            Business Operations Manager
          </span>
        </div>


      </div>
    </header>

    <div className="bo-body bo-body--employee-sidebar">
      <aside className="manager-sidebar">
        <div className="manager-profile">
          <div className="manager-profile-avatar">{initials}</div>

          <strong title={displayName}>{displayName}</strong>

          <span className="manager-profile-email">
            {user?.email ?? 'Employee account'}
          </span>
          <span className="manager-role-badge">Business Operations Manager</span>
        </div>

        <nav className="manager-nav" aria-label="Business Operations Manager navigation">
          <NavLink
            to={`${basePath}/facilities`}
            className={({ isActive }) =>
              isActive
                ? 'manager-nav-link manager-nav-link-active'
                : 'manager-nav-link'
            }
          >
            <Building2 size={18} />
            <span>Facilities</span>
          </NavLink>

          <NavLink
            to={`${basePath}/monitoring`}
            className={({ isActive }) =>
              isActive
                ? 'manager-nav-link manager-nav-link-active'
                : 'manager-nav-link'
            }
          >
            <Activity size={18} />
            <span>Multi-Facility Monitoring</span>
          </NavLink>

          <NavLink
            to={`${basePath}/unit-types`}
            className={({ isActive }) =>
              isActive
                ? 'manager-nav-link manager-nav-link-active'
                : 'manager-nav-link'
            }
          >
            <Tag size={18} />
            <span>Unit Types &amp; Pricing</span>
          </NavLink>

          <NavLink
            to={`${basePath}/policies`}
            className={({ isActive }) =>
              isActive
                ? 'manager-nav-link manager-nav-link-active'
                : 'manager-nav-link'
            }
          >
            <AtSign size={18} />
            <span>Policies</span>
          </NavLink>

          <NavLink
            to={`${basePath}/discounts`}
            className={({ isActive }) =>
              (isActive || pathname === `${basePath}/extra-fees`)
                ? 'manager-nav-link manager-nav-link-active'
                : 'manager-nav-link'
            }
          >
            <Percent size={18} />
            <span>Discount &amp; Extra Fee</span>
          </NavLink>

          <NavLink
            to={`${basePath}/reports`}
            className={({ isActive }) =>
              isActive
                ? 'manager-nav-link manager-nav-link-active'
                : 'manager-nav-link'
            }
          >
            <ChartNoAxesCombined size={18} />
            <span>Reports</span>
          </NavLink>
        </nav>

        <div className="manager-sidebar-actions">
          <button
            type="button"
            className="manager-signout"
            onClick={logout}
          >
            <LogOut size={18} />
            <span>Sign Out</span>
          </button>
        </div>
      </aside>

      <main className="bo-main" id="business-main" tabIndex={-1}>
        <EmployeePageTransition />
      </main>
    </div>

    <footer className="bo-footer">
      <div className="bo-footer__main">
        <div className="bo-footer__brand">
          <BrandLogo inverse />

          <p>
            Manage facilities, unit types and pricing, policies,
            discounts, extra fees, and business reports.
          </p>
        </div>

        <nav className="bo-footer__column" aria-label="Business operations shortcuts">
          <strong>OPERATIONS</strong>
          <Link to={`${basePath}/facilities`}>Facilities</Link>
          <Link to={`${basePath}/monitoring`}>Multi-Facility Monitoring</Link>
          <Link to={`${basePath}/reports`}>Reports</Link>
        </nav>

        <nav className="bo-footer__column" aria-label="Business configuration shortcuts">
          <strong>CONFIGURATION</strong>
          <Link to={`${basePath}/unit-types`}>Unit Types &amp; Pricing</Link>
          <Link to={`${basePath}/policies`}>Policies</Link>
          <Link to={`${basePath}/discounts`}>Discount &amp; Extra Fee</Link>
        </nav>

        <div className="bo-footer__column">
          <strong>ACCOUNT</strong>


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
          © 2026 FStoRent. All rights reserved.
        </span>

        <span>Business Operations Manager workspace</span>
      </div>
    </footer>
  </div>
)
}
