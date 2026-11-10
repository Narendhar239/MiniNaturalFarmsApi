import { useEffect, useState } from 'react'
import { useSearchParams } from 'react-router-dom'
import { useTranslation } from 'react-i18next'
import { catalogApi } from '../api/services'
import { getErrorMessage } from '../api/client'
import { pick } from '../utils/format'
import ProductCard from '../components/ProductCard'
import Pagination from '../components/Pagination'
import { ErrorBox, Loading } from '../components/Status'

export default function ProductsPage() {
  const { t, i18n } = useTranslation()
  const [params, setParams] = useSearchParams()
  const queryString = params.toString()

  const [categories, setCategories] = useState([])
  const [data, setData] = useState(null)
  const [loading, setLoading] = useState(true)
  const [error, setError] = useState('')
  const [reload, setReload] = useState(0)

  const [search, setSearch] = useState(params.get('search') || '')
  const [minPrice, setMinPrice] = useState(params.get('minPrice') || '')
  const [maxPrice, setMaxPrice] = useState(params.get('maxPrice') || '')

  const categoryId = params.get('categoryId') || ''
  const sort = params.get('sort') || 'newest'
  const page = Number(params.get('page')) || 1

  useEffect(() => {
    catalogApi.categories().then(setCategories).catch(() => {})
  }, [])

  useEffect(() => {
    setSearch(params.get('search') || '')
    setMinPrice(params.get('minPrice') || '')
    setMaxPrice(params.get('maxPrice') || '')
  }, [queryString])

  useEffect(() => {
    let cancelled = false
    const p = new URLSearchParams(queryString)
    setLoading(true)
    setError('')

    catalogApi
      .products({
        search: p.get('search') || undefined,
        categoryId: p.get('categoryId') || undefined,
        minPrice: p.get('minPrice') || undefined,
        maxPrice: p.get('maxPrice') || undefined,
        sort: p.get('sort') || 'newest',
        page: Number(p.get('page')) || 1,
        pageSize: 12,
      })
      .then((res) => !cancelled && setData(res))
      .catch((e) => !cancelled && setError(getErrorMessage(e)))
      .finally(() => !cancelled && setLoading(false))

    return () => {
      cancelled = true
    }
  }, [queryString, reload])

  const update = (changes) => {
    const next = new URLSearchParams(params)
    Object.entries(changes).forEach(([key, value]) => {
      if (value === '' || value == null) next.delete(key)
      else next.set(key, value)
    })
    if (!('page' in changes)) next.delete('page')
    setParams(next)
  }

  const applyFilters = (e) => {
    e.preventDefault()
    update({ search: search.trim(), minPrice, maxPrice })
  }

  const changePage = (nextPage) => {
    update({ page: nextPage })
    window.scrollTo({ top: 0, behavior: 'smooth' })
  }

  return (
    <>
      <h1>{t('products.title')}</h1>

      <form className="filters" onSubmit={applyFilters}>
        <input
          type="search"
          value={search}
          onChange={(e) => setSearch(e.target.value)}
          placeholder={t('products.searchPlaceholder')}
          aria-label={t('products.searchPlaceholder')}
        />

        <select value={categoryId} onChange={(e) => update({ categoryId: e.target.value })} aria-label={t('products.allCategories')}>
          <option value="">{t('products.allCategories')}</option>
          {categories.map((c) => (
            <option key={c.id} value={c.id}>{pick(c, 'name', i18n.language)}</option>
          ))}
        </select>

        <select value={sort} onChange={(e) => update({ sort: e.target.value === 'newest' ? '' : e.target.value })} aria-label={t('products.sortBy')}>
          <option value="newest">{t('products.sortNewest')}</option>
          <option value="price_asc">{t('products.sortPriceAsc')}</option>
          <option value="price_desc">{t('products.sortPriceDesc')}</option>
          <option value="name">{t('products.sortName')}</option>
        </select>

        <input type="number" min="0" value={minPrice} onChange={(e) => setMinPrice(e.target.value)} placeholder={t('products.minPrice')} aria-label={t('products.minPrice')} />
        <input type="number" min="0" value={maxPrice} onChange={(e) => setMaxPrice(e.target.value)} placeholder={t('products.maxPrice')} aria-label={t('products.maxPrice')} />

        <button type="submit" className="btn btn-primary">{t('products.apply')}</button>
        <button type="button" className="btn btn-outline" onClick={() => setParams({})}>{t('products.clear')}</button>
      </form>

      {loading && <Loading />}
      {error && <ErrorBox message={error} onRetry={() => setReload((n) => n + 1)} />}

      {!loading && !error && data && (
        <>
          <p className="muted">{t('products.found', { count: data.totalCount })}</p>

          {data.items.length === 0 ? (
            <p className="center">{t('products.noResults')}</p>
          ) : (
            <div className="product-grid">
              {data.items.map((p) => <ProductCard key={p.id} product={p} />)}
            </div>
          )}

          <Pagination page={data.page} totalPages={data.totalPages} onChange={changePage} />
        </>
      )}
    </>
  )
}
