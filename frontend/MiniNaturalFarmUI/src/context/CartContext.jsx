import { createContext, useCallback, useContext, useEffect, useMemo, useState } from 'react'
import { cartApi } from '../api/services'
import { useAuth } from './AuthContext'

const CartContext = createContext(null)

const EMPTY = { items: [], totalQuantity: 0, subTotal: 0, hasUnavailableItems: false }

export function CartProvider({ children }) {
  const { isAuthenticated } = useAuth()
  const [cart, setCart] = useState(EMPTY)

  const refresh = useCallback(async () => {
    if (!isAuthenticated) {
      setCart(EMPTY)
      return
    }
    try {
      setCart(await cartApi.get())
    } catch {
      // keep what we have; the page shows its own errors on actions
    }
  }, [isAuthenticated])

  useEffect(() => {
    refresh()
  }, [refresh])

  const addItem = useCallback(async (productId, quantity = 1) => {
    setCart(await cartApi.addItem(productId, quantity))
  }, [])

  const setQuantity = useCallback(async (productId, quantity) => {
    setCart(await cartApi.setQuantity(productId, quantity))
  }, [])

  const removeItem = useCallback(async (productId) => {
    setCart(await cartApi.removeItem(productId))
  }, [])

  const value = useMemo(
    () => ({ cart, refresh, addItem, setQuantity, removeItem }),
    [cart, refresh, addItem, setQuantity, removeItem]
  )

  return <CartContext.Provider value={value}>{children}</CartContext.Provider>
}

export function useCart() {
  const ctx = useContext(CartContext)
  if (!ctx) throw new Error('useCart must be used inside CartProvider')
  return ctx
}
