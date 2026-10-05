import type {
  AuthTokenData,
  AuthUser,
  CurrentAccount,
  CustomerLoginRequest,
  CustomerRegisterRequest,
  EmployeeLoginRequest,
  UpdateCustomerProfileRequest,
} from '../auth/auth.types'
import type { ApiResponse } from './api.types'
import { httpClient } from './httpClient'

export const authApi = {
  registerCustomer(
    request: CustomerRegisterRequest,
  ): Promise<ApiResponse<{ userAccountId: string; customerId: string }>> {
    return httpClient.post('/auth/customer/register', request)
  },

  loginCustomer(request: CustomerLoginRequest): Promise<ApiResponse<AuthTokenData>> {
    return httpClient.post('/auth/customer/login', request)
  },

  loginEmployee(request: EmployeeLoginRequest): Promise<ApiResponse<AuthTokenData>> {
    return httpClient.post('/auth/employee/login', request)
  },

  getCurrentAccount(): Promise<ApiResponse<CurrentAccount>> {
    return httpClient.get('/auth/me')
  },

  updateCustomerProfile(
    request: UpdateCustomerProfileRequest,
  ): Promise<ApiResponse<AuthUser>> {
    return httpClient.patch('/customers/me/profile', request)
  },
}
