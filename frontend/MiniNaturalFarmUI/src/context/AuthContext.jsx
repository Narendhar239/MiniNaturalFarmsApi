import { createContext, useCallback, useContext, useEffect, useMemo, useState } from 'react'
import { authApi } from '../api/services'
import { clearAuth, loadAuth, saveAuth } from '../api/client'

const AuthContext = createContext(null)

function toSession(res) {
  return {
    token: res.token,
    expiresAt: res.expiresAt,
    user: { id: res.userId, email: res.email, fullName: res.fullName, roles: res.roles },
  }
}

export function AuthProvider({ children }) {
  const [session, setSession] = useState(() => {
    const auth = loadAuth()
    if (!auth) clearAuth()
    return auth
  })

  useEffect(() => {
    const onLogout = () => setSession(null)
    window.addEventListener('auth:logout', onLogout)
    return () => window.removeEventListener('auth:logout', onLogout)
  }, [])

  const finish = useCallback((res) => {
    const next = toSession(res)
    saveAuth(next)
    setSession(next)
    return next.user
  }, [])

  const login = useCallback(async (data) => finish(await authApi.login(data)), [finish])
  const register = useCallback(async (data) => finish(await authApi.register(data)), [finish])

  const logout = useCallback(() => {
    clearAuth()
    setSession(null)
  }, [])

  const updateUser = useCallback((patch) => {
    setSession((prev) => {
      if (!prev) return prev
      const next = { ...prev, user: { ...prev.user, ...patch } }
      saveAuth(next)
      return next
    })
  }, [])

  const value = useMemo(
    () => ({
      user: session?.user ?? null,
      isAuthenticated: !!session,
      login,
      register,
      logout,
      updateUser,
    }),
    [session, login, register, logout, updateUser]
  )

  return <AuthContext.Provider value={value}>{children}</AuthContext.Provider>
}

export function useAuth() {
  const ctx = useContext(AuthContext)
  if (!ctx) throw new Error('useAuth must be used inside AuthProvider')
  return ctx
}
