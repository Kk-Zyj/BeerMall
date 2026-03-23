// src/api/cart.ts
import { request } from '@/api/request'

export type CartItem = {
  itemId: number
  productId: number
  productName: string
  skuId: number
  specName: string
  price: number
  quantity: number
  image: string
}

export type CartResp = {
  items: CartItem[]
  totalCount: number
  totalPrice: number
}

export function apiGetCart() {
  return request<CartResp>('/api/Cart')
}

export function apiCartAdd(payload: {
  productId: number
  skuId: number
  quantity: number
}) {
  return request<CartResp>('/api/Cart/add', { method: 'POST', data: payload })
}

export function apiClearCart() {
  return request<any>('/api/Cart/clear', { method: 'DELETE' })
}
