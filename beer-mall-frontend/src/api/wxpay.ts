import { request } from '@/api/request'

export type WxPrepayParams = {
  timeStamp: string
  nonceStr: string
  package: string // "prepay_id=xxx"
  signType: 'RSA'
  paySign: string
}

export function apiWxPrepay(orderId: number) {
  return request<WxPrepayParams>('/api/WxPay/prepay', {
    method: 'POST',
    data: { orderId },
  })
}
