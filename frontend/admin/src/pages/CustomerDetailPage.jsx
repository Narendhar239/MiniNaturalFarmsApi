import { Link, useParams } from 'react-router-dom'
import { adminApi } from '../api/services'
import useAsync from '../hooks/useAsync'
import StatCard from '../components/StatCard'
import StatusPill from '../components/StatusPill'
import { ErrorBox, Loading } from '../components/Status'
import { formatDate, formatDateTime, formatPrice } from '../utils/format'

export default function CustomerDetailPage() {
  const { id } = useParams()
  const { data: c, loading, error, reload } = useAsync(() => adminApi.customers.get(id), [id])

  if (error) return <><ErrorBox message={error} onRetry={reload} /><Link to="/customers" className="link">← Back to customers</Link></>
  if (loading && !c) return <Loading />

  return (
    <>
      <Link to="/customers" className="link">← Back to customers</Link>
      <h1>{c.fullName}</h1>
      <p className="muted">{c.email} · {c.phoneNumber || 'no phone'} · joined {formatDate(c.createdAt)}</p>

      <div className="stats">
        <StatCard label="Orders" value={c.orderCount} hint="not counting cancelled" />
        <StatCard label="Total spent" value={formatPrice(c.totalSpent)} hint="paid orders" />
        <StatCard label="Last order" value={formatDate(c.lastOrderAt)} />
        <StatCard label="Saved addresses" value={c.addressCount} />
      </div>

      <section className="card panel">
        <h2>Recent orders</h2>
        {c.recentOrders.length === 0 ? (
          <p className="muted">No orders yet.</p>
        ) : (
          <div className="table-wrap">
            <table className="table">
              <thead><tr><th>Order</th><th>Status</th><th>Payment</th><th className="right">Items</th><th className="right">Total</th></tr></thead>
              <tbody>
                {c.recentOrders.map((o) => (
                  <tr key={o.id}>
                    <td>
                      <Link to={`/orders/${o.id}`} className="link">{o.orderNumber}</Link>
                      <div className="muted small">{formatDateTime(o.createdAt)}</div>
                    </td>
                    <td><StatusPill value={o.status} /></td>
                    <td><StatusPill value={o.paymentStatus} /></td>
                    <td className="right">{o.itemCount}</td>
                    <td className="right">{formatPrice(o.totalAmount)}</td>
                  </tr>
                ))}
              </tbody>
            </table>
          </div>
        )}
      </section>
    </>
  )
}
