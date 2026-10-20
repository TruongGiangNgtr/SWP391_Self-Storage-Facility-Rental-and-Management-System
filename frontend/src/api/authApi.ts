import type {
  AuthTokenData,
  CurrentAccount,
  CustomerLoginRequest,
  CustomerRegisterRequest,
  EmployeeLoginRequest,
  UpdateCustomerProfileRequest,
  UpdatedCustomerProfile,
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

  getCurrentAccount(signal?: AbortSignal): Promise<ApiResponse<CurrentAccount>> {
    return httpClient.get('/auth/me', signal)
  },

  updateCustomerProfile(
    request: UpdateCustomerProfileRequest,
  ): Promise<ApiResponse<UpdatedCustomerProfile>> {
    return httpClient.patch('/customers/me/profile', request)
  },
}
