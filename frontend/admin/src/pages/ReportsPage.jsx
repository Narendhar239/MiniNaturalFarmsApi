import { useState } from 'react'
import { Link } from 'react-router-dom'
import { adminApi } from '../api/services'
import useAsync from '../hooks/useAsync'
import BarChart from '../components/BarChart'
import StatCard from '../components/StatCard'
import StatusPill from '../components/StatusPill'
import { ErrorBox, Loading } from '../components/Status'
import { downloadCsv } from '../utils/csv'
import { daysAgo, formatNumber, formatPrice, monthStart, today } from '../utils/format'

const TABS = [
  ['sales', 'Sales'],
  ['products', 'Top products'],
  ['payments', 'Payments'],
  ['customers', 'Customers'],
  ['inventory', 'Inventory'],
]

export default function ReportsPage() {
  const [tab, setTab] = useState('sales')
  const [range, setRange] = useState({ from: daysAgo(29), to: today() })

  const preset = (from) => setRange({ from, to: today() })
  const usesRange = tab !== 'inventory'

  return (
    <>
      <h1>Reports</h1>

      <div className="tabs">
        {TABS.map(([key, label]) => (
          <button key={key} type="button" className={tab === key ? 'active' : ''} onClick={() => setTab(key)}>{label}</button>
        ))}
      </div>

      {usesRange && (
        <div className="filters card">
          <label className="inline">From <input type="date" value={range.from} max={range.to} onChange={(e) => setRange((r) => ({ ...r, from: e.target.value }))} /></label>
          <label className="inline">To <input type="date" value={range.to} min={range.from} max={today()} onChange={(e) => setRange((r) => ({ ...r, to: e.target.value }))} /></label>
          <button type="button" className="btn btn-outline btn-sm" onClick={() => preset(daysAgo(6))}>Last 7 days</button>
          <button type="button" className="btn btn-outline btn-sm" onClick={() => preset(daysAgo(29))}>Last 30 days</button>
          <button type="button" className="btn btn-outline btn-sm" onClick={() => preset(daysAgo(89))}>Last 90 days</button>
          <button type="button" className="btn btn-outline btn-sm" onClick={() => preset(monthStart())}>This month</button>
        </div>
      )}

      {tab === 'sales' && <SalesTab {...range} />}
      {tab === 'products' && <ProductsTab {...range} />}
      {tab === 'payments' && <PaymentsTab {...range} />}
      {tab === 'customers' && <CustomersTab {...range} />}
      {tab === 'inventory' && <InventoryTab />}
    </>
  )
}

function Report({ state, children }) {
  if (state.error) return <ErrorBox message={state.error} onRetry={state.reload} />
  if (state.loading && !state.data) return <Loading />
  return children(state.data)
}

function SalesTab({ from, to }) {
  const [groupBy, setGroupBy] = useState('day')
  const state = useAsync(() => adminApi.reports.sales({ from, to, groupBy }), [from, to, groupBy])

  return (
    <Report state={state}>
      {(d) => (
        <>
          <div className="stats">
            <StatCard label="Revenue" value={formatPrice(d.totalRevenue)} hint="paid orders" />
            <StatCard label="Orders" value={formatNumber(d.totalOrders)} hint="not cancelled" />
            <StatCard label="Units sold" value={formatNumber(d.totalUnits)} />
          </div>

          <section className="card panel">
            <div className="section-head">
              <h2>Revenue per {groupBy}</h2>
              <div className="actions">
                <select value={groupBy} onChange={(e) => setGroupBy(e.target.value)}>
                  <option value="day">By day</option>
                  <option value="month">By month</option>
                </select>
                <button
                  type="button"
                  className="btn btn-outline btn-sm"
                  onClick={() => downloadCsv(`sales-${from}-to-${to}.csv`, [
                    ['Period', 'Orders', 'Units', 'Revenue', 'Average order value'],
                    ...d.rows.map((r) => [r.period, r.orders, r.units, r.revenue, r.averageOrderValue]),
                  ])}
                >
                  Export CSV
                </button>
              </div>
            </div>

            <BarChart
              data={d.rows.map((r) => ({ label: r.period, short: groupBy === 'month' ? r.period : r.period.slice(5), value: r.revenue }))}
              format={formatPrice}
            />

            <div className="table-wrap">
              <table className="table">
                <thead><tr><th>Period</th><th className="right">Orders</th><th className="right">Units</th><th className="right">Revenue</th><th className="right">Avg. order</th></tr></thead>
                <tbody>
                  {d.rows.map((r) => (
                    <tr key={r.period}>
                      <td>{r.period}</td>
                      <td className="right">{r.orders}</td>
                      <td className="right">{r.units}</td>
                      <td className="right">{formatPrice(r.revenue)}</td>
                      <td className="right">{formatPrice(r.averageOrderValue)}</td>
                    </tr>
                  ))}
                </tbody>
              </table>
            </div>
          </section>
        </>
      )}
    </Report>
  )
}

function ProductsTab({ from, to }) {
  const state = useAsync(() => adminApi.reports.topProducts({ from, to, take: 20 }), [from, to])

  return (
    <Report state={state}>
      {(rows) => (
        <section className="card panel">
          <div className="section-head">
            <h2>Best-selling products</h2>
            <button
              type="button"
              className="btn btn-outline btn-sm"
              onClick={() => downloadCsv(`top-products-${from}-to-${to}.csv`, [
                ['Product', 'Units sold', 'Revenue'],
                ...rows.map((r) => [r.name, r.unitsSold, r.revenue]),
              ])}
            >
              Export CSV
            </button>
          </div>
          {rows.length === 0 ? (
            <p className="muted">No sales in this period.</p>
          ) : (
            <div className="table-wrap">
              <table className="table">
                <thead><tr><th>#</th><th>Product</th><th className="right">Units sold</th><th className="right">Revenue</th></tr></thead>
                <tbody>
                  {rows.map((r, i) => (
                    <tr key={r.productId}>
                      <td>{i + 1}</td>
                      <td><Link to={`/products/${r.productId}`} className="link">{r.name}</Link></td>
                      <td className="right">{formatNumber(r.unitsSold)}</td>
                      <td className="right">{formatPrice(r.revenue)}</td>
                    </tr>
                  ))}
                </tbody>
              </table>
            </div>
          )}
        </section>
      )}
    </Report>
  )
}

function PaymentsTab({ from, to }) {
  const state = useAsync(() => adminApi.reports.payments({ from, to }), [from, to])

  return (
    <Report state={state}>
      {(d) => (
        <>
          <div className="stats">
            <StatCard label="Collected" value={formatPrice(d.collected)} hint="paid orders" />
            <StatCard label="Refunded" value={formatPrice(d.refunded)} />
            <StatCard label="Awaiting refund" value={formatPrice(d.awaitingRefund)} hint="paid but cancelled" tone={d.awaitingRefund > 0 ? 'warn' : ''} to="/orders?status=Cancelled" />
            <StatCard label="To collect" value={formatPrice(d.toCollect)} hint="unpaid, not cancelled" />
          </div>

          <section className="card panel">
            <div className="section-head">
              <h2>Breakdown</h2>
              <button
                type="button"
                className="btn btn-outline btn-sm"
                onClick={() => downloadCsv(`payments-${from}-to-${to}.csv`, [
                  ['Method', 'Payment status', 'Orders', 'Amount'],
                  ...d.breakdown.map((b) => [b.method, b.status, b.orders, b.amount]),
                ])}
              >
                Export CSV
              </button>
            </div>
            {d.breakdown.length === 0 ? (
              <p className="muted">No orders in this period.</p>
            ) : (
              <div className="table-wrap">
                <table className="table">
                  <thead><tr><th>Method</th><th>Payment status</th><th className="right">Orders</th><th className="right">Amount</th></tr></thead>
                  <tbody>
                    {d.breakdown.map((b) => (
                      <tr key={`${b.method}-${b.status}`}>
                        <td>{b.method === 'Cod' ? 'Cash on delivery' : b.method}</td>
                        <td><StatusPill value={b.status} /></td>
                        <td className="right">{b.orders}</td>
                        <td className="right">{formatPrice(b.amount)}</td>
                      </tr>
                    ))}
                  </tbody>
                </table>
              </div>
            )}
          </section>
        </>
      )}
    </Report>
  )
}

function CustomersTab({ from, to }) {
  const state = useAsync(() => adminApi.reports.customers({ from, to, take: 20 }), [from, to])

  return (
    <Report state={state}>
      {(d) => (
        <>
          <div className="stats">
            <StatCard label="Total customers" value={formatNumber(d.totalCustomers)} />
            <StatCard label="New in this period" value={formatNumber(d.newCustomers)} />
          </div>

          <section className="card panel">
            <div className="section-head">
              <h2>Top customers by spend</h2>
              <button
                type="button"
                className="btn btn-outline btn-sm"
                onClick={() => downloadCsv(`top-customers-${from}-to-${to}.csv`, [
                  ['Name', 'Email', 'Orders', 'Total spent'],
                  ...d.topCustomers.map((c) => [c.fullName, c.email, c.orders, c.totalSpent]),
                ])}
              >
                Export CSV
              </button>
            </div>
            {d.topCustomers.length === 0 ? (
              <p className="muted">No paid orders in this period.</p>
            ) : (
              <div className="table-wrap">
                <table className="table">
                  <thead><tr><th>Customer</th><th className="right">Orders</th><th className="right">Total spent</th></tr></thead>
                  <tbody>
                    {d.topCustomers.map((c) => (
                      <tr key={c.userId}>
                        <td>
                          <Link to={`/customers/${c.userId}`} className="link">{c.fullName}</Link>
                          <div className="muted small">{c.email}</div>
                        </td>
                        <td className="right">{c.orders}</td>
                        <td className="right">{formatPrice(c.totalSpent)}</td>
                      </tr>
                    ))}
                  </tbody>
                </table>
              </div>
            )}
          </section>
        </>
      )}
    </Report>
  )
}

function InventoryTab() {
  const [threshold, setThreshold] = useState(10)
  const state = useAsync(() => adminApi.reports.inventory({ lowStockThreshold: threshold }), [threshold])

  return (
    <Report state={state}>
      {(d) => (
        <>
          <div className="stats">
            <StatCard label="Products" value={formatNumber(d.totalProducts)} hint={`${d.activeProducts} visible`} />
            <StatCard label="Out of stock" value={formatNumber(d.outOfStock)} tone={d.outOfStock > 0 ? 'warn' : ''} to="/inventory?filter=Out" />
            <StatCard label={`Low stock (≤ ${d.lowStockThreshold})`} value={formatNumber(d.lowStock)} tone={d.lowStock > 0 ? 'warn' : ''} to="/inventory?filter=Low" />
            <StatCard label="Stock value" value={formatPrice(d.stockValue)} hint="visible products, at selling price" />
          </div>

          <section className="card panel">
            <div className="section-head">
              <h2>Products to restock</h2>
              <label className="inline">
                Threshold
                <input type="number" min="1" max="1000" value={threshold} onChange={(e) => setThreshold(Number(e.target.value) || 1)} style={{ width: 80 }} />
              </label>
            </div>
            {d.lowStockItems.length === 0 ? (
              <p className="muted">Everything is well stocked.</p>
            ) : (
              <div className="table-wrap">
                <table className="table">
                  <thead><tr><th>Product</th><th>Category</th><th>Status</th><th className="right">Stock</th></tr></thead>
                  <tbody>
                    {d.lowStockItems.map((i) => (
                      <tr key={i.productId}>
                        <td><Link to={`/products/${i.productId}`} className="link">{i.nameEn}</Link></td>
                        <td>{i.categoryNameEn}</td>
                        <td><StatusPill value={i.stockStatus} /></td>
                        <td className="right">{i.stockQuantity}</td>
                      </tr>
                    ))}
                  </tbody>
                </table>
              </div>
            )}
          </section>
        </>
      )}
    </Report>
  )
}
