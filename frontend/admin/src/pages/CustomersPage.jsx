import { useState } from 'react'
import { Link } from 'react-router-dom'
import { adminApi } from '../api/services'
import useAsync from '../hooks/useAsync'
import useDebounce from '../hooks/useDebounce'
import Pagination from '../components/Pagination'
import { ErrorBox, Loading } from '../components/Status'
import { formatDate, formatPrice } from '../utils/format'

export default function CustomersPage() {
  const [search, setSearch] = useState('')
  const q = useDebounce(search, 400)
  const [page, setPage] = useState(1)

  const { data, loading, error, reload } = useAsync(
    () => adminApi.customers.list({ search: q, page, pageSize: 20 }),
    [q, page]
  )

  return (
    <>
      <h1>Customers</h1>

      <div className="filters card">
        <input type="search" placeholder="Search name, email or phone" value={search} onChange={(e) => { setSearch(e.target.value); setPage(1) }} />
      </div>

      {error && <ErrorBox message={error} onRetry={reload} />}
      {loading && !data && <Loading />}

      {data && (
        <>
          {data.items.length === 0 ? (
            <p className="muted">No customers found.</p>
          ) : (
            <div className="card table-wrap">
              <table className="table">
                <thead>
                  <tr><th>Customer</th><th>Phone</th><th>Joined</th><th className="right">Orders</th><th className="right">Total spent</th><th>Last order</th></tr>
                </thead>
                <tbody>
                  {data.items.map((c) => (
                    <tr key={c.id}>
                      <td>
                        <Link to={`/customers/${c.id}`} className="link">{c.fullName}</Link>
                        <div className="muted small">{c.email}</div>
                      </td>
                      <td>{c.phoneNumber || '—'}</td>
                      <td>{formatDate(c.createdAt)}</td>
                      <td className="right">{c.orderCount}</td>
                      <td className="right">{formatPrice(c.totalSpent)}</td>
                      <td>{formatDate(c.lastOrderAt)}</td>
                    </tr>
                  ))}
                </tbody>
              </table>
            </div>
          )}
          <Pagination page={data.page} totalPages={data.totalPages} onChange={(n) => { setPage(n); window.scrollTo({ top: 0 }) }} />
        </>
      )}
    </>
  )
}
