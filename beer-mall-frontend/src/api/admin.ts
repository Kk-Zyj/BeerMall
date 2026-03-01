import { request } from "@/api/request";

export function apiGetGroupRule() {
  return request<any>("/api/admin/0");
}
