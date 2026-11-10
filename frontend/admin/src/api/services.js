import client from './client'

const data = (r) => r.data

// Drop empty filters so they are not sent as "?search=&status="
const clean = (params = {}) =>
  Object.fromEntries(Object.entries(params).filter(([, v]) => v !== '' && v !== null && v !== undefined))

export const authApi = {
  login: (body) => client.post('/api/auth/login', body).then(data),
}

export const categoryApi = {
  list: () => client.get('/api/categories', { params: { includeInactive: true } }).then(data),
  create: (body) => client.post('/api/categories', body).then(data),
  update: (id, body) => client.put(`/api/categories/${id}`, body).then(data),
  remove: (id) => client.delete(`/api/categories/${id}`),
  uploadImage: (id, file) => {
    const form = new FormData()
    form.append('file', file)
    return client.post(`/api/categories/${id}/image`, form).then(data)
  },
}

export const productApi = {
  list: (params) => client.get('/api/products', { params: clean(params) }).then(data),
  get: (id) => client.get(`/api/products/${id}`).then(data),
  create: (body) => client.post('/api/products', body).then(data),
  update: (id, body) => client.put(`/api/products/${id}`, body).then(data),
  deactivate: (id) => client.delete(`/api/products/${id}`),
  addImages: (id, files) => {
    const form = new FormData()
    files.forEach((f) => form.append('files', f))
    return client.post(`/api/products/${id}/images`, form).then(data)
  },
  deleteImage: (id, imageId) => client.delete(`/api/products/${id}/images/${imageId}`).then(data),
  setPrimaryImage: (id, imageId) => client.put(`/api/products/${id}/images/${imageId}/primary`).then(data),
}

export const adminApi = {
  orders: {
    list: (p) => client.get('/api/admin/orders', { params: clean(p) }).then(data),
    get: (id) => client.get(`/api/admin/orders/${id}`).then(data),
    setStatus: (id, status) => client.put(`/api/admin/orders/${id}/status`, { status }).then(data),
    refund: (id) => client.post(`/api/admin/orders/${id}/refund`).then(data),
    invoice: (id) => client.get(`/api/orders/${id}/invoice`, { responseType: 'blob' }).then(data),
  },
  inventory: {
    list: (p) => client.get('/api/admin/inventory', { params: clean(p) }).then(data),
    setStock: (productId, quantity) => client.put(`/api/admin/inventory/${productId}/stock`, { quantity }).then(data),
  },
  customers: {
    list: (p) => client.get('/api/admin/customers', { params: clean(p) }).then(data),
    get: (id) => client.get(`/api/admin/customers/${id}`).then(data),
  },
  reports: {
    dashboard: () => client.get('/api/admin/reports/dashboard').then(data),
    sales: (p) => client.get('/api/admin/reports/sales', { params: clean(p) }).then(data),
    topProducts: (p) => client.get('/api/admin/reports/top-products', { params: clean(p) }).then(data),
    inventory: (p) => client.get('/api/admin/reports/inventory', { params: clean(p) }).then(data),
    customers: (p) => client.get('/api/admin/reports/customers', { params: clean(p) }).then(data),
    payments: (p) => client.get('/api/admin/reports/payments', { params: clean(p) }).then(data),
  },
}
