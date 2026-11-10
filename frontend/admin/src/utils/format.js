const inr = new Intl.NumberFormat('en-IN', {
  style: 'currency',
  currency: 'INR',
  minimumFractionDigits: 0,
  maximumFractionDigits: 2,
})

export const formatPrice = (v) => inr.format(v ?? 0)
export const formatNumber = (v) => new Intl.NumberFormat('en-IN').format(v ?? 0)

export const formatDateTime = (iso) =>
  iso ? new Date(iso).toLocaleString('en-IN', { dateStyle: 'medium', timeStyle: 'short' }) : '—'

export const formatDate = (iso) =>
  iso ? new Date(iso).toLocaleDateString('en-IN', { dateStyle: 'medium' }) : '—'

// yyyy-MM-dd in the browser's local time (what <input type="date"> and the API expect)
export const toIsoDate = (d) => d.toLocaleDateString('en-CA')
export const today = () => toIsoDate(new Date())
export const daysAgo = (n) => {
  const d = new Date()
  d.setDate(d.getDate() - n)
  return toIsoDate(d)
}
export const monthStart = () => {
  const d = new Date()
  d.setDate(1)
  return toIsoDate(d)
}
