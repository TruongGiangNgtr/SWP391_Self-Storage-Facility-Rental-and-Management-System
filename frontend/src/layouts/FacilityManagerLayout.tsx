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
  NavLink,
  Outlet,
} from 'react-router-dom'
import { useAuth } from '../auth/auth.context'
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

  return (
    <div className="manager-shell">
      <aside className="manager-sidebar">
        <div className="manager-profile">
          <div className="manager-profile-avatar">
            {getInitials(user?.fullName)}
          </div>

          <strong>
            {user?.fullName?.trim() || 'Facility Manager'}
          </strong>

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
  )
}
