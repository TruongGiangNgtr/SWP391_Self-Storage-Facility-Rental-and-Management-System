import {
  useCallback,
  useEffect,
  useMemo,
  useState,
  type ReactNode,
} from 'react'
import { authApi } from '../api/authApi'
import { authToken } from '../api/authToken'
import { ApiRequestError } from '../api/httpClient'
import { abandonCustomerContinuation, clearCustomerContinuation } from './customerContinuation'
import { AuthContext, type AuthContextValue } from './auth.context'
import type { AuthUser, CustomerLoginRequest, EmployeeLoginRequest, UpdateCustomerProfileRequest, UpdatedCustomerProfile } from './auth.types'

function mapCurrentAccount(
  account: Awaited<ReturnType<typeof authApi.getCurrentAccount>>['data'],
): AuthUser {
  return {
    fullName: account.profile?.fullName,
    email: account.email,
    userAccountId: account.userAccountId,
    role: account.role,
    status: account.status,
    customerId: account.profile?.customerId ?? undefined,
    employeeId: account.profile?.employeeId ?? undefined,
    facilityId: account.profile?.facilityId,
  }
}

export function AuthProvider({ children }: { children: ReactNode }) {
  const [user, setUser] = useState<AuthUser | null>(null)
  const [isInitializing, setIsInitializing] = useState(true)

  const logout = useCallback(() => {
    abandonCustomerContinuation()
    authToken.clear()
    setUser(null)
  }, [])

  const loadCurrentAccount = useCallback(async (): Promise<AuthUser> => {
    const response = await authApi.getCurrentAccount()
    const currentUser = mapCurrentAccount(response.data)
    if (currentUser.status !== 'ACTIVE') throw new ApiRequestError(403, { code: 'ACCOUNT_INACTIVE', message: 'Account is inactive.' })
    setUser(currentUser)
    return currentUser
  }, [])

  useEffect(() => {
    async function restoreSession() {
      if (!authToken.get()) {
        setIsInitializing(false)
        return
      }

      try {
        await loadCurrentAccount()
      } catch {
        logout()
      } finally {
        setIsInitializing(false)
      }
    }

    void restoreSession()
  }, [loadCurrentAccount, logout])

  useEffect(() => {
    window.addEventListener('frms:unauthorized', logout)
    return () => window.removeEventListener('frms:unauthorized', logout)
  }, [logout])

  const loginCustomer = useCallback(
    async (request: CustomerLoginRequest): Promise<AuthUser> => {
      if (authToken.get()) clearCustomerContinuation()
      const response = await authApi.loginCustomer(request)
      authToken.set(response.data.accessToken)

      try {
        return await loadCurrentAccount()
      } catch (error) {
        logout()
        throw error
      }
    },
    [loadCurrentAccount, logout],
  )

  const loginEmployee = useCallback(
    async (request: EmployeeLoginRequest): Promise<AuthUser> => {
      clearCustomerContinuation()
      const response = await authApi.loginEmployee(request)
      authToken.set(response.data.accessToken)

      try {
        return await loadCurrentAccount()
      } catch (error) {
        logout()
        throw error
      }
    },
    [loadCurrentAccount, logout],
  )

  const updateCustomerProfile = useCallback(async (request: UpdateCustomerProfileRequest): Promise<UpdatedCustomerProfile> => {
    if (user?.role !== 'CUSTOMER') throw new ApiRequestError(403, { code: 'FORBIDDEN', message: 'Customer access required.' })
    const token = authToken.get()
    const { data } = await authApi.updateCustomerProfile(request)
    if (!data || data.userAccountId !== user.userAccountId || data.customerId !== user.customerId ||
      data.role !== 'CUSTOMER' || data.status !== 'ACTIVE' || typeof data.fullName !== 'string' || !data.fullName.trim() || data.fullName.length > 150) {
      throw new ApiRequestError(502, { code: 'UNEXPECTED_ERROR', message: 'Invalid profile update response.' })
    }
    // Use the server's accepted name; keep National ID page-local, never in session storage.
    if (authToken.get() === token) setUser(current => current?.userAccountId === data.userAccountId ? { ...current, fullName: data.fullName } : current)
    return data
  }, [user])

  const value = useMemo<AuthContextValue>(
    () => ({
      user,
      isAuthenticated: user !== null,
      isInitializing,
      loginCustomer,
      loginEmployee,
      updateCustomerProfile,
      logout,
    }),
    [isInitializing, loginCustomer, loginEmployee, logout, updateCustomerProfile, user],
  )

  return <AuthContext.Provider value={value}>{children}</AuthContext.Provider>
}
