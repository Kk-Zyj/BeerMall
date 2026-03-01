// src/api/category.ts
import { request } from '@/api/request'

export function apiCategoryList() {
  return request<any[]>('/api/Category/list')
}
