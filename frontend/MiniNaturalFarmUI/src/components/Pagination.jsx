import { useTranslation } from 'react-i18next'

export default function Pagination({ page, totalPages, onChange }) {
  const { t } = useTranslation()
  if (totalPages <= 1) return null

  return (
    <div className="pagination">
      <button type="button" className="btn btn-outline btn-sm" disabled={page <= 1} onClick={() => onChange(page - 1)}>
        {t('products.prev')}
      </button>
      <span>{t('products.pageOf', { page, total: totalPages })}</span>
      <button type="button" className="btn btn-outline btn-sm" disabled={page >= totalPages} onClick={() => onChange(page + 1)}>
        {t('products.next')}
      </button>
    </div>
  )
}
