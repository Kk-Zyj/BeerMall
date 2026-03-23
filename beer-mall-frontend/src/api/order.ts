// src/api/order.ts
import { request } from '@/api/request'

export function apiCreateOrder(dto: any) {
  return request<any>('/api/Order/create', { method: 'POST', data: dto })
}

export function apiOrderList(type = 0) {
  return request<any>(`/api/Order/list?type=${type}`)
}

export function apiOrderDetail(id: number) {
  return request<any>(`/api/Order/${id}`)
}

export function apiOrderPay(id: number) {
  return request<any>(`/api/Order/${id}/pay`, { method: 'POST' })
}

export function apiOrderCancel(id: number) {
  return request<any>(`/api/Order/${id}/cancel`, { method: 'POST' })
}

export function apiConfirmReceipt(id: number) {
  return request<any>(`/api/Order/${id}/confirm-receipt`, {
    method: 'POST',
  })
}
