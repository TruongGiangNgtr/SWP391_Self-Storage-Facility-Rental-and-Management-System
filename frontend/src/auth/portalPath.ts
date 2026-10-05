import type { UserRole } from './auth.types'

const PORTAL_PATHS: Record<UserRole, string> = {
  CUSTOMER: '/customer',
  FACILITY_STAFF: '/staff',
  FACILITY_MANAGER: '/manager',
  BUSINESS_OPERATIONS_MANAGER: '/business',
  SYSTEM_ADMINISTRATOR: '/admin',
}

export function getPortalPath(role: UserRole): string {
  return PORTAL_PATHS[role]
}
