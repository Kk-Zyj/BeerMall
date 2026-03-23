// src/api/request.ts
import { API_BASE_URL } from '@/config/api'

type UniRequestMethod = 'GET' | 'POST' | 'PUT' | 'DELETE'

const TOKEN_KEY = 'token'

export interface ApiEnvelope<T> {
  code: number
  message: string
  data: T
  traceId?: string
}

export class ApiError extends Error {
  code?: number
  statusCode?: number
  traceId?: string

  constructor(
    message: string,
    opts?: { code?: number; statusCode?: number; traceId?: string },
  ) {
    super(message)
    this.name = 'ApiError'
    this.code = opts?.code
    this.statusCode = opts?.statusCode
    this.traceId = opts?.traceId
  }
}

function joinUrl(base: string, path: string) {
  if (!path) return base
  if (path.startsWith('http')) return path
  if (base.endsWith('/') && path.startsWith('/')) return base + path.slice(1)
  if (!base.endsWith('/') && !path.startsWith('/')) return base + '/' + path
  return base + path
}

function isPlainObject(v: any): v is Record<string, any> {
  return (
    v !== null &&
    typeof v === 'object' &&
    Object.prototype.toString.call(v) === '[object Object]'
  )
}

function hasOwn(obj: any, key: string) {
  return Object.prototype.hasOwnProperty.call(obj, key)
}

function showError(msg: string, silent?: boolean) {
  if (silent) return
  try {
    uni.showToast({ title: msg, icon: 'none' })
  } catch {
    // ignore
  }
}

export function request<T>(
  path: string,
  options?: {
    method?: UniRequestMethod
    data?: any
    headers?: Record<string, string>
    timeout?: number
    silent?: boolean
  },
): Promise<T> {
  const url = joinUrl(API_BASE_URL, path)
  const method = options?.method ?? 'GET'
  const token = uni.getStorageSync(TOKEN_KEY)

  return new Promise<T>((resolve, reject) => {
    uni.request({
      url,
      method,
      data: options?.data,
      header: {
        'Content-Type': 'application/json',
        ...(token ? { Authorization: `Bearer ${token}` } : {}),
        ...(options?.headers ?? {}),
      },
      timeout: options?.timeout ?? 15000,
      success: (res: any) => {
        if (!res || typeof res.statusCode !== 'number') {
          const err = new ApiError('网络异常：无响应')
          showError(err.message, options?.silent)
          reject(err)
          return
        }

        if (res.statusCode === 401) {
          uni.removeStorageSync(TOKEN_KEY)
        }

        if (res.statusCode < 200 || res.statusCode >= 300) {
          const msg =
            (isPlainObject(res.data) && (res.data.message || res.data.error)) ||
            `请求失败（HTTP ${res.statusCode}）`
          const err = new ApiError(String(msg), { statusCode: res.statusCode })
          showError(err.message, options?.silent)
          reject(err)
          return
        }

        const body = res.data
        if (
          isPlainObject(body) &&
          hasOwn(body, 'code') &&
          hasOwn(body, 'data')
        ) {
          const env = body as ApiEnvelope<T>
          if (typeof env.code === 'number' && env.code !== 0) {
            const err = new ApiError(env.message || '请求失败', {
              code: env.code,
              statusCode: res.statusCode,
              traceId: env.traceId,
            })
            showError(err.message, options?.silent)
            reject(err)
            return
          }
          resolve(env.data as T)
          return
        }

        resolve(body as T)
      },
      fail: (err: any) => {
        const e = new ApiError(err?.errMsg || '网络请求失败')
        showError(e.message, options?.silent)
        reject(e)
      },
    })
  })
}
