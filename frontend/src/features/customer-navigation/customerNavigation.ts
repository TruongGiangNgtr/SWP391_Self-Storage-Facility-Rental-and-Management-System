import { Bell, Bookmark, CalendarDays, FileText, LayoutGrid, Settings, Ticket, WalletCards, type LucideIcon } from 'lucide-react'

interface CustomerNavigationItem {
  to: string
  label: string
  icon: LucideIcon
  end?: boolean
  unavailable?: boolean
}

// Keep the agreed navigation complete. Availability describes UI implementation,
// not a business permission, persisted state, or a promise that an API is ready.
export const CUSTOMER_NAV_ITEMS: readonly CustomerNavigationItem[] = [
  { to: '/customer', label: 'Overview', icon: LayoutGrid, end: true },
  { to: '/customer/visits', label: 'My Visits', icon: Bookmark },
  { to: '/customer/reservations', label: 'My Reservations', icon: CalendarDays },
  { to: '/customer/contracts', label: 'My Contracts', icon: FileText },
  { to: '/customer/invoices', label: 'Deposit & Invoices', icon: WalletCards },
  { to: '/customer/support-tickets', label: 'Support Tickets', icon: Ticket },
  { to: '/customer/account-settings', label: 'Account Settings', icon: Settings },
]

export const CUSTOMER_NOTIFICATIONS = {
  to: '/customer/notifications', label: 'Notifications & History', icon: Bell,
} as const

export const CUSTOMER_UNAVAILABLE_PAGES = [
  ...CUSTOMER_NAV_ITEMS.filter(item => item.unavailable).map(item => ({ to: item.to, title: item.label, historyTabs: false })),
] as const

export function getCustomerNavigationItem(pathname: string) {
  return [...CUSTOMER_NAV_ITEMS, CUSTOMER_NOTIFICATIONS].find(item =>
    'end' in item && item.end ? pathname === item.to : pathname === item.to || pathname.startsWith(`${item.to}/`),
  )
}
