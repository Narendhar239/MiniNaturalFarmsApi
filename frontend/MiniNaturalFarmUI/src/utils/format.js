const inr = new Intl.NumberFormat('en-IN', {
  style: 'currency',
  currency: 'INR',
  minimumFractionDigits: 0,
  maximumFractionDigits: 2,
})

export const formatPrice = (value) => inr.format(value)

export const formatDate = (iso, lang) =>
  new Date(iso).toLocaleString(lang === 'te' ? 'te-IN' : 'en-IN', { dateStyle: 'medium', timeStyle: 'short' })

export const pick = (item, field, lang) => {
  const en = item[`${field}En`]
  const te = item[`${field}Te`]
  return (lang === 'te' ? te || en : en || te) || ''
}

export const primaryImage = (product) => product.images?.[0]?.url || null

export const discountPercent = (product) =>
  product.discountPrice != null && product.price > 0
    ? Math.round((1 - product.discountPrice / product.price) * 100)
    : 0
