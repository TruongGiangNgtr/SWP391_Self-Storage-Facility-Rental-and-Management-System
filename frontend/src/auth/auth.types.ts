export type UserRole =
  | 'CUSTOMER'
  | 'FACILITY_STAFF'
  | 'FACILITY_MANAGER'
  | 'BUSINESS_OPERATIONS_MANAGER'
  | 'SYSTEM_ADMINISTRATOR'

export interface AuthUser {
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
    customerId?: string
    employeeId?: string
    fullName: string
    facilityId?: string | null
  }
}

export interface CustomerRegisterRequest {
  fullName: string
  phoneNumber: string
  email: string
  password: string
  address?: string | null
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
  fullName: string
  address?: string | null
  cccd?: string | null
}
