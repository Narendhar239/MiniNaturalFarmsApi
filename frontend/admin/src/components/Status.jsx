export function Loading() {
  return <div className="loading">Loading…</div>
}

export function ErrorBox({ message, onRetry }) {
  return (
    <div className="alert alert-error">
      {message}
      {onRetry && (
        <>
          {' '}
          <button type="button" className="link-btn" onClick={onRetry}>Try again</button>
        </>
      )}
    </div>
  )
}
