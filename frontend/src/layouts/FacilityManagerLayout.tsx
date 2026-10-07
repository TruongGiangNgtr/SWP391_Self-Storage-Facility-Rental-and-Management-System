import {
  ChartNoAxesCombined,
  ClipboardCheck,
  ClipboardList,
  Grid2X2,
  LogOut,
  UserCheck,
  Warehouse,
} from 'lucide-react'
import {
  Link,
  NavLink,
  Outlet,
} from 'react-router-dom'
import { useAuth } from '../auth/auth.context'
import { BrandLogo } from '../components/BrandLogo'
import '../styles/manager.css'

function getInitials(fullName?: string | null) {
  if (!fullName?.trim()) {
    return 'FM'
  }

  const parts = fullName
    .trim()
    .split(/\s+/)
    .filter(Boolean)

  return parts
    .slice(-2)
    .map((part) => part[0]?.toUpperCase() ?? '')
    .join('') || 'FM'
}

const navItems = [
  {
    to: '/manager/handover-unit-selection',
    label: 'Select Handover Unit',
    icon: Grid2X2,
  },
  {
    to: '/manager/physical-units',
    label: 'Manage Physical Unit',
    icon: Warehouse,
  },
  {
    to: '/manager/operations',
    label: 'Monitor Facility Operations',
    icon: ClipboardList,
  },
  {
    to: '/manager/returns-inspections',
    label: 'Returns & Inspections',
    icon: ClipboardCheck,
  },
  {
    to: '/manager/support-assignment',
    label: 'Assign Support Staff',
    icon: UserCheck,
  },
  {
    to: '/manager/reports',
    label: 'Reports',
    icon: ChartNoAxesCombined,
  },
] as const

export function FacilityManagerLayout() {
  const { user, logout } = useAuth()
  const displayName = user?.fullName?.trim() || 'Facility Manager'
  const initials = getInitials(user?.fullName)

  return (
    <div className="manager-shell">
      <header className="manager-header">
        <div className="manager-brand">
          <BrandLogo size="small" />
          <span className="manager-portal-badge">
            FACILITY MANAGER PORTAL
          </span>
        </div>

        <div className="manager-header-user">
          <div className="manager-header-avatar">
            {initials}
          </div>

          <div className="manager-header-user-text">
            <strong>{displayName}</strong>
            <span>{user?.email ?? 'Employee account'}</span>
          </div>
        </div>
      </header>

      <div className="manager-body">
        <aside className="manager-sidebar">
          <div className="manager-profile">
            <div className="manager-profile-avatar">
              {initials}
            </div>

            <strong>{displayName}</strong>

            <span className="manager-profile-email">
              {user?.email ?? 'Employee account'}
            </span>

            <span className="manager-role-badge">
              Facility Manager
            </span>
          </div>

          <nav
            className="manager-nav"
            aria-label="Facility Manager navigation"
          >
            {navItems.map((item) => {
              const Icon = item.icon

              return (
                <NavLink
                  key={item.to}
                  to={item.to}
                  className={({ isActive }) =>
                    isActive
                      ? 'manager-nav-link manager-nav-link-active'
                      : 'manager-nav-link'
                  }
                >
                  <Icon size={18} />
                  <span>{item.label}</span>
                </NavLink>
              )
            })}
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

        <main className="manager-main">
          <Outlet />
        </main>
      </div>

      <footer className="manager-footer">
        <div className="manager-footer-main">
          <div className="manager-footer-brand">
            <BrandLogo inverse />
            <p>
              Facility-level storage operations, handover coordination,
              inspections, support assignment, and reporting for FStoRent.
            </p>
            <span className="manager-footer-trust">
              Secure · Facility-scoped · Role-based access
            </span>
          </div>

          <div className="manager-footer-column">
            <strong>OPERATIONS</strong>
            <Link
              className="manager-footer-link"
              to="/manager/handover-unit-selection"
            >
              Handover Unit Selection
            </Link>
            <Link
              className="manager-footer-link"
              to="/manager/physical-units"
            >
              Physical Unit Management
            </Link>
            <Link
              className="manager-footer-link"
              to="/manager/operations"
            >
              Facility Monitoring
            </Link>
          </div>

          <div className="manager-footer-column">
            <strong>WORKFLOWS</strong>
            <Link
              className="manager-footer-link"
              to="/manager/returns-inspections"
            >
              Returns &amp; Inspections
            </Link>
            <Link
              className="manager-footer-link"
              to="/manager/support-assignment"
            >
              Support Assignment
            </Link>
            <Link
              className="manager-footer-link"
              to="/manager/reports"
            >
              Facility Reports
            </Link>
          </div>

          <div className="manager-footer-column">
            <strong>ACCOUNT</strong>
            <span>{displayName}</span>
            <span>Facility Manager</span>
            <button
              type="button"
              className="manager-footer-logout"
              onClick={logout}
            >
              Sign Out
            </button>
          </div>
        </div>

        <div className="manager-footer-bottom">
          <span>
            © 2026 FStoRent. All rights reserved.
          </span>

          <div>
            <span>Privacy Policy</span>
            <span>Terms of Service</span>
            <Link
              className="manager-footer-bottom-link"
              to="/manager/handover-unit-selection"
            >
              Facility Manager Portal
            </Link>
          </div>
        </div>
      </footer>
    </div>
  )
}
