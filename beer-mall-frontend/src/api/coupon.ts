// src/api/coupon.ts
import { request } from '@/api/request'

export function apiActiveCoupons() {
  return request<any[]>('/api/app/coupon/active')
}

export function apiReceiveCoupon(id: number, userId: number) {
  return request<any>(`/api/app/coupon/${id}/receive?userId=${userId}`, {
    method: 'POST',
  })
}

export function apiMyAvailableCoupons(userId: number, orderAmount: number) {
  return request<any[]>(
    `/api/app/coupon/my/available?userId=${userId}&orderAmount=${orderAmount}`,
  )
}
