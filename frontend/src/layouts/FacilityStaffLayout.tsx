import {
  ClipboardCheck,
  ClipboardList,
  DoorOpen,
  Handshake,
  LifeBuoy,
  LogOut,
  RotateCcw,
} from 'lucide-react'
import { Link, NavLink, Outlet } from 'react-router-dom'
import { useAuth } from '../auth/auth.context'
import { BrandLogo } from '../components/BrandLogo'
import '../styles/staff.css'

function getInitials(fullName?: string | null) {
  if (!fullName?.trim()) return 'FS'

  const parts = fullName.trim().split(/\s+/).filter(Boolean)
  return parts
    .slice(-2)
    .map((part) => part[0]?.toUpperCase() ?? '')
    .join('') || 'FS'
}

const navItems = [
  { to: '/staff/work-list', label: 'Daily Work List', icon: ClipboardList },
  { to: '/staff/reservation-check-in', label: 'Reservation Check-in', icon: ClipboardCheck },
  { to: '/staff/handover-processing', label: 'Handover Processing', icon: Handshake },
  { to: '/staff/access-visits', label: 'Access Visit Processing', icon: DoorOpen },
  { to: '/staff/returns', label: 'Handle Returns', icon: RotateCcw },
  { to: '/staff/support', label: 'Support Processing', icon: LifeBuoy },
] as const

export function FacilityStaffLayout() {
  const { user, logout } = useAuth()
  const displayName = user?.fullName?.trim() || 'Facility Staff'
  const initials = getInitials(user?.fullName)

  return (
    <div className="staff-shell">
      <header className="staff-header">
        <div className="staff-brand">
          <BrandLogo size="small" />
          <span className="staff-portal-badge">FACILITY STAFF PORTAL</span>
        </div>

        <div className="staff-header-user">
          <div className="staff-header-avatar">{initials}</div>
          <div className="staff-header-user-text">
            <strong>{displayName}</strong>
            <span>{user?.email ?? 'Employee account'}</span>
          </div>
        </div>
      </header>

      <div className="staff-body">
        <aside className="staff-sidebar">
          <div className="staff-profile">
            <div className="staff-profile-avatar">{initials}</div>
            <strong>{displayName}</strong>
            <span className="staff-profile-email">{user?.email ?? 'Employee account'}</span>
            <span className="staff-role-badge">Facility Staff</span>
          </div>

          <nav className="staff-nav" aria-label="Facility Staff navigation">
            {navItems.map((item) => {
              const Icon = item.icon
              return (
                <NavLink
                  key={item.to}
                  to={item.to}
                  className={({ isActive }) => isActive ? 'staff-nav-link staff-nav-link-active' : 'staff-nav-link'}
                >
                  <Icon size={18} />
                  <span>{item.label}</span>
                </NavLink>
              )
            })}
          </nav>

          <div className="staff-sidebar-actions">
            <button type="button" className="staff-signout" onClick={logout}>
              <LogOut size={18} />
              <span>Sign Out</span>
            </button>
          </div>
        </aside>

        <main className="staff-main">
          <Outlet />
        </main>
      </div>

      <footer className="staff-footer">
        <div>
          <BrandLogo inverse />
          <span>Facility-scoped operations · Actual actor tracking · Authoritative server state</span>
        </div>
        <Link to="/staff/work-list">Facility Staff Portal</Link>
      </footer>
    </div>
  )
}
