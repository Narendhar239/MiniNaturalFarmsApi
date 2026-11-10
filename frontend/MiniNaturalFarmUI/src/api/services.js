import client from './client'

const data = (r) => r.data

export const authApi = {
  register: (body) => client.post('/api/auth/register', body).then(data),
  login: (body) => client.post('/api/auth/login', body).then(data),
}

export const catalogApi = {
  categories: () => client.get('/api/categories').then(data),
  products: (params) => client.get('/api/products', { params }).then(data),
  product: (id) => client.get(`/api/products/${id}`).then(data),
}

export const cartApi = {
  get: () => client.get('/api/cart').then(data),
  addItem: (productId, quantity) => client.post('/api/cart/items', { productId, quantity }).then(data),
  setQuantity: (productId, quantity) => client.put(`/api/cart/items/${productId}`, { quantity }).then(data),
  removeItem: (productId) => client.delete(`/api/cart/items/${productId}`).then(data),
}

export const wishlistApi = {
  list: () => client.get('/api/wishlist').then(data),
  ids: () => client.get('/api/wishlist/ids').then(data),
  add: (productId) => client.post(`/api/wishlist/${productId}`),
  remove: (productId) => client.delete(`/api/wishlist/${productId}`),
}

export const addressApi = {
  list: () => client.get('/api/addresses').then(data),
  create: (body) => client.post('/api/addresses', body).then(data),
  update: (id, body) => client.put(`/api/addresses/${id}`, body).then(data),
  setDefault: (id) => client.put(`/api/addresses/${id}/default`).then(data),
  remove: (id) => client.delete(`/api/addresses/${id}`),
}

export const profileApi = {
  get: () => client.get('/api/profile').then(data),
  update: (body) => client.put('/api/profile', body).then(data),
  changePassword: (body) => client.post('/api/profile/change-password', body),
}

export const ordersApi = {
  checkout: (body) => client.post('/api/orders/checkout', body).then(data),
  list: (page, pageSize = 10) => client.get('/api/orders', { params: { page, pageSize } }).then(data),
  get: (id) => client.get(`/api/orders/${id}`).then(data),
  cancel: (id) => client.post(`/api/orders/${id}/cancel`).then(data),
  payment: (id) => client.post(`/api/orders/${id}/payment`).then(data),
  verifyPayment: (id, body) => client.post(`/api/orders/${id}/payment/verify`, body).then(data),
  invoice: (id) => client.get(`/api/orders/${id}/invoice`, { responseType: 'blob' }).then(data),
}
