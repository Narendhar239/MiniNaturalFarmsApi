import { useEffect, useState } from 'react'
import { Link } from 'react-router-dom'
import { useTranslation } from 'react-i18next'
import { ordersApi } from '../api/services'
import { getErrorMessage } from '../api/client'
import { formatDate, formatPrice } from '../utils/format'
import Pagination from '../components/Pagination'
import StatusPill from '../components/StatusPill'
import { ErrorBox, Loading } from '../components/Status'

export default function OrdersPage() {
  const { t, i18n } = useTranslation()
  const [page, setPage] = useState(1)
  const [data, setData] = useState(null)
  const [loading, setLoading] = useState(true)
  const [error, setError] = useState('')
  const [reload, setReload] = useState(0)

  useEffect(() => {
    let cancelled = false
    setLoading(true)
    setError('')

    ordersApi
      .list(page)
      .then((res) => !cancelled && setData(res))
      .catch((e) => !cancelled && setError(getErrorMessage(e)))
      .finally(() => !cancelled && setLoading(false))

    return () => {
      cancelled = true
    }
  }, [page, reload])

  return (
    <>
      <h1>{t('orders.title')}</h1>

      {loading && <Loading />}
      {error && <ErrorBox message={error} onRetry={() => setReload((n) => n + 1)} />}

      {!loading && !error && data && (
        <>
          {data.items.length === 0 ? (
            <div className="center">
              <p className="muted">{t('orders.empty')}</p>
              <Link to="/products" className="btn btn-primary">{t('cart.continueShopping')}</Link>
            </div>
          ) : (
            data.items.map((o) => (
              <div key={o.id} className="card order-card">
                <div>
                  <strong>{o.orderNumber}</strong>
                  <p className="muted small">{formatDate(o.createdAt, i18n.language)} · {t('orders.itemsCount', { count: o.itemCount })}</p>
                  <div className="pills">
                    <StatusPill value={o.status} />
                    <StatusPill value={o.paymentStatus} group="payStatus" />
                    <span className="muted small">{t(`payMethod.${o.paymentMethod}`, o.paymentMethod)}</span>
                  </div>
                </div>
                <div className="order-side">
                  <strong>{formatPrice(o.totalAmount)}</strong>
                  <Link to={`/orders/${o.id}`} className="btn btn-outline btn-sm">{t('orders.view')}</Link>
                </div>
              </div>
            ))
          )}

          <Pagination page={data.page} totalPages={data.totalPages} onChange={(p) => { setPage(p); window.scrollTo({ top: 0, behavior: 'smooth' }) }} />
        </>
      )}
    </>
  )
}
