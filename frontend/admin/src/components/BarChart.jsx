export default function BarChart({ data, format = (v) => v, height = 190 }) {
  if (!data || data.length === 0) return <p className="muted">No data for this period.</p>

  const max = Math.max(1, ...data.map((d) => d.value))
  const step = Math.ceil(data.length / 8)

  return (
    <div className="chart" style={{ height }}>
      {data.map((d, i) => (
        <div key={d.label} className="chart-col" title={`${d.label}: ${format(d.value)}`}>
          <div className="chart-bar-wrap">
            <div className="chart-bar" style={{ height: `${(d.value / max) * 100}%` }} />
          </div>
          <span className="chart-label">{i % step === 0 ? d.short ?? d.label : ''}</span>
        </div>
      ))}
    </div>
  )
}
