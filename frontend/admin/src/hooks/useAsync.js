import { useEffect, useState } from 'react'
import { getErrorMessage } from '../api/client'

/** Runs fn() whenever deps change. Keeps the previous data visible while reloading. */
export default function useAsync(fn, deps) {
  const [state, setState] = useState({ data: null, loading: true, error: '' })
  const [reloadKey, setReloadKey] = useState(0)

  useEffect(() => {
    let cancelled = false
    setState((s) => ({ ...s, loading: true, error: '' }))

    fn()
      .then((data) => !cancelled && setState({ data, loading: false, error: '' }))
      .catch((e) => !cancelled && setState({ data: null, loading: false, error: getErrorMessage(e) }))

    return () => {
      cancelled = true
    }
    // eslint-disable-next-line react-hooks/exhaustive-deps
  }, [...deps, reloadKey])

  return {
    ...state,
    reload: () => setReloadKey((k) => k + 1),
    setData: (data) => setState((s) => ({ ...s, data })),
  }
}
