import axios from 'axios'
import { ElMessage } from 'element-plus'

export const ADMIN_TOKEN_KEY = 'admin_token'

const service = axios.create({
  baseURL: import.meta.env.VITE_API_BASE_URL || 'https://localhost:7252/api',
  timeout: 10000
})

service.interceptors.request.use(
  (config) => {
    const token = localStorage.getItem(ADMIN_TOKEN_KEY)
    if (token) {
      config.headers = config.headers || {}
      config.headers.Authorization = `Bearer ${token}`
    }
    return config
  },
  (error) => Promise.reject(error)
)

service.interceptors.response.use(
  (response) => {
    const body = response.data

    if (body && typeof body === 'object' && 'code' in body && 'data' in body) {
      if (body.code !== 0) {
        ElMessage.error(body.message || '请求失败')
        return Promise.reject(body)
      }
      return body.data
    }

    return body
  },
  (error) => {
    const status = error?.response?.status
    const data = error?.response?.data

    const msg =
      data?.message ||
      data?.title ||
      data?.detail ||
      (data?.errors && Object.values(data.errors)?.flat?.()?.[0]) ||
      '网络请求失败'

    if (status === 401 || status === 403) {
      localStorage.removeItem(ADMIN_TOKEN_KEY)
      if (location.pathname !== '/login') {
        location.href = '/login'
      }
    }

    ElMessage.error(String(msg))
    return Promise.reject(error)
  }
)

export default service