import {
  useCallback,
  useEffect,
  useMemo,
  useState,
  type ReactNode,
} from 'react'
import { authApi } from '../api/authApi'
import { authToken } from '../api/authToken'
import { AuthContext, type AuthContextValue } from './auth.context'
import type { AuthUser, CustomerLoginRequest, EmployeeLoginRequest } from './auth.types'

function mapCurrentAccount(
  account: Awaited<ReturnType<typeof authApi.getCurrentAccount>>['data'],
): AuthUser {
  return {
    userAccountId: account.userAccountId,
    role: account.role,
    status: account.status,
    customerId: account.profile.customerId,
    employeeId: account.profile.employeeId,
    facilityId: account.profile.facilityId,
  }
}

export function AuthProvider({ children }: { children: ReactNode }) {
  const [user, setUser] = useState<AuthUser | null>(null)
  const [isInitializing, setIsInitializing] = useState(true)

  const logout = useCallback(() => {
    authToken.clear()
    setUser(null)
  }, [])

  const loadCurrentAccount = useCallback(async (): Promise<AuthUser> => {
    const response = await authApi.getCurrentAccount()
    const currentUser = mapCurrentAccount(response.data)
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
      const response = await authApi.loginCustomer(request)
      authToken.set(response.data.accessToken)

      try {
        return await loadCurrentAccount()
      } catch {
        setUser(response.data.user)
        return response.data.user
      }
    },
    [loadCurrentAccount],
  )

  const loginEmployee = useCallback(
    async (request: EmployeeLoginRequest): Promise<AuthUser> => {
      const response = await authApi.loginEmployee(request)
      authToken.set(response.data.accessToken)

      try {
        return await loadCurrentAccount()
      } catch {
        setUser(response.data.user)
        return response.data.user
      }
    },
    [loadCurrentAccount],
  )

  const value = useMemo<AuthContextValue>(
    () => ({
      user,
      isAuthenticated: user !== null,
      isInitializing,
      loginCustomer,
      loginEmployee,
      logout,
    }),
    [isInitializing, loginCustomer, loginEmployee, logout, user],
  )

  return <AuthContext.Provider value={value}>{children}</AuthContext.Provider>
}
