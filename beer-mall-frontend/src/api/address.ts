// src/api/address.ts
import { request } from '@/api/request'

export function apiAddressList() {
  return request<any[]>('/api/Address')
}

export function apiAddressCreate(dto: any) {
  return request<any>('/api/Address', { method: 'POST', data: dto })
}

export function apiAddressDetail(id: number) {
  return request<any>(`/api/Address/${id}`)
}

export function apiAddressDelete(id: number) {
  return request<any>(`/api/Address/${id}`, { method: 'DELETE' })
}