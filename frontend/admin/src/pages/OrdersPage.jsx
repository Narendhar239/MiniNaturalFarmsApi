import { useState } from 'react'
import { Link, useSearchParams } from 'react-router-dom'
import { adminApi } from '../api/services'
import useAsync from '../hooks/useAsync'
import useDebounce from '../hooks/useDebounce'
import Pagination from '../components/Pagination'
import StatusPill from '../components/StatusPill'
import { ErrorBox, Loading } from '../components/Status'
import { formatDateTime, formatPrice } from '../utils/format'

const STATUSES = ['Pending', 'Confirmed', 'Processing', 'Dispatched', 'Delivered', 'Cancelled']
const PAYMENT_STATUSES = ['Pending', 'Paid', 'Failed', 'Refunded']
const METHODS = ['Cod', 'Online']

export default function OrdersPage() {
  const [params] = useSearchParams()
  const initialStatus = STATUSES.includes(params.get('status')) ? params.get('status') : ''

  const [filters, setFilters] = useState({
    search: '', status: initialStatus, paymentStatus: '', paymentMethod: '', from: '', to: '',
  })
  const [page, setPage] = useState(1)

  const search = useDebounce(filters.search, 400)
  const { status, paymentStatus, paymentMethod, from, to } = filters

  const { data, loading, error, reload } = useAsync(
    () => adminApi.orders.list({ search, status, paymentStatus, paymentMethod, from, to, page, pageSize: 20 }),
    [search, status, paymentStatus, paymentMethod, from, to, page]
  )

  const update = (name, value) => {
    setFilters((f) => ({ ...f, [name]: value }))
    setPage(1)
  }

  return (
    <>
      <h1>Orders</h1>

      <div className="filters card">
        <input type="search" placeholder="Search order no., name, phone, email" value={filters.search} onChange={(e) => update('search', e.target.value)} />
        <select value={status} onChange={(e) => update('status', e.target.value)}>
          <option value="">All statuses</option>
          {STATUSES.map((s) => <option key={s} value={s}>{s}</option>)}
        </select>
        <select value={paymentStatus} onChange={(e) => update('paymentStatus', e.target.value)}>
          <option value="">All payments</option>
          {PAYMENT_STATUSES.map((s) => <option key={s} value={s}>{s}</option>)}
        </select>
        <select value={paymentMethod} onChange={(e) => update('paymentMethod', e.target.value)}>
          <option value="">All methods</option>
          {METHODS.map((s) => <option key={s} value={s}>{s === 'Cod' ? 'Cash on delivery' : 'Online'}</option>)}
        </select>
        <label className="inline">From <input type="date" value={from} onChange={(e) => update('from', e.target.value)} /></label>
        <label className="inline">To <input type="date" value={to} onChange={(e) => update('to', e.target.value)} /></label>
      </div>

      {error && <ErrorBox message={error} onRetry={reload} />}
      {loading && !data && <Loading />}

      {data && (
        <>
          {data.items.length === 0 ? (
            <p className="muted">No orders match these filters.</p>
          ) : (
            <div className="card table-wrap">
              <table className="table">
                <thead>
                  <tr><th>Order</th><th>Customer</th><th>Status</th><th>Payment</th><th className="right">Items</th><th className="right">Total</th><th /></tr>
                </thead>
                <tbody>
                  {data.items.map((o) => (
                    <tr key={o.id}>
                      <td>
                        <strong>{o.orderNumber}</strong>
                        <div className="muted small">{formatDateTime(o.createdAt)}</div>
                      </td>
                      <td>
                        {o.customerName}
                        <div className="muted small">{o.customerEmail}</div>
                      </td>
                      <td><StatusPill value={o.status} /></td>
                      <td>
                        <StatusPill value={o.paymentStatus} />
                        <div className="muted small">{o.paymentMethod === 'Cod' ? 'Cash on delivery' : o.paymentMethod}</div>
                      </td>
                      <td className="right">{o.itemCount}</td>
                      <td className="right">{formatPrice(o.totalAmount)}</td>
                      <td className="right"><Link to={`/orders/${o.id}`} className="btn btn-outline btn-sm">Open</Link></td>
                    </tr>
                  ))}
                </tbody>
              </table>
            </div>
          )}
          <Pagination page={data.page} totalPages={data.totalPages} onChange={(p) => { setPage(p); window.scrollTo({ top: 0 }) }} />
        </>
      )}
    </>
  )
}
