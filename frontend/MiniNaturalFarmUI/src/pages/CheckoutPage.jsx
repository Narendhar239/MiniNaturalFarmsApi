import { useEffect, useState } from 'react'
import { Link, useNavigate } from 'react-router-dom'
import { useTranslation } from 'react-i18next'
import { addressApi, ordersApi } from '../api/services'
import { getErrorMessage } from '../api/client'
import { useAuth } from '../context/AuthContext'
import { useCart } from '../context/CartContext'
import { formatPrice, pick } from '../utils/format'
import { shippingFor } from '../utils/config'
import { payForOrder } from '../utils/razorpay'
import { Loading } from '../components/Status'

export default function CheckoutPage() {
  const { t, i18n } = useTranslation()
  const lang = i18n.language
  const navigate = useNavigate()
  const { user } = useAuth()
  const { cart, refresh } = useCart()

  const [addresses, setAddresses] = useState([])
  const [addressId, setAddressId] = useState(null)
  const [method, setMethod] = useState('Cod')
  const [loadingAddresses, setLoadingAddresses] = useState(true)
  const [step, setStep] = useState('idle')
  const [error, setError] = useState('')

  useEffect(() => {
    refresh()
  }, [refresh])

  useEffect(() => {
    let cancelled = false
    addressApi
      .list()
      .then((list) => {
        if (cancelled) return
        setAddresses(list)
        const preferred = list.find((a) => a.isDefault) || list[0]
        setAddressId(preferred ? preferred.id : null)
      })
      .catch((e) => !cancelled && setError(getErrorMessage(e)))
      .finally(() => !cancelled && setLoadingAddresses(false))
    return () => {
      cancelled = true
    }
  }, [])

  const placeOrder = async () => {
    setError('')
    setStep('placing')
    try {
      const result = await ordersApi.checkout({ addressId, paymentMethod: method })
      await refresh()
      const orderId = result.order.id

      if (!result.payment) {
        navigate(`/orders/${orderId}?placed=1`, { replace: true })
        return
      }

      const outcome = await payForOrder({ orderId, payment: result.payment, user })
      if (outcome.status === 'paid') navigate(`/orders/${orderId}?placed=1`, { replace: true })
      else if (outcome.status === 'verify-failed') navigate(`/orders/${orderId}?payment=verify`, { replace: true })
      else navigate(`/orders/${orderId}?payment=cancelled`, { replace: true })
    } catch (e) {
      setError(getErrorMessage(e))
      setStep('idle')
      refresh()
    }
  }

  if (loadingAddresses) return <Loading />

  if (cart.items.length === 0 && step === 'idle') {
    return (
      <div className="center">
        <h1>{t('checkout.title')}</h1>
        <p className="muted">{t('cart.empty')}</p>
        <Link to="/products" className="btn btn-primary">{t('cart.continueShopping')}</Link>
      </div>
    )
  }

  const shipping = shippingFor(cart.subTotal)
  const total = cart.subTotal + shipping
  const busy = step !== 'idle'
  const canPlace = !busy && addressId && cart.items.length > 0 && !cart.hasUnavailableItems

  return (
    <>
      <h1>{t('checkout.title')}</h1>
      {error && <div className="alert alert-error">{error}</div>}
      {cart.hasUnavailableItems && (
        <div className="alert alert-warn">
          {t('cart.unavailableNote')} <Link to="/cart" className="link">{t('nav.cart')}</Link>
        </div>
      )}

      <div className="checkout-layout">
        <div>
          <section className="card panel">
            <h2>{t('checkout.deliveryAddress')}</h2>

            {addresses.length === 0 ? (
              <>
                <p className="muted">{t('checkout.noAddress')}</p>
                <Link to="/account/addresses" className="btn btn-primary btn-sm">{t('checkout.addAddress')}</Link>
              </>
            ) : (
              <>
                {addresses.map((a) => (
                  <label key={a.id} className={`option ${addressId === a.id ? 'selected' : ''}`}>
                    <input type="radio" name="address" checked={addressId === a.id} onChange={() => setAddressId(a.id)} />
                    <span>
                      <strong>{a.fullName}</strong> · {a.phone}
                      <br />
                      {a.line1}{a.line2 ? `, ${a.line2}` : ''}, {a.city}, {a.state} - {a.pincode}
                    </span>
                  </label>
                ))}
                <Link to="/account/addresses" className="link">{t('checkout.manageAddresses')}</Link>
              </>
            )}
          </section>

          <section className="card panel">
            <h2>{t('checkout.paymentMethod')}</h2>
            <label className={`option ${method === 'Cod' ? 'selected' : ''}`}>
              <input type="radio" name="method" checked={method === 'Cod'} onChange={() => setMethod('Cod')} />
              <span><strong>{t('checkout.cod')}</strong><br /><span className="muted">{t('checkout.codHint')}</span></span>
            </label>
            <label className={`option ${method === 'Online' ? 'selected' : ''}`}>
              <input type="radio" name="method" checked={method === 'Online'} onChange={() => setMethod('Online')} />
              <span><strong>{t('checkout.online')}</strong><br /><span className="muted">{t('checkout.onlineHint')}</span></span>
            </label>
          </section>
        </div>

        <aside className="card summary">
          <h2>{t('checkout.orderSummary')}</h2>
          {cart.items.map((item) => (
            <div key={item.id} className="summary-row small">
              <span>{pick(item, 'name', lang)} × {item.quantity}</span>
              <span>{formatPrice(item.lineTotal)}</span>
            </div>
          ))}
          <div className="summary-row"><span>{t('cart.subtotal')}</span><span>{formatPrice(cart.subTotal)}</span></div>
          <div className="summary-row">
            <span>{t('cart.shipping')}</span>
            <span>{shipping === 0 ? t('cart.free') : formatPrice(shipping)}</span>
          </div>
          <div className="summary-row summary-total"><span>{t('cart.total')}</span><span>{formatPrice(total)}</span></div>

          <button type="button" className="btn btn-primary btn-block" disabled={!canPlace} onClick={placeOrder}>
            {busy ? t('checkout.placing') : method === 'Online' ? t('checkout.payNow') : t('checkout.placeOrder')}
          </button>
        </aside>
      </div>
    </>
  )
}
