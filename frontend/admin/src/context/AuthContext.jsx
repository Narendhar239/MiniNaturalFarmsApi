import { createContext, useCallback, useContext, useEffect, useMemo, useState } from 'react'
import { authApi } from '../api/services'
import { clearAuth, loadAuth, saveAuth } from '../api/client'

const AuthContext = createContext(null)

export function AuthProvider({ children }) {
  const [session, setSession] = useState(() => loadAuth())

  useEffect(() => {
    const onLogout = () => setSession(null)
    window.addEventListener('auth:logout', onLogout)
    return () => window.removeEventListener('auth:logout', onLogout)
  }, [])

  const login = useCallback(async (email, password) => {
    const res = await authApi.login({ email, password })

    // Only admins may use this app; do not keep the token otherwise
    if (!res.roles?.includes('Admin')) throw new Error('NOT_ADMIN')

    const next = {
      token: res.token,
      expiresAt: res.expiresAt,
      user: { id: res.userId, email: res.email, fullName: res.fullName, roles: res.roles },
    }
    saveAuth(next)
    setSession(next)
  }, [])

  const logout = useCallback(() => {
    clearAuth()
    setSession(null)
  }, [])

  const value = useMemo(
    () => ({ user: session?.user ?? null, isAuthenticated: !!session, login, logout }),
    [session, login, logout]
  )

  return <AuthContext.Provider value={value}>{children}</AuthContext.Provider>
}

export function useAuth() {
  const ctx = useContext(AuthContext)
  if (!ctx) throw new Error('useAuth must be used inside AuthProvider')
  return ctx
}
