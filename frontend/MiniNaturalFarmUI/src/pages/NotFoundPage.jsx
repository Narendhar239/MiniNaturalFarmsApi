import { Link } from 'react-router-dom'
import { useTranslation } from 'react-i18next'

export default function NotFoundPage() {
  const { t } = useTranslation()
  return (
    <div className="center">
      <h1>404</h1>
      <p>{t('common.notFound')}</p>
      <Link to="/" className="btn btn-primary">{t('common.goHome')}</Link>
    </div>
  )
}
