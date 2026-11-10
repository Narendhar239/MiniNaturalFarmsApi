import { Link } from 'react-router-dom'
import { adminApi } from '../api/services'
import useAsync from '../hooks/useAsync'
import { ErrorBox, Loading } from '../components/Status'
import StatCard from '../components/StatCard'
import BarChart from '../components/BarChart'
import StatusPill from '../components/StatusPill'
import { formatDateTime, formatNumber, formatPrice } from '../utils/format'

const STATUSES = ['Pending', 'Confirmed', 'Processing', 'Dispatched', 'Delivered', 'Cancelled']

export default function DashboardPage() {
  const { data, loading, error, reload } = useAsync(() => adminApi.reports.dashboard(), [])

  if (error) return <ErrorBox message={error} onRetry={reload} />
  if (loading && !data) return <Loading />

  const attention = data.lowStockCount + data.outOfStockCount
  const chart = data.last30Days.map((r) => ({ label: r.period, short: r.period.slice(5), value: r.revenue }))

  return (
    <>
      <h1>Dashboard</h1>

      <div className="stats">
        <StatCard label="Today's revenue" value={formatPrice(data.todayRevenue)} hint={`${data.todayOrders} orders today`} />
        <StatCard label="This month's revenue" value={formatPrice(data.monthRevenue)} hint={`${data.monthOrders} orders this month`} />
        <StatCard label="Customers" value={formatNumber(data.totalCustomers)} hint={`${data.newCustomersThisMonth} new this month`} to="/customers" />
        <StatCard
          label="Stock alerts"
          value={formatNumber(attention)}
          hint={`${data.lowStockCount} low · ${data.outOfStockCount} out of stock`}
          tone={attention > 0 ? 'warn' : ''}
          to="/inventory?filter=Low"
        />
      </div>

      <section className="card panel">
        <h2>Orders by status</h2>
        <div className="status-row">
          {STATUSES.map((s) => (
            <Link key={s} to={`/orders?status=${s}`} className="status-box">
              <strong>{data.ordersByStatus[s] ?? 0}</strong>
              <StatusPill value={s} />
            </Link>
          ))}
        </div>
      </section>

      <section className="card panel">
        <h2>Revenue, last 30 days</h2>
        <BarChart data={chart} format={formatPrice} />
        <p className="muted small">Revenue counts paid orders. COD orders count once they are delivered.</p>
      </section>

      <div className="two-col">
        <section className="card panel">
          <h2>Top products (30 days)</h2>
          {data.topProducts.length === 0 ? (
            <p className="muted">No sales yet.</p>
          ) : (
            <div className="table-wrap">
              <table className="table">
                <thead><tr><th>Product</th><th className="right">Units</th><th className="right">Revenue</th></tr></thead>
                <tbody>
                  {data.topProducts.map((p) => (
                    <tr key={p.productId}>
                      <td><Link to={`/products/${p.productId}`} className="link">{p.name}</Link></td>
                      <td className="right">{formatNumber(p.unitsSold)}</td>
                      <td className="right">{formatPrice(p.revenue)}</td>
                    </tr>
                  ))}
                </tbody>
              </table>
            </div>
          )}
        </section>

        <section className="card panel">
          <h2>Recent orders</h2>
          {data.recentOrders.length === 0 ? (
            <p className="muted">No orders yet.</p>
          ) : (
            <div className="table-wrap">
              <table className="table">
                <thead><tr><th>Order</th><th>Status</th><th className="right">Total</th></tr></thead>
                <tbody>
                  {data.recentOrders.map((o) => (
                    <tr key={o.id}>
                      <td>
                        <Link to={`/orders/${o.id}`} className="link">{o.orderNumber}</Link>
                        <div className="muted small">{o.customerName} · {formatDateTime(o.createdAt)}</div>
                      </td>
                      <td><StatusPill value={o.status} /></td>
                      <td className="right">{formatPrice(o.totalAmount)}</td>
                    </tr>
                  ))}
                </tbody>
              </table>
            </div>
          )}
        </section>
      </div>
    </>
  )
}
