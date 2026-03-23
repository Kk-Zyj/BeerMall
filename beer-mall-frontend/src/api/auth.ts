// src/api/auth.ts
import { request } from '@/api/request'

export interface LoginResp {
  token?: string
  userId?: number
  nickName?: string
  avatarUrl?: string
  mobile?: string
  isMobileVerified?: boolean
  user?: {
    id: number
    nickName?: string
    avatarUrl?: string
    mobile?: string
    isMobileVerified?: boolean
  }
}

export function apiLogin(code: string) {
  return request<LoginResp>('/api/Auth/login', {
    method: 'POST',
    data: { code },
  })
}

export function apiBindMobile(payload: { code: string }) {
  return request<
    LoginResp & {
      success?: boolean
    }
  >('/api/Auth/bind-mobile', {
    method: 'POST',
    data: payload,
  })
}
