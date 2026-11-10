import { NavLink, Outlet } from 'react-router-dom'
import { useTranslation } from 'react-i18next'

export default function AccountLayout() {
  const { t } = useTranslation()
  return (
    <>
      <h1>{t('account.title')}</h1>
      <div className="tabs">
        <NavLink to="/account" end>{t('account.profile')}</NavLink>
        <NavLink to="/account/addresses">{t('account.addresses')}</NavLink>
      </div>
      <Outlet />
    </>
  )
}
