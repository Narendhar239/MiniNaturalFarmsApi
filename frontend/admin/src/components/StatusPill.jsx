const LABELS = { OutOfStock: 'Out of stock', InStock: 'In stock', Low: 'Low stock', Cod: 'COD' }

export default function StatusPill({ value }) {
  return <span className={`pill pill-${String(value).toLowerCase()}`}>{LABELS[value] ?? value}</span>
}
