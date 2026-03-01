// src/api/auth.ts
import { request } from '@/api/request'

export interface LoginResp {
  userId?: number
  id?: number
  mobile?: string
  isMobileVerified?: boolean
  success?: boolean
}

export function apiLogin(code: string) {
  return request<LoginResp>('/api/Auth/login', {
    method: 'POST',
    data: { code },
  })
}

export function apiBindMobile(payload: { userId: number; code: string }) {
  return request<LoginResp & { mobile?: string; success?: boolean }>(
    '/api/Auth/bind-mobile',
    { method: 'POST', data: payload },
  )
}
