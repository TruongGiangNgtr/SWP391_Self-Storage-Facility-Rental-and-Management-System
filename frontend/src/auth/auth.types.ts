export type UserRole =
  | 'CUSTOMER'
  | 'FACILITY_STAFF'
  | 'FACILITY_MANAGER'
  | 'BUSINESS_OPERATIONS_MANAGER'
  | 'SYSTEM_ADMINISTRATOR'

export interface AuthUser {
  fullName?: string | null
  email?: string
  userAccountId: string
  role: UserRole
  status: 'ACTIVE' | 'INACTIVE'
  customerId?: string
  employeeId?: string
  facilityId?: string | null
}

export interface AuthTokenData {
  accessToken: string
  tokenType: 'Bearer'
  user: AuthUser
}

export interface CurrentAccount {
  userAccountId: string
  role: UserRole
  status: 'ACTIVE' | 'INACTIVE'
  email: string
  phoneNumber: string
  profile: {
    customerId: string | null
    employeeId: string | null
    fullName: string | null
    facilityId: string | null
    // Missing until AUTH-004 is aligned; missing is not an empty National ID.
    cccd?: string | null
  } | null
}

export interface CustomerRegisterRequest {
  fullName: string
  phoneNumber: string
  email: string
  password: string
  cccd?: string | null
}

export interface CustomerLoginRequest {
  phoneNumber: string
  password: string
}

export interface EmployeeLoginRequest {
  email: string
  password: string
}

export interface UpdateCustomerProfileRequest {
  fullName?: string
  cccd?: string | null
}

export interface UpdatedCustomerProfile {
  userAccountId: string
  customerId: string
  role: 'CUSTOMER'
  status: 'ACTIVE'
  fullName: string
}
