import { createContext, useContext, useMemo, useState, type PropsWithChildren } from 'react'
import type { AuthenticatedUser } from '../models/api'

type AuthState = { accessToken: string; user: AuthenticatedUser } | null
type AuthContextValue = { auth: AuthState; setAuth: (auth: AuthState) => void; clearAuth: () => void }
const AuthContext = createContext<AuthContextValue | null>(null)

export function AuthProvider({ children }: PropsWithChildren) {
  const [auth, setAuth] = useState<AuthState>(null)
  const value = useMemo(() => ({ auth, setAuth, clearAuth: () => setAuth(null) }), [auth])
  return <AuthContext.Provider value={value}>{children}</AuthContext.Provider>
}

export function useAuth() { const value = useContext(AuthContext); if (!value) throw new Error('useAuth must be used within AuthProvider'); return value }
