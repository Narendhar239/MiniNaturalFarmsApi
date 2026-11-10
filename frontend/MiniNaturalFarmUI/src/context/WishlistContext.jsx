import { createContext, useCallback, useContext, useEffect, useMemo, useState } from 'react'
import { wishlistApi } from '../api/services'
import { useAuth } from './AuthContext'

const WishlistContext = createContext(null)

export function WishlistProvider({ children }) {
  const { isAuthenticated } = useAuth()
  const [ids, setIds] = useState(() => new Set())

  useEffect(() => {
    if (!isAuthenticated) {
      setIds(new Set())
      return
    }
    let cancelled = false
    wishlistApi
      .ids()
      .then((list) => !cancelled && setIds(new Set(list)))
      .catch(() => {})
    return () => {
      cancelled = true
    }
  }, [isAuthenticated])

  const has = useCallback((productId) => ids.has(productId), [ids])

  const toggle = useCallback(
    async (productId) => {
      const previous = ids
      const adding = !previous.has(productId)
      const next = new Set(previous)
      if (adding) next.add(productId)
      else next.delete(productId)
      setIds(next)

      try {
        if (adding) await wishlistApi.add(productId)
        else await wishlistApi.remove(productId)
      } catch (error) {
        setIds(previous)
        throw error
      }
    },
    [ids]
  )

  const value = useMemo(() => ({ ids, count: ids.size, has, toggle }), [ids, has, toggle])

  return <WishlistContext.Provider value={value}>{children}</WishlistContext.Provider>
}

export function useWishlist() {
  const ctx = useContext(WishlistContext)
  if (!ctx) throw new Error('useWishlist must be used inside WishlistProvider')
  return ctx
}
