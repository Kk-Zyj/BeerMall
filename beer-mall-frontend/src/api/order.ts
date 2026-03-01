// src/api/order.ts
import { request } from '@/api/request'

export function apiCreateOrder(dto: any) {
  return request<any>('/api/Order/create', { method: 'POST', data: dto })
}

export function apiOrderList(userId: number, type = 0) {
  return request<any>(`/api/Order/list?userId=${userId}&type=${type}`)
}

export function apiOrderDetail(id: number, userId: number) {
  return request<any>(`/api/Order/${id}?userId=${userId}`)
}

export function apiOrderPay(id: number) {
  return request<any>(`/api/Order/${id}/pay`, { method: 'POST' })
}

export function apiOrderCancel(id: number) {
  return request<any>(`/api/Order/${id}/cancel`, { method: 'POST' })
}
