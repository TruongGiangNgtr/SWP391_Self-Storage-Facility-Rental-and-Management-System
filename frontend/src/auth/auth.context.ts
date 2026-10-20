import { createContext, useContext } from 'react'
import type { AuthUser, CustomerLoginRequest, EmployeeLoginRequest, UpdateCustomerProfileRequest, UpdatedCustomerProfile } from './auth.types'

export interface AuthContextValue {
  user: AuthUser | null
  isAuthenticated: boolean
  isInitializing: boolean
  loginCustomer(request: CustomerLoginRequest): Promise<AuthUser>
  loginEmployee(request: EmployeeLoginRequest): Promise<AuthUser>
  updateCustomerProfile(request: UpdateCustomerProfileRequest): Promise<UpdatedCustomerProfile>
  logout(): void
}

export const AuthContext = createContext<AuthContextValue | undefined>(undefined)

export function useAuth(): AuthContextValue {
  const context = useContext(AuthContext)

  if (!context) {
    throw new Error('useAuth must be used inside AuthProvider.')
  }

  return context
}
