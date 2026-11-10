import { Link } from 'react-router-dom'
import { useTranslation } from 'react-i18next'
import useAuthGate from '../hooks/useAuthGate'
import { useWishlist } from '../context/WishlistContext'
import { discountPercent, formatPrice, pick, primaryImage } from '../utils/format'

export default function ProductCard({ product }) {
  const { t, i18n } = useTranslation()
  const ensureAuth = useAuthGate()
  const { has, toggle } = useWishlist()

  const name = pick(product, 'name', i18n.language)
  const image = primaryImage(product)
  const hasDiscount = product.discountPrice != null
  const price = hasDiscount ? product.discountPrice : product.price
  const outOfStock = product.stockQuantity <= 0
  const liked = has(product.id)

  const onHeart = () => {
    if (!ensureAuth()) return
    toggle(product.id).catch(() => {})
  }

  return (
    <div className="card product-card">
      <Link to={`/products/${product.id}`} className="product-link">
        <div className="product-image">
          {image ? <img src={image} alt={name} loading="lazy" /> : <span className="img-placeholder">🌿</span>}
          {hasDiscount && <span className="badge badge-sale">{t('product.percentOff', { percent: discountPercent(product) })}</span>}
          {outOfStock && <span className="badge badge-out">{t('product.outOfStock')}</span>}
        </div>
        <div className="product-info">
          <h3>{name}</h3>
          <div className="price">
            <strong>{formatPrice(price)}</strong>
            {hasDiscount && <s>{formatPrice(product.price)}</s>}
          </div>
        </div>
      </Link>

      <button
        type="button"
        className={`heart ${liked ? 'active' : ''}`}
        onClick={onHeart}
        aria-label={liked ? t('product.removeFromWishlist') : t('product.addToWishlist')}
        title={liked ? t('product.removeFromWishlist') : t('product.addToWishlist')}
      >
        {liked ? '♥' : '♡'}
      </button>
    </div>
  )
}
