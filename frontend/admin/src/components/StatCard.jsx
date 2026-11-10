import { Link } from 'react-router-dom'

export default function StatCard({ label, value, hint, tone = '', to }) {
  const body = (
    <>
      <span className="stat-label">{label}</span>
      <strong className="stat-value">{value}</strong>
      {hint && <span className="stat-hint">{hint}</span>}
    </>
  )
  return to ? (
    <Link to={to} className={`card stat ${tone}`}>{body}</Link>
  ) : (
    <div className={`card stat ${tone}`}>{body}</div>
  )
}
