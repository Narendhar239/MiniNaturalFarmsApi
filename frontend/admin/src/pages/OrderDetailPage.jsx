import { useState } from 'react'
import { Link, useParams } from 'react-router-dom'
import { adminApi } from '../api/services'
import { getBlobErrorMessage, getErrorMessage } from '../api/client'
import useAsync from '../hooks/useAsync'
import StatusPill from '../components/StatusPill'
import { ErrorBox, Loading } from '../components/Status'
import { formatDateTime, formatPrice } from '../utils/format'

export default function OrderDetailPage() {
  const { id } = useParams()
  const { data: order, loading, error, reload, setData } = useAsync(() => adminApi.orders.get(id), [id])
  const [busy, setBusy] = useState(false)
  const [message, setMessage] = useState(null)

  if (error) return <><ErrorBox message={error} onRetry={reload} /><Link to="/orders" className="link">← Back to orders</Link></>
  if (loading && !order) return <Loading />

  const run = async (action, successText) => {
    setMessage(null)
    setBusy(true)
    try {
      const updated = await action()
      if (updated) setData(updated)
      if (successText) setMessage({ type: 'success', text: successText })
    } catch (e) {
      setMessage({ type: 'error', text: e?.response?.data instanceof Blob ? await getBlobErrorMessage(e) : getErrorMessage(e) })
    } finally {
      setBusy(false)
    }
  }

  const changeStatus = (status) => {
    const extra = status === 'Cancelled' ? ' The stock will be put back.' : ''
    if (!window.confirm(`Change this order to ${status}?${extra}`)) return
    run(() => adminApi.orders.setStatus(order.id, status), `Order is now ${status}.`)
  }

  const refund = () => {
    if (!window.confirm(`Refund ${formatPrice(order.totalAmount)} to the customer through Razorpay?`)) return
    run(() => adminApi.orders.refund(order.id), 'Refund issued.')
  }

  const invoice = () =>
    run(async () => {
      const blob = await adminApi.orders.invoice(order.id)
      const url = URL.createObjectURL(blob)
      const link = document.createElement('a')
      link.href = url
      link.download = `Invoice-${order.orderNumber}.pdf`
      document.body.appendChild(link)
      link.click()
      link.remove()
      URL.revokeObjectURL(url)
      return null
    })

  const online = order.paymentMethod === 'Online'
  const canRefund = online && order.paymentStatus === 'Paid' && order.status === 'Cancelled'
  const needsCancelForRefund = online && order.paymentStatus === 'Paid' && order.status !== 'Cancelled'
  const canInvoice = order.status !== 'Cancelled' && (order.paymentMethod === 'Cod' || order.paymentStatus === 'Paid')

  return (
    <>
      <Link to="/orders" className="link">← Back to orders</Link>
      <h1>{order.orderNumber}</h1>

      <div className="pills">
        <StatusPill value={order.status} />
        <StatusPill value={order.paymentStatus} />
        <span className="muted small">{online ? 'Online payment' : 'Cash on delivery'} · {formatDateTime(order.createdAt)}</span>
      </div>

      {message && <div className={`alert alert-${message.type}`}>{message.text}</div>}

      <div className="actions">
        {order.allowedNextStatuses.map((s) => (
          <button
            key={s}
            type="button"
            className={s === 'Cancelled' ? 'btn btn-danger' : 'btn btn-primary'}
            disabled={busy}
            onClick={() => changeStatus(s)}
          >
            {s === 'Cancelled' ? 'Cancel order' : `Mark ${s}`}
          </button>
        ))}
        {canRefund && <button type="button" className="btn btn-primary" disabled={busy} onClick={refund}>Refund {formatPrice(order.totalAmount)}</button>}
        {canInvoice && <button type="button" className="btn btn-outline" disabled={busy} onClick={invoice}>Download invoice</button>}
      </div>
      {needsCancelForRefund && <p className="muted small">To refund a paid online order, cancel it first.</p>}

      <div className="two-col wide-left">
        <section className="card panel">
          <h2>Items</h2>
          <div className="table-wrap">
            <table className="table">
              <thead><tr><th>Product</th><th className="right">Price</th><th className="right">Qty</th><th className="right">Amount</th></tr></thead>
              <tbody>
                {order.items.map((i) => (
                  <tr key={`${i.productId}-${i.productName}`}>
                    <td>
                      <div className="cell-product">
                        {i.imageUrl ? <img src={i.imageUrl} alt="" /> : <span className="thumb-ph">🌿</span>}
                        <Link to={`/products/${i.productId}`} className="link">{i.productName}</Link>
                      </div>
                    </td>
                    <td className="right">{formatPrice(i.unitPrice)}</td>
                    <td className="right">{i.quantity}</td>
                    <td className="right">{formatPrice(i.lineTotal)}</td>
                  </tr>
                ))}
              </tbody>
            </table>
          </div>
          <div className="totals">
            <div><span>Subtotal</span><span>{formatPrice(order.subTotal)}</span></div>
            <div><span>Shipping</span><span>{order.shippingCharge === 0 ? 'FREE' : formatPrice(order.shippingCharge)}</span></div>
            <div className="grand"><span>Total</span><span>{formatPrice(order.totalAmount)}</span></div>
          </div>
        </section>

        <div>
          <section className="card panel">
            <h2>Customer & delivery</h2>
            <p>
              <strong>{order.shipFullName}</strong><br />
              {order.shipAddress}<br />
              {order.shipCity}, {order.shipState} - {order.shipPincode}<br />
              {order.shipPhone}<br />
              <span className="muted">{order.customerEmail}</span>
            </p>
          </section>

          {online && (
            <section className="card panel">
              <h2>Razorpay</h2>
              <dl className="kv">
                <dt>Order id</dt><dd>{order.razorpayOrderId || '—'}</dd>
                <dt>Payment id</dt><dd>{order.razorpayPaymentId || '—'}</dd>
                <dt>Refund id</dt><dd>{order.razorpayRefundId || '—'}</dd>
                {order.refundedAt && (<><dt>Refunded at</dt><dd>{formatDateTime(order.refundedAt)}</dd></>)}
              </dl>
            </section>
          )}
        </div>
      </div>
    </>
  )
}
