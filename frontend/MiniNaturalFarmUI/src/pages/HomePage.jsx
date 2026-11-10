import { useEffect, useState } from 'react'
import { Link } from 'react-router-dom'
import { useTranslation } from 'react-i18next'
import { catalogApi } from '../api/services'
import { getErrorMessage } from '../api/client'
import { pick } from '../utils/format'
import ProductCard from '../components/ProductCard'
import { ErrorBox, Loading } from '../components/Status'

export default function HomePage() {
  const { t, i18n } = useTranslation()
  const [categories, setCategories] = useState([])
  const [products, setProducts] = useState([])
  const [loading, setLoading] = useState(true)
  const [error, setError] = useState('')
  const [reload, setReload] = useState(0)

  useEffect(() => {
    let cancelled = false
    setLoading(true)
    setError('')

    Promise.all([
      catalogApi.categories(),
      catalogApi.products({ sort: 'newest', page: 1, pageSize: 8 }),
    ])
      .then(([cats, prods]) => {
        if (cancelled) return
        setCategories(cats)
        setProducts(prods.items)
      })
      .catch((e) => !cancelled && setError(getErrorMessage(e)))
      .finally(() => !cancelled && setLoading(false))

    return () => {
      cancelled = true
    }
  }, [reload])

  return (
    <>
      <section className="hero">
        <h1>{t('home.heroTitle')}</h1>
        <p>{t('home.heroSubtitle')}</p>
        <Link to="/products" className="btn btn-light">{t('home.shopNow')}</Link>
      </section>

      {loading && <Loading />}
      {error && <ErrorBox message={error} onRetry={() => setReload((n) => n + 1)} />}

      {!loading && !error && (
        <>
          {categories.length > 0 && (
            <section className="section">
              <h2>{t('home.shopByCategory')}</h2>
              <div className="category-grid">
                {categories.map((c) => (
                  <Link key={c.id} to={`/products?categoryId=${c.id}`} className="card category-card">
                    {c.imageUrl ? <img src={c.imageUrl} alt={pick(c, 'name', i18n.language)} loading="lazy" /> : <span className="img-placeholder">🌿</span>}
                    <h3>{pick(c, 'name', i18n.language)}</h3>
                    <span className="muted">{t('home.itemsCount', { count: c.productCount })}</span>
                  </Link>
                ))}
              </div>
            </section>
          )}

          {products.length > 0 && (
            <section className="section">
              <div className="section-head">
                <h2>{t('home.newArrivals')}</h2>
                <Link to="/products" className="link">{t('home.viewAll')}</Link>
              </div>
              <div className="product-grid">
                {products.map((p) => <ProductCard key={p.id} product={p} />)}
              </div>
            </section>
          )}
        </>
      )}
    </>
  )
}
