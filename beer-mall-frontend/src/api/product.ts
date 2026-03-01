// src/api/product.ts
import { request } from '@/api/request'

export function apiProductList() {
  return request<any[]>('/api/Product/list')
}

export function apiProductDetail(id: number) {
  return request<any>(`/api/Product/${id}`)
}
