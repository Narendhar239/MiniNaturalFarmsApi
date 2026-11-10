import { useEffect, useState } from 'react'
import { Link, useParams, useSearchParams } from 'react-router-dom'
import { useTranslation } from 'react-i18next'
import { ordersApi } from '../api/services'
import { getErrorMessage } from '../api/client'
import { useAuth } from '../context/AuthContext'
import { formatDate, formatPrice } from '../utils/format'
import { payForOrder } from '../utils/razorpay'
import StatusPill from '../components/StatusPill'
import { ErrorBox, Loading } from '../components/Status'

async function readError(e) {
  if (e?.response?.data instanceof Blob) {
    try {
      const body = JSON.parse(await e.response.data.text())
      return body.detail || body.title || getErrorMessage(e)
    } catch {
      return getErrorMessage(e)
    }
  }
  return getErrorMessage(e)
}

export default function OrderDetailPage() {
  const { id } = useParams()
  const [searchParams] = useSearchParams()
  const { t, i18n } = useTranslation()
  const { user } = useAuth()

  const [order, setOrder] = useState(null)
  const [loading, setLoading] = useState(true)
  const [error, setError] = useState('')
  const [reload, setReload] = useState(0)
  const [busy, setBusy] = useState(false)
  const [actionError, setActionError] = useState('')

  const placed = searchParams.get('placed') === '1'
  const paymentNote = searchParams.get('payment')

  useEffect(() => {
    let cancelled = false
    setLoading(true)
    setError('')

    ordersApi
      .get(id)
      .then((res) => !cancelled && setOrder(res))
      .catch((e) => !cancelled && setError(getErrorMessage(e)))
      .finally(() => !cancelled && setLoading(false))

    return () => {
      cancelled = true
    }
  }, [id, reload])

  if (loading) return <Loading />

  if (error) {
    return (
      <>
        <ErrorBox message={error} onRetry={() => setReload((n) => n + 1)} />
        <Link to="/orders" className="link">{t('orders.back')}</Link>
      </>
    )
  }

  const online = order.paymentMethod === 'Online'
  const canPay = online && order.paymentStatus === 'Pending' && order.status === 'Pending'
  const canCancel = ['Pending', 'Confirmed'].includes(order.status) && !(online && order.paymentStatus === 'Paid')
  const canInvoice = order.status !== 'Cancelled' && (order.paymentMethod === 'Cod' || order.paymentStatus === 'Paid')

  const payNow = async () => {
    setActionError('')
    setBusy(true)
    try {
      const payment = await ordersApi.payment(order.id)
      const outcome = await payForOrder({ orderId: order.id, payment, user })
      if (outcome.status === 'paid') setOrder(outcome.order)
      else if (outcome.status === 'verify-failed') setActionError(t('checkout.verifyFailed'))
      else if (outcome.status === 'script-error') setActionError(t('checkout.scriptFailed'))
    } catch (e) {
      setActionError(getErrorMessage(e))
    } finally {
      setBusy(false)
    }
  }

  const cancel = async () => {
    if (!window.confirm(t('orders.confirmCancel'))) return
    setActionError('')
    setBusy(true)
    try {
      setOrder(await ordersApi.cancel(order.id))
    } catch (e) {
      setActionError(getErrorMessage(e))
    } finally {
      setBusy(false)
    }
  }

  const downloadInvoice = async () => {
    setActionError('')
    setBusy(true)
    try {
      const blob = await ordersApi.invoice(order.id)
      const url = URL.createObjectURL(blob)
      const link = document.createElement('a')
      link.href = url
      link.download = `Invoice-${order.orderNumber}.pdf`
      document.body.appendChild(link)
      link.click()
      link.remove()
      URL.revokeObjectURL(url)
    } catch (e) {
      setActionError(await readError(e))
    } finally {
      setBusy(false)
    }
  }

  return (
    <>
      <Link to="/orders" className="link">← {t('orders.back')}</Link>

      {placed && <div className="alert alert-success">{t('orders.placedTitle')}</div>}
      {paymentNote === 'cancelled' && canPay && <div className="alert alert-warn">{t('checkout.paymentCancelled')}</div>}
      {paymentNote === 'verify' && <div className="alert alert-warn">{t('checkout.verifyFailed')}</div>}
      {actionError && <div className="alert alert-error">{actionError}</div>}

      <h1>{order.orderNumber}</h1>
      <div className="pills">
        <StatusPill value={order.status} />
        <StatusPill value={order.paymentStatus} group="payStatus" />
        <span className="muted small">{t(`payMethod.${order.paymentMethod}`, order.paymentMethod)} · {formatDate(order.createdAt, i18n.language)}</span>
      </div>

      <div className="actions">
        {canPay && <button type="button" className="btn btn-primary" disabled={busy} onClick={payNow}>{t('checkout.payNow')}</button>}
        {canInvoice && <button type="button" className="btn btn-outline" disabled={busy} onClick={downloadInvoice}>{t('orders.invoice')}</button>}
        {canCancel && <button type="button" className="btn btn-outline" disabled={busy} onClick={cancel}>{t('orders.cancel')}</button>}
      </div>

      <div className="checkout-layout">
        <section className="card panel">
          <h2>{t('orders.items')}</h2>
          {order.items.map((item) => (
            <div key={`${item.productId}-${item.productName}`} className="order-item">
              {item.imageUrl ? <img src={item.imageUrl} alt={item.productName} loading="lazy" /> : <span className="img-placeholder small-ph">🌿</span>}
              <div>
                <strong>{item.productName}</strong>
                <p className="muted small">{formatPrice(item.unitPrice)} × {item.quantity}</p>
              </div>
              <strong>{formatPrice(item.lineTotal)}</strong>
            </div>
          ))}
        </section>

        <aside>
          <div className="card summary">
            <div className="summary-row"><span>{t('cart.subtotal')}</span><span>{formatPrice(order.subTotal)}</span></div>
            <div className="summary-row">
              <span>{t('cart.shipping')}</span>
              <span>{order.shippingCharge === 0 ? t('cart.free') : formatPrice(order.shippingCharge)}</span>
            </div>
            <div className="summary-row summary-total"><span>{t('cart.total')}</span><span>{formatPrice(order.totalAmount)}</span></div>
          </div>

          <div className="card panel">
            <h2>{t('orders.shipTo')}</h2>
            <p>
              <strong>{order.shipFullName}</strong><br />
              {order.shipAddress}<br />
              {order.shipCity}, {order.shipState} - {order.shipPincode}<br />
              {order.shipPhone}
            </p>
          </div>
        </aside>
      </div>
    </>
  )
}
