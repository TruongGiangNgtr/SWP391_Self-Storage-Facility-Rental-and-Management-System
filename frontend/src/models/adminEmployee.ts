import type { UserRole } from '../auth/auth.types'
import type { UserAccountStatus } from './adminUser'

export type EmployeeRole = Exclude<UserRole, 'CUSTOMER'>

export interface CreateAdminEmployeeRequest {
  fullName: string
  email: string
  phoneNumber: string
  role: EmployeeRole
  facilityId: string | null
}

export interface UpdateAdminEmployeeRequest {
  fullName: string
}

export interface AssignAdminEmployeeRequest {
  role: EmployeeRole
  facilityId: string | null
}

export interface AdminEmployeeFilters {
  status: 'ALL' | UserAccountStatus
  role: 'ALL' | EmployeeRole
  search: string
}

export const ADMIN_EMPLOYEE_ROLES: EmployeeRole[] = [
  'FACILITY_STAFF',
  'FACILITY_MANAGER',
  'BUSINESS_OPERATIONS_MANAGER',
  'SYSTEM_ADMINISTRATOR',
]

export function isFacilityScopedEmployeeRole(role: EmployeeRole) {
  return role === 'FACILITY_STAFF' || role === 'FACILITY_MANAGER'
}
