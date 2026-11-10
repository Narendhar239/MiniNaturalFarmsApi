import { useTranslation } from 'react-i18next'

export default function Footer() {
  const { t } = useTranslation()
  return (
    <footer className="footer">
      <div className="container">
        <p>© {new Date().getFullYear()} Mini Natural Farms. {t('common.rights')}</p>
      </div>
    </footer>
  )
}
