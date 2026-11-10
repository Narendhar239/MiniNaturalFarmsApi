import axios from 'axios'

const STORAGE_KEY = 'mnf_auth'

function readAuth() {
  try {
    const raw = localStorage.getItem(STORAGE_KEY)
    return raw ? JSON.parse(raw) : null
  } catch {
    return null
  }
}

export function loadAuth() {
  const auth = readAuth()
  return auth && auth.token && new Date(auth.expiresAt) > new Date() ? auth : null
}

export function saveAuth(auth) {
  localStorage.setItem(STORAGE_KEY, JSON.stringify(auth))
}

export function clearAuth() {
  localStorage.removeItem(STORAGE_KEY)
}

const client = axios.create({ baseURL: import.meta.env.VITE_API_URL || 'http://localhost:5033' })

client.interceptors.request.use((config) => {
  const auth = loadAuth()
  if (auth) {
    config.headers.Authorization = `Bearer ${auth.token}`
  } else if (readAuth()) {
    clearAuth()
    window.dispatchEvent(new Event('auth:logout'))
  }
  return config
})

client.interceptors.response.use(
  (response) => response,
  (error) => {
    if (error.response?.status === 401 && error.config?.headers?.Authorization) {
      clearAuth()
      window.dispatchEvent(new Event('auth:logout'))
    }
    return Promise.reject(error)
  }
)

export function getErrorMessage(error, fallback = 'Something went wrong. Please try again.') {
  if (!error?.response) return 'Cannot reach the server. Please check your connection.'
  const data = error.response.data
  if (typeof data === 'string' && data) return data
  if (Array.isArray(data?.errors)) return data.errors.join(' ')
  if (data?.errors && typeof data.errors === 'object') return Object.values(data.errors).flat().join(' ')
  return data?.detail || data?.title || fallback
}

export default client

