import { useState } from 'react'
import { Link, useSearchParams } from 'react-router-dom'
import { adminApi, categoryApi } from '../api/services'
import { getErrorMessage } from '../api/client'
import useAsync from '../hooks/useAsync'
import useDebounce from '../hooks/useDebounce'
import Pagination from '../components/Pagination'
import StatusPill from '../components/StatusPill'
import { ErrorBox, Loading } from '../components/Status'
import { formatPrice } from '../utils/format'

const FILTERS = [['All', 'All products'], ['Low', 'Low stock'], ['Out', 'Out of stock']]

export default function InventoryPage() {
  const [params] = useSearchParams()
  const initial = FILTERS.some(([v]) => v === params.get('filter')) ? params.get('filter') : 'All'

  const [filter, setFilter] = useState(initial)
  const [search, setSearch] = useState('')
  const q = useDebounce(search, 400)
  const [categoryId, setCategoryId] = useState('')
  const [threshold, setThreshold] = useState(10)
  const [page, setPage] = useState(1)
  const [edits, setEdits] = useState({})
  const [busyId, setBusyId] = useState(null)
  const [actionError, setActionError] = useState('')

  const cats = useAsync(() => categoryApi.list(), [])
  const list = useAsync(
    () => adminApi.inventory.list({ search: q, categoryId, filter, lowStockThreshold: threshold, page, pageSize: 20 }),
    [q, categoryId, filter, threshold, page]
  )

  const save = async (item) => {
    const raw = edits[item.productId]
    const qty = Number(raw)
    if (raw === '' || !Number.isInteger(qty) || qty < 0) {
      setActionError('Enter a whole number, 0 or more.')
      return
    }

    setActionError('')
    setBusyId(item.productId)
    try {
      await adminApi.inventory.setStock(item.productId, qty)
      setEdits((e) => {
        const next = { ...e }
        delete next[item.productId]
        return next
      })
      list.reload()
    } catch (err) {
      setActionError(getErrorMessage(err))
    } finally {
      setBusyId(null)
    }
  }

  const { data } = list

  return (
    <>
      <h1>Inventory</h1>

      <div className="filters card">
        <input type="search" placeholder="Search products" value={search} onChange={(e) => { setSearch(e.target.value); setPage(1) }} />
        <select value={categoryId} onChange={(e) => { setCategoryId(e.target.value); setPage(1) }}>
          <option value="">All categories</option>
          {(cats.data || []).map((c) => <option key={c.id} value={c.id}>{c.nameEn}</option>)}
        </select>
        <select value={filter} onChange={(e) => { setFilter(e.target.value); setPage(1) }}>
          {FILTERS.map(([value, label]) => <option key={value} value={value}>{label}</option>)}
        </select>
        <label className="inline">
          Low stock at or below
          <input type="number" min="1" max="1000" value={threshold} onChange={(e) => { setThreshold(Number(e.target.value) || 1); setPage(1) }} style={{ width: 80 }} />
        </label>
      </div>

      {actionError && <div className="alert alert-error">{actionError}</div>}
      {list.error && <ErrorBox message={list.error} onRetry={list.reload} />}
      {list.loading && !data && <Loading />}

      {data && (
        <>
          {data.items.length === 0 ? (
            <p className="muted">Nothing to show.</p>
          ) : (
            <div className="card table-wrap">
              <table className="table">
                <thead><tr><th>Product</th><th>Category</th><th className="right">Price</th><th>Status</th><th className="right">Stock</th></tr></thead>
                <tbody>
                  {data.items.map((i) => {
                    const edited = edits[i.productId]
                    const changed = edited !== undefined && String(edited) !== String(i.stockQuantity)
                    return (
                      <tr key={i.productId}>
                        <td>
                          <div className="cell-product">
                            {i.imageUrl ? <img src={i.imageUrl} alt="" /> : <span className="thumb-ph">🌿</span>}
                            <div>
                              <Link to={`/products/${i.productId}`} className="link">{i.nameEn}</Link>
                              <div className="muted small">{i.nameTe}{i.isActive ? '' : ' · hidden'}</div>
                            </div>
                          </div>
                        </td>
                        <td>{i.categoryNameEn}</td>
                        <td className="right">{formatPrice(i.discountPrice ?? i.price)}</td>
                        <td><StatusPill value={i.stockStatus} /></td>
                        <td className="right nowrap">
                          <input
                            type="number"
                            min="0"
                            step="1"
                            className="stock-input"
                            value={edited ?? i.stockQuantity}
                            onChange={(e) => setEdits((s) => ({ ...s, [i.productId]: e.target.value }))}
                          />{' '}
                          {changed && (
                            <button type="button" className="btn btn-primary btn-sm" disabled={busyId === i.productId} onClick={() => save(i)}>Save</button>
                          )}
                        </td>
                      </tr>
                    )
                  })}
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
