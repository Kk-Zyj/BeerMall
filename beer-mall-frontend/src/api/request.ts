// src/api/request.ts
import { API_BASE_URL } from '@/config/api'

type UniRequestMethod = 'GET' | 'POST' | 'PUT' | 'DELETE'

export interface ApiResult<T> {
  data: T
  statusCode: number
  header: any
}

export class ApiError extends Error {
  statusCode?: number
  constructor(message: string, statusCode?: number) {
    super(message)
    this.name = 'ApiError'
    this.statusCode = statusCode
  }
}

function joinUrl(base: string, path: string) {
  if (!path) return base
  if (path.startsWith('http')) return path
  if (base.endsWith('/') && path.startsWith('/')) return base + path.slice(1)
  if (!base.endsWith('/') && !path.startsWith('/')) return base + '/' + path
  return base + path
}

export function request<T>(
  path: string,
  options?: {
    method?: UniRequestMethod
    data?: any
    headers?: Record<string, string>
    timeout?: number
  },
): Promise<T> {
  const url = joinUrl(API_BASE_URL, path)
  const method = options?.method ?? 'GET'

  return new Promise<T>((resolve, reject) => {
    uni.request({
      url,
      method,
      data: options?.data,
      header: {
        'Content-Type': 'application/json',
        ...(options?.headers ?? {}),
      },
      timeout: options?.timeout ?? 15000,
      success: (res) => {
        // 1) HTTP 层失败
        if (!res || typeof res.statusCode !== 'number') {
          reject(new ApiError('网络异常：无响应'))
          return
        }
        if (res.statusCode < 200 || res.statusCode >= 300) {
          const msg =
            (res.data as any)?.message ||
            (res.data as any)?.error ||
            `请求失败（HTTP ${res.statusCode}）`
          reject(new ApiError(msg, res.statusCode))
          return
        }

        // 2) 业务层：如果你的后端未来统一成 {code,message,data}，这里可集中处理
        resolve(res.data as T)
      },
      fail: (err) => {
        reject(new ApiError(err?.errMsg || '网络请求失败'))
      },
    })
  })
}
