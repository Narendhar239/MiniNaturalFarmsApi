import { useState } from 'react'
import { Link } from 'react-router-dom'
import { categoryApi, productApi } from '../api/services'
import { getErrorMessage } from '../api/client'
import useAsync from '../hooks/useAsync'
import useDebounce from '../hooks/useDebounce'
import Pagination from '../components/Pagination'
import StatusPill from '../components/StatusPill'
import { ErrorBox, Loading } from '../components/Status'
import { formatPrice } from '../utils/format'

export default function ProductsPage() {
  const [search, setSearch] = useState('')
  const q = useDebounce(search, 400)
  const [categoryId, setCategoryId] = useState('')
  const [page, setPage] = useState(1)
  const [busyId, setBusyId] = useState(null)
  const [actionError, setActionError] = useState('')

  const cats = useAsync(() => categoryApi.list(), [])
  const list = useAsync(
    () => productApi.list({ search: q, categoryId, includeInactive: true, page, pageSize: 20 }),
    [q, categoryId, page]
  )

  const toggleActive = async (p) => {
    setActionError('')
    setBusyId(p.id)
    try {
      if (p.isActive) {
        if (!window.confirm(`Hide "${p.nameEn}" from the shop?`)) return
        await productApi.deactivate(p.id)
      } else {
        await productApi.update(p.id, {
          nameEn: p.nameEn,
          nameTe: p.nameTe,
          descriptionEn: p.descriptionEn,
          descriptionTe: p.descriptionTe,
          price: p.price,
          discountPrice: p.discountPrice,
          stockQuantity: p.stockQuantity,
          categoryId: p.categoryId,
          isActive: true,
        })
      }
      list.reload()
    } catch (e) {
      setActionError(getErrorMessage(e))
    } finally {
      setBusyId(null)
    }
  }

  const { data } = list

  return (
    <>
      <div className="section-head">
        <h1>Products</h1>
        <Link to="/products/new" className="btn btn-primary">+ New product</Link>
      </div>

      <div className="filters card">
        <input type="search" placeholder="Search by name (English or Telugu)" value={search} onChange={(e) => { setSearch(e.target.value); setPage(1) }} />
        <select value={categoryId} onChange={(e) => { setCategoryId(e.target.value); setPage(1) }}>
          <option value="">All categories</option>
          {(cats.data || []).map((c) => <option key={c.id} value={c.id}>{c.nameEn}</option>)}
        </select>
      </div>

      {actionError && <div className="alert alert-error">{actionError}</div>}
      {list.error && <ErrorBox message={list.error} onRetry={list.reload} />}
      {list.loading && !data && <Loading />}

      {data && (
        <>
          {data.items.length === 0 ? (
            <p className="muted">No products found.</p>
          ) : (
            <div className="card table-wrap">
              <table className="table">
                <thead>
                  <tr><th>Product</th><th>Category</th><th className="right">Price</th><th className="right">Stock</th><th>Status</th><th /></tr>
                </thead>
                <tbody>
                  {data.items.map((p) => (
                    <tr key={p.id}>
                      <td>
                        <div className="cell-product">
                          {p.images[0] ? <img src={p.images[0].url} alt="" /> : <span className="thumb-ph">🌿</span>}
                          <div>
                            <Link to={`/products/${p.id}`} className="link">{p.nameEn}</Link>
                            <div className="muted small">{p.nameTe}</div>
                          </div>
                        </div>
                      </td>
                      <td>{p.categoryNameEn}</td>
                      <td className="right">
                        {formatPrice(p.discountPrice ?? p.price)}
                        {p.discountPrice != null && <div className="muted small"><s>{formatPrice(p.price)}</s></div>}
                      </td>
                      <td className="right">{p.stockQuantity}</td>
                      <td><StatusPill value={p.isActive ? 'Active' : 'Inactive'} /></td>
                      <td className="right nowrap">
                        <Link to={`/products/${p.id}`} className="btn btn-outline btn-sm">Edit</Link>{' '}
                        <button type="button" className="btn btn-outline btn-sm" disabled={busyId === p.id} onClick={() => toggleActive(p)}>
                          {p.isActive ? 'Hide' : 'Show'}
                        </button>
                      </td>
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
