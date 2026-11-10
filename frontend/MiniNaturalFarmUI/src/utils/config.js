export const FREE_SHIPPING_THRESHOLD = 500
export const FLAT_SHIPPING = 40

export const shippingFor = (subTotal) =>
  subTotal <= 0 || subTotal >= FREE_SHIPPING_THRESHOLD ? 0 : FLAT_SHIPPING
