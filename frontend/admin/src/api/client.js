import axios from 'axios'

const STORAGE_KEY = 'mnf_admin_auth'

export const loadAuth = () => {
  try {
    const raw = localStorage.getItem(STORAGE_KEY)
    if (!raw) return null
    const auth = JSON.parse(raw)
    if (!auth?.token || !auth.user) return null
    if (auth.expiresAt && new Date(auth.expiresAt).getTime() <= Date.now()) return null
    return auth
  } catch {
    return null
  }
}

export const saveAuth = (auth) => localStorage.setItem(STORAGE_KEY, JSON.stringify(auth))
export const clearAuth = () => localStorage.removeItem(STORAGE_KEY)

const client = axios.create({
  baseURL: import.meta.env.VITE_API_URL || 'http://localhost:5033',
})

client.interceptors.request.use((config) => {
  const auth = loadAuth()
  if (auth?.token) config.headers.Authorization = `Bearer ${auth.token}`
  return config
})

client.interceptors.response.use(
  (response) => response,
  (error) => {
    // A logged-in session that the server no longer accepts (expired token)
    if (error.response?.status === 401 && loadAuth()) {
      clearAuth()
      window.dispatchEvent(new Event('auth:logout'))
    }
    return Promise.reject(error)
  }
)

export function getErrorMessage(error) {
  if (!error?.response) return 'Cannot reach the server. Is the API running (http profile)?'

  const data = error.response.data
  if (typeof data === 'string' && data) return data
  if (data?.errors) {
    const first = Object.values(data.errors).flat()[0]
    if (first) return first
  }
  if (data?.detail) return data.detail
  if (data?.title) return data.title
  if (error.response.status === 403) return 'You do not have permission to do this.'
  return 'Something went wrong. Please try again.'
}

export async function getBlobErrorMessage(error) {
  const data = error?.response?.data
  if (data instanceof Blob) {
    try {
      const body = JSON.parse(await data.text())
      return body.detail || body.title || getErrorMessage(error)
    } catch {
      return getErrorMessage(error)
    }
  }
  return getErrorMessage(error)
}

export default client
