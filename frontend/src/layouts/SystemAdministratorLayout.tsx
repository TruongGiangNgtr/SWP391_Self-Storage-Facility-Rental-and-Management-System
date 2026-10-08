import {
  ClipboardList,
  FileText,
  History,
  LogOut,
  ShieldCheck,
  UserCheck,
  Users,
} from 'lucide-react'
import {
  Link,
  NavLink,
  Outlet,
} from 'react-router-dom'
import { useAuth } from '../auth/auth.context'
import { BrandLogo } from '../components/BrandLogo'
import '../styles/admin.css'

function getInitials(fullName?: string | null) {
  if (!fullName?.trim()) {
    return 'SA'
  }

  const parts = fullName
    .trim()
    .split(/\s+/)
    .filter(Boolean)

  return parts
    .slice(-2)
    .map((part) => part[0]?.toUpperCase() ?? '')
    .join('') || 'SA'
}

const navItems = [
  {
    to: '/admin/users',
    label: 'User Account Monitoring',
    icon: Users,
  },
  {
    to: '/admin/customer-status',
    label: 'Customer Account Status',
    icon: UserCheck,
  },
  {
    to: '/admin/employees',
    label: 'Employee Account Management',
    icon: Users,
  },
  {
    to: '/admin/assignment',
    label: 'Role & Facility Assignment',
    icon: ShieldCheck,
  },
  {
    to: '/admin/access-management',
    label: 'Access Management',
    icon: ShieldCheck,
  },
  {
    to: '/admin/login-history',
    label: 'Login History',
    icon: History,
  },
  {
    to: '/admin/activity-logs',
    label: 'Activity Logs',
    icon: ClipboardList,
  },
] as const

export function SystemAdministratorLayout() {
  const { user, logout } = useAuth()
  const displayName = user?.fullName?.trim() || 'System Administrator'
  const initials = getInitials(user?.fullName)

  return (
    <div className="admin-shell">
      <header className="admin-header">
        <div className="admin-brand">
          <BrandLogo size="small" />
          <span className="admin-portal-badge">
            SYSTEM ADMINISTRATOR PORTAL
          </span>
        </div>

        <div className="admin-header-user">
          <div className="admin-header-avatar">{initials}</div>
          <div className="admin-header-user-text">
            <strong>{displayName}</strong>
            <span>{user?.email ?? 'Employee account'}</span>
          </div>
        </div>
      </header>

      <div className="admin-body">
        <aside className="admin-sidebar">
          <div className="admin-profile">
            <div className="admin-profile-avatar">{initials}</div>
            <strong>{displayName}</strong>
            <span className="admin-profile-email">
              {user?.email ?? 'Employee account'}
            </span>
            <span className="admin-role-badge">
              System Administrator
            </span>
          </div>

          <nav
            className="admin-nav"
            aria-label="System Administrator navigation"
          >
            {navItems.map((item) => {
              const Icon = item.icon

              return (
                <NavLink
                  key={item.to}
                  to={item.to}
                  className={({ isActive }) =>
                    isActive
                      ? 'admin-nav-link admin-nav-link-active'
                      : 'admin-nav-link'
                  }
                >
                  <Icon size={18} />
                  <span>{item.label}</span>
                </NavLink>
              )
            })}
          </nav>

          <div className="admin-sidebar-actions">
            <button
              type="button"
              className="admin-signout"
              onClick={logout}
            >
              <LogOut size={18} />
              <span>Sign Out</span>
            </button>
          </div>
        </aside>

        <main className="admin-main">
          <Outlet />
        </main>
      </div>

      <footer className="admin-footer">
        <div className="admin-footer-main">
          <div className="admin-footer-brand">
            <BrandLogo inverse />
            <p>
              Access and account administration for FStoRent. Administration is separated from Customer business-data mutation.
            </p>
            <span className="admin-footer-trust">
              Secure · Role-based · Auditable
            </span>
          </div>

          <div className="admin-footer-column">
            <strong>ACCOUNTS</strong>
            <Link className="admin-footer-link" to="/admin/users">
              User Account Monitoring
            </Link>
            <Link className="admin-footer-link" to="/admin/customer-status">
              Customer Account Status
            </Link>
            <Link className="admin-footer-link" to="/admin/employees">
              Employee Management
            </Link>
          </div>

          <div className="admin-footer-column">
            <strong>SECURITY</strong>
            <Link className="admin-footer-link" to="/admin/assignment">
              Role &amp; Facility Assignment
            </Link>
            <Link className="admin-footer-link" to="/admin/login-history">
              Login History
            </Link>
            <Link className="admin-footer-link" to="/admin/activity-logs">
              Activity Logs
            </Link>
          </div>

          <div className="admin-footer-column">
            <strong>ACCOUNT</strong>
            <span>{displayName}</span>
            <span>System Administrator</span>
            <button
              type="button"
              className="admin-footer-logout"
              onClick={logout}
            >
              Sign Out
            </button>
          </div>
        </div>

        <div className="admin-footer-bottom">
          <span>© 2026 FStoRent. All rights reserved.</span>
          <div>
            <FileText size={12} aria-hidden="true" />
            <Link className="admin-footer-bottom-link" to="/admin/users">
              System Administrator Portal
            </Link>
          </div>
        </div>
      </footer>
    </div>
  )
}
