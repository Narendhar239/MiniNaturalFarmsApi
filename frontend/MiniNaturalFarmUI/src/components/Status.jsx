import { useTranslation } from 'react-i18next'

export function Loading() {
  const { t } = useTranslation()
  return <p className="muted center">{t('common.loading')}</p>
}

export function ErrorBox({ message, onRetry }) {
  const { t } = useTranslation()
  return (
    <div className="alert alert-error">
      <p>{message || t('common.error')}</p>
      {onRetry && (
        <button type="button" className="btn btn-outline btn-sm" onClick={onRetry}>
          {t('common.retry')}
        </button>
      )}
    </div>
  )
}
