import { ordersApi } from '../api/services'

let scriptPromise = null

function loadScript() {
  if (window.Razorpay) return Promise.resolve(true)
  if (!scriptPromise) {
    scriptPromise = new Promise((resolve) => {
      const script = document.createElement('script')
      script.src = 'https://checkout.razorpay.com/v1/checkout.js'
      script.onload = () => resolve(true)
      script.onerror = () => {
        scriptPromise = null
        resolve(false)
      }
      document.body.appendChild(script)
    })
  }
  return scriptPromise
}

/**
 * Opens Razorpay Checkout and resolves with one of:
 * { status: 'paid', order } | { status: 'dismissed' } | { status: 'verify-failed' } | { status: 'script-error' }
 */
export async function payForOrder({ orderId, payment, user }) {
  const loaded = await loadScript()
  if (!loaded) return { status: 'script-error' }

  return new Promise((resolve) => {
    const rzp = new window.Razorpay({
      key: payment.keyId,
      amount: payment.amountPaise,
      currency: payment.currency,
      name: 'Mini Natural Farms',
      description: `Order ${payment.orderNumber}`,
      order_id: payment.razorpayOrderId,
      prefill: { name: user?.fullName, email: user?.email },
      theme: { color: '#2e7d32' },
      handler: async (response) => {
        try {
          const order = await ordersApi.verifyPayment(orderId, {
            razorpayOrderId: response.razorpay_order_id,
            razorpayPaymentId: response.razorpay_payment_id,
            razorpaySignature: response.razorpay_signature,
          })
          resolve({ status: 'paid', order })
        } catch {
          resolve({ status: 'verify-failed' })
        }
      },
      modal: { ondismiss: () => resolve({ status: 'dismissed' }) },
    })
    rzp.open()
  })
}
