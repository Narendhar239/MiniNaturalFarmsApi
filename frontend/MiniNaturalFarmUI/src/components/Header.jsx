import { Link, NavLink, useNavigate } from 'react-router-dom'
import { useTranslation } from 'react-i18next'
import { useAuth } from '../context/AuthContext'
import { useCart } from '../context/CartContext'
import { useWishlist } from '../context/WishlistContext'

export default function Header() {
  const { t, i18n } = useTranslation()
  const { user, isAuthenticated, logout } = useAuth()
  const { cart } = useCart()
  const { count: wishCount } = useWishlist()
  const navigate = useNavigate()

  const handleLogout = () => {
    logout()
    navigate('/')
  }

  return (
    <header className="header">
      <div className="container header-inner">
        <Link to="/" className="brand">🌿 Mini Natural Farms</Link>

        <nav className="nav">
          <NavLink to="/" end>{t('nav.home')}</NavLink>
          <NavLink to="/products">{t('nav.products')}</NavLink>
          {isAuthenticated && <NavLink to="/orders">{t('nav.orders')}</NavLink>}
          {isAuthenticated && <NavLink to="/account">{t('nav.account')}</NavLink>}
        </nav>

        <div className="header-actions">
          <div className="lang-switch" role="group" aria-label={t('nav.language')}>
            <button type="button" className={i18n.language === 'en' ? 'active' : ''} onClick={() => i18n.changeLanguage('en')}>EN</button>
            <button type="button" className={i18n.language === 'te' ? 'active' : ''} onClick={() => i18n.changeLanguage('te')}>తె</button>
          </div>

          <Link to="/wishlist" className="icon-link" title={t('nav.wishlist')}>
            ♥ {wishCount > 0 && <span className="count">{wishCount}</span>}
          </Link>
          <Link to="/cart" className="icon-link" title={t('nav.cart')}>
            🛒 {cart.totalQuantity > 0 && <span className="count">{cart.totalQuantity}</span>}
          </Link>

          {isAuthenticated ? (
            <>
              <span className="hello">{t('nav.hello', { name: user.fullName })}</span>
              <button type="button" className="btn btn-outline btn-sm" onClick={handleLogout}>{t('nav.logout')}</button>
            </>
          ) : (
            <>
              <Link to="/login" className="btn btn-outline btn-sm">{t('nav.login')}</Link>
              <Link to="/register" className="btn btn-primary btn-sm">{t('nav.register')}</Link>
            </>
          )}
        </div>
      </div>
    </header>
  )
}
