import { useEffect, useState } from 'react'
import { Link } from 'react-router-dom'
import { useTranslation } from 'react-i18next'
import { wishlistApi } from '../api/services'
import { getErrorMessage } from '../api/client'
import { useCart } from '../context/CartContext'
import { useWishlist } from '../context/WishlistContext'
import { formatPrice, pick } from '../utils/format'
import { ErrorBox, Loading } from '../components/Status'

export default function WishlistPage() {
  const { t, i18n } = useTranslation()
  const lang = i18n.language
  const { addItem } = useCart()
  const { toggle } = useWishlist()

  const [items, setItems] = useState([])
  const [loading, setLoading] = useState(true)
  const [error, setError] = useState('')
  const [actionError, setActionError] = useState('')
  const [notice, setNotice] = useState('')
  const [reload, setReload] = useState(0)

  useEffect(() => {
    let cancelled = false
    setLoading(true)
    setError('')

    wishlistApi
      .list()
      .then((res) => !cancelled && setItems(res))
      .catch((e) => !cancelled && setError(getErrorMessage(e)))
      .finally(() => !cancelled && setLoading(false))

    return () => {
      cancelled = true
    }
  }, [reload])

  const remove = async (productId) => {
    setActionError('')
    try {
      await toggle(productId)
      setItems((list) => list.filter((i) => i.productId !== productId))
    } catch (e) {
      setActionError(getErrorMessage(e))
    }
  }

  const addToCart = async (productId) => {
    setActionError('')
    setNotice('')
    try {
      await addItem(productId, 1)
      setNotice(t('product.added'))
    } catch (e) {
      setActionError(getErrorMessage(e))
    }
  }

  if (loading) return <Loading />
  if (error) return <ErrorBox message={error} onRetry={() => setReload((n) => n + 1)} />

  return (
    <>
      <h1>{t('wishlist.title')}</h1>
      {actionError && <div className="alert alert-error">{actionError}</div>}
      {notice && (
        <div className="alert alert-success">
          {notice} · <Link to="/cart" className="link">{t('product.goToCart')}</Link>
        </div>
      )}

      {items.length === 0 ? (
        <div className="center">
          <p className="muted">{t('wishlist.empty')}</p>
          <Link to="/products" className="btn btn-primary">{t('cart.continueShopping')}</Link>
        </div>
      ) : (
        <div className="product-grid">
          {items.map((item) => {
            const name = pick(item, 'name', lang)
            const hasDiscount = item.discountPrice != null
            return (
              <div key={item.productId} className="card product-card">
                <Link to={`/products/${item.productId}`} className="product-link">
                  <div className="product-image">
                    {item.imageUrl ? <img src={item.imageUrl} alt={name} loading="lazy" /> : <span className="img-placeholder">🌿</span>}
                    {!item.isAvailable && <span className="badge badge-out">{t('product.outOfStock')}</span>}
                  </div>
                  <div className="product-info">
                    <h3>{name}</h3>
                    <div className="price">
                      <strong>{formatPrice(hasDiscount ? item.discountPrice : item.price)}</strong>
                      {hasDiscount && <s>{formatPrice(item.price)}</s>}
                    </div>
                  </div>
                </Link>
                <div className="card-actions">
                  <button type="button" className="btn btn-primary btn-sm" disabled={!item.isAvailable} onClick={() => addToCart(item.productId)}>
                    {t('product.addToCart')}
                  </button>
                  <button type="button" className="btn btn-outline btn-sm" onClick={() => remove(item.productId)}>
                    {t('wishlist.remove')}
                  </button>
                </div>
              </div>
            )
          })}
        </div>
      )}
    </>
  )
}
