import { useTranslation } from 'react-i18next'

export default function StatusPill({ value, group = 'status' }) {
  const { t } = useTranslation()
  return <span className={`pill pill-${String(value).toLowerCase()}`}>{t(`${group}.${value}`, value)}</span>
}
