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

export function apiGetCart(userId: number) {
  return request<CartResp>(`/api/Cart?userId=${userId}`)
}

// Swagger: POST /api/Cart/add  body: AddCartDto { userId, productId, skuId, quantity }
// 说明：后端支持 quantity 正/负（正数加、负数减，减到 0 则删除）
export function apiCartAdd(payload: {
  userId: number
  productId: number
  skuId: number
  quantity: number
}) {
  return request<CartResp>('/api/Cart/add', { method: 'POST', data: payload })
}

// Swagger: DELETE /api/Cart/clear?userId=
export function apiClearCart(userId: number) {
  return request<any>(`/api/Cart/clear?userId=${userId}`, { method: 'DELETE' })
}
