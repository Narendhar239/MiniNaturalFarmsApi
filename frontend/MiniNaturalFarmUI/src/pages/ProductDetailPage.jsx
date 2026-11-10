import { useEffect, useState } from 'react'
import { Link, useParams } from 'react-router-dom'
import { useTranslation } from 'react-i18next'
import { catalogApi } from '../api/services'
import { getErrorMessage } from '../api/client'
import useAuthGate from '../hooks/useAuthGate'
import { useCart } from '../context/CartContext'
import { useWishlist } from '../context/WishlistContext'
import { discountPercent, formatPrice, pick } from '../utils/format'
import { ErrorBox, Loading } from '../components/Status'

export default function ProductDetailPage() {
  const { id } = useParams()
  const { t, i18n } = useTranslation()
  const lang = i18n.language
  const ensureAuth = useAuthGate()
  const { addItem } = useCart()
  const { has, toggle } = useWishlist()

  const [product, setProduct] = useState(null)
  const [selected, setSelected] = useState(0)
  const [loading, setLoading] = useState(true)
  const [error, setError] = useState('')
  const [reload, setReload] = useState(0)

  const [qty, setQty] = useState(1)
  const [adding, setAdding] = useState(false)
  const [notice, setNotice] = useState('')
  const [actionError, setActionError] = useState('')

  useEffect(() => {
    let cancelled = false
    setLoading(true)
    setError('')
    setSelected(0)
    setQty(1)
    setNotice('')
    setActionError('')

    catalogApi
      .product(id)
      .then((res) => !cancelled && setProduct(res))
      .catch((e) => !cancelled && setError(getErrorMessage(e)))
      .finally(() => !cancelled && setLoading(false))

    return () => {
      cancelled = true
    }
  }, [id, reload])

  if (loading) return <Loading />

  if (error) {
    return (
      <>
        <ErrorBox message={error} onRetry={() => setReload((n) => n + 1)} />
        <Link to="/products" className="link">{t('product.back')}</Link>
      </>
    )
  }

  const name = pick(product, 'name', lang)
  const images = product.images || []
  const hasDiscount = product.discountPrice != null
  const price = hasDiscount ? product.discountPrice : product.price
  const stock = product.stockQuantity
  const maxQty = Math.max(1, Math.min(stock, 99))
  const liked = has(product.id)

  const handleAdd = async () => {
    if (!ensureAuth()) return
    setActionError('')
    setNotice('')
    setAdding(true)
    try {
      await addItem(product.id, qty)
      setNotice(t('product.added'))
    } catch (e) {
      setActionError(getErrorMessage(e))
    } finally {
      setAdding(false)
    }
  }

  const handleHeart = () => {
    if (!ensureAuth()) return
    toggle(product.id).catch(() => {})
  }

  return (
    <>
      <Link to="/products" className="link">← {t('product.back')}</Link>

      <div className="detail">
        <div>
          <div className="detail-main-image">
            {images.length > 0 ? <img src={images[selected]?.url} alt={name} /> : <span className="img-placeholder">🌿</span>}
          </div>
          {images.length > 1 && (
            <div className="thumbs">
              {images.map((img, index) => (
                <button type="button" key={img.id} className={index === selected ? 'active' : ''} onClick={() => setSelected(index)}>
                  <img src={img.url} alt="" />
                </button>
              ))}
            </div>
          )}
        </div>

        <div>
          <h1>{name}</h1>
          <p className="muted">{t('product.category')}: {pick({ nameEn: product.categoryNameEn, nameTe: product.categoryNameTe }, 'name', lang)}</p>

          <div className="price price-lg">
            <strong>{formatPrice(price)}</strong>
            {hasDiscount && (
              <>
                <s>{formatPrice(product.price)}</s>
                <span className="badge badge-sale static">{t('product.percentOff', { percent: discountPercent(product) })}</span>
              </>
            )}
          </div>

          <p className={stock > 0 ? 'stock in' : 'stock out'}>
            {stock <= 0 ? t('product.outOfStock') : stock <= 10 ? t('product.onlyLeft', { count: stock }) : t('product.inStock')}
          </p>

          {actionError && <div className="alert alert-error">{actionError}</div>}
          {notice && (
            <div className="alert alert-success">
              {notice} · <Link to="/cart" className="link">{t('product.goToCart')}</Link>
            </div>
          )}

          <div className="buy-row">
            <div className="qty" aria-label={t('product.quantity')}>
              <button type="button" onClick={() => setQty((q) => Math.max(1, q - 1))} disabled={qty <= 1 || stock <= 0}>−</button>
              <span>{qty}</span>
              <button type="button" onClick={() => setQty((q) => Math.min(maxQty, q + 1))} disabled={qty >= maxQty || stock <= 0}>+</button>
            </div>
            <button type="button" className="btn btn-primary" onClick={handleAdd} disabled={stock <= 0 || adding}>
              {t('product.addToCart')}
            </button>
            <button
              type="button"
              className={`btn btn-outline ${liked ? 'liked' : ''}`}
              onClick={handleHeart}
              title={liked ? t('product.removeFromWishlist') : t('product.addToWishlist')}
              aria-label={liked ? t('product.removeFromWishlist') : t('product.addToWishlist')}
            >
              {liked ? '♥' : '♡'}
            </button>
          </div>

          <h3>{t('product.description')}</h3>
          <p className="description">{pick(product, 'description', lang)}</p>
        </div>
      </div>
    </>
  )
}
