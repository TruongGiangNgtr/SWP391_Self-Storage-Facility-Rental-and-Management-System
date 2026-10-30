import { EmployeePageTransition } from '../components/EmployeePageTransition'
import {
  ClipboardList,
  History,
  LogOut,
  ShieldCheck,
  UserCheck,
  Users,
} from 'lucide-react'
import {
  NavLink,
} from 'react-router-dom'
import { useAuth } from '../auth/auth.context'
import { BrandLogo } from '../components/BrandLogo'
import '../styles/manager.css'
import '../styles/admin.css'
import '../styles/employeeIdentity.css'
import '../styles/employeeWorkspace.css'

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
    label: 'User accounts',
    icon: Users,
  },
  {
    to: '/admin/customer-status',
    label: 'Customer access',
    icon: UserCheck,
  },
  {
    to: '/admin/employees',
    label: 'Employee accounts',
    icon: Users,
  },
  {
    to: '/admin/assignment',
    label: 'Roles & access',
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
    <div className="admin-shell employee-identity employee-workspace">
      <a className="portal-skip-link" href="#admin-main">Skip to content</a>
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
            <span>System Administrator</span>
          </div>
        </div>
      </header>

      <div className="admin-body">
        <aside className="manager-sidebar">
          <div className="manager-profile">
            <div className="manager-profile-avatar">{initials}</div>
            <strong title={displayName}>{displayName}</strong>
            <span className="manager-profile-email" title={user?.email ?? undefined}>
              {user?.email ?? 'Employee account'}
            </span>
            <span className="manager-role-badge">System Administrator</span>
          </div>

          <nav className="manager-nav" aria-label="System Administrator navigation">
            {navItems.map((item) => {
              const Icon = item.icon
              return (
                <NavLink
                  key={item.to}
                  to={item.to}
                  className={({ isActive }) => isActive
                    ? 'manager-nav-link manager-nav-link-active'
                    : 'manager-nav-link'}
                >
                  <Icon size={18} />
                  <span>{item.label}</span>
                </NavLink>
              )
            })}
          </nav>

          <div className="manager-sidebar-actions">
            <button type="button" className="manager-signout" onClick={logout}>
              <LogOut size={18} />
              <span>Sign Out</span>
            </button>
          </div>
        </aside>

        <main className="admin-main" id="admin-main" tabIndex={-1}>
          <EmployeePageTransition />
        </main>
      </div>

      <footer className="admin-footer">
        <span>© 2026 FStoRent</span>
        <span>System Administrator workspace</span>
      </footer>
    </div>
  )
}
