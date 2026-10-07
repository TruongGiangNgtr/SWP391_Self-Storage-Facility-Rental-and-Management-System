import type { UserRole } from '../auth/auth.types'

export type UserAccountStatus = 'ACTIVE' | 'INACTIVE'

export interface AdminUserProfile {
  customerId: string | null
  employeeId: string | null
  fullName: string
  address: string | null
  cccd: string | null
  facilityId: string | null
}

export interface AdminUserAccount {
  userAccountId: string
  role: UserRole
  status: UserAccountStatus
  email: string
  phoneNumber: string
  createdAt: string
  profile: AdminUserProfile | null
}

export interface AdminUserListQuery {
  page?: number
  pageSize?: number
}

export interface AdminUserFilters {
  status: 'ALL' | UserAccountStatus
  role: 'ALL' | UserRole
  search: string
}

export interface AdminApiErrorShape {
  code: string
  message: string
  traceId?: string
  errors?: Record<string, string[]>
}

export const ADMIN_USER_ROLES: UserRole[] = [
  'CUSTOMER',
  'FACILITY_STAFF',
  'FACILITY_MANAGER',
  'BUSINESS_OPERATIONS_MANAGER',
  'SYSTEM_ADMINISTRATOR',
]

export const ADMIN_USER_STATUSES: UserAccountStatus[] = [
  'ACTIVE',
  'INACTIVE',
]

export const USER_ROLE_LABELS: Record<UserRole, string> = {
  CUSTOMER: 'Customer',
  FACILITY_STAFF: 'Facility Staff',
  FACILITY_MANAGER: 'Facility Manager',
  BUSINESS_OPERATIONS_MANAGER: 'Business Operations Manager',
  SYSTEM_ADMINISTRATOR: 'System Administrator',
}

export function getAccountType(account: AdminUserAccount): 'Customer' | 'Employee' | 'Account' {
  if (account.profile?.customerId) {
    return 'Customer'
  }

  if (account.profile?.employeeId) {
    return 'Employee'
  }

  return account.role === 'CUSTOMER' ? 'Customer' : 'Account'
}
