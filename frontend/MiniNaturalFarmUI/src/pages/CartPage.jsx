import { useEffect, useState } from 'react'
import { Link, useNavigate } from 'react-router-dom'
import { useTranslation } from 'react-i18next'
import { getErrorMessage } from '../api/client'
import { useCart } from '../context/CartContext'
import { formatPrice, pick } from '../utils/format'
import { FREE_SHIPPING_THRESHOLD, shippingFor } from '../utils/config'

export default function CartPage() {
  const { t, i18n } = useTranslation()
  const lang = i18n.language
  const navigate = useNavigate()
  const { cart, refresh, setQuantity, removeItem } = useCart()

  const [busyId, setBusyId] = useState(null)
  const [error, setError] = useState('')

  useEffect(() => {
    refresh()
  }, [refresh])

  const run = async (productId, action) => {
    setError('')
    setBusyId(productId)
    try {
      await action()
    } catch (e) {
      setError(getErrorMessage(e))
    } finally {
      setBusyId(null)
    }
  }

  if (cart.items.length === 0) {
    return (
      <div className="center">
        <h1>{t('cart.title')}</h1>
        <p className="muted">{t('cart.empty')}</p>
        <Link to="/products" className="btn btn-primary">{t('cart.continueShopping')}</Link>
      </div>
    )
  }

  const shipping = shippingFor(cart.subTotal)
  const total = cart.subTotal + shipping
  const remainingForFree = FREE_SHIPPING_THRESHOLD - cart.subTotal

  return (
    <>
      <h1>{t('cart.title')}</h1>
      {error && <div className="alert alert-error">{error}</div>}
      {cart.hasUnavailableItems && <div className="alert alert-warn">{t('cart.unavailableNote')}</div>}

      <div className="cart-layout">
        <div className="cart-list">
          {cart.items.map((item) => {
            const name = pick(item, 'name', lang)
            const busy = busyId === item.productId
            const maxQty = Math.min(99, item.stockQuantity)

            return (
              <div key={item.id} className={`card cart-item ${item.isAvailable ? '' : 'unavailable'}`}>
                <Link to={`/products/${item.productId}`} className="cart-thumb">
                  {item.imageUrl ? <img src={item.imageUrl} alt={name} loading="lazy" /> : <span className="img-placeholder">🌿</span>}
                </Link>

                <div className="cart-info">
                  <Link to={`/products/${item.productId}`}><h3>{name}</h3></Link>
                  <div className="price">
                    <strong>{formatPrice(item.unitPrice)}</strong>
                    {item.discountPrice != null && <s>{formatPrice(item.price)}</s>}
                  </div>
                  {!item.isAvailable && (
                    <p className="stock out">
                      {item.stockQuantity > 0 && item.stockQuantity < item.quantity
                        ? t('product.onlyLeft', { count: item.stockQuantity })
                        : t('cart.unavailable')}
                    </p>
                  )}
                </div>

                <div className="qty">
                  <button type="button" disabled={busy || item.quantity <= 1} onClick={() => run(item.productId, () => setQuantity(item.productId, item.quantity - 1))}>−</button>
                  <span>{item.quantity}</span>
                  <button type="button" disabled={busy || item.quantity >= maxQty} onClick={() => run(item.productId, () => setQuantity(item.productId, item.quantity + 1))}>+</button>
                </div>

                <div className="cart-line">{formatPrice(item.lineTotal)}</div>

                <button type="button" className="btn btn-outline btn-sm" disabled={busy} onClick={() => run(item.productId, () => removeItem(item.productId))}>
                  {t('cart.remove')}
                </button>
              </div>
            )
          })}
        </div>

        <aside className="card summary">
          <div className="summary-row"><span>{t('cart.subtotal')}</span><span>{formatPrice(cart.subTotal)}</span></div>
          <div className="summary-row">
            <span>{t('cart.shipping')}</span>
            <span>{shipping === 0 ? t('cart.free') : formatPrice(shipping)}</span>
          </div>
          {shipping > 0 && remainingForFree > 0 && (
            <p className="muted small">{t('cart.freeShippingHint', { amount: formatPrice(remainingForFree) })}</p>
          )}
          <div className="summary-row summary-total"><span>{t('cart.total')}</span><span>{formatPrice(total)}</span></div>

          <button type="button" className="btn btn-primary btn-block" disabled={cart.hasUnavailableItems} onClick={() => navigate('/checkout')}>
            {t('cart.checkout')}
          </button>
          <Link to="/products" className="link center block">{t('cart.continueShopping')}</Link>
        </aside>
      </div>
    </>
  )
}
