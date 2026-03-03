import { apiWxPrepay } from '@/api/wxpay'

type MaybePromise = void | Promise<void>

export interface PayOrderOptions {
  orderId: number
  orderNo?: string | number
  onCancel?: (orderId: number) => MaybePromise
  onPaid?: (orderId: number) => MaybePromise
  confirmTitle?: string
  confirmText?: string
  payLoadingTitle?: string
  successToastTitle?: string
  failToastTitle?: string
}

function showPayConfirm(title: string, content: string): Promise<boolean> {
  return new Promise((resolve) => {
    uni.showModal({
      title,
      content,
      confirmText: '确认支付',
      cancelText: '稍后支付',
      success: (res) => resolve(!!res.confirm),
      fail: () => resolve(false),
    })
  })
}

function isPayCancel(err: any) {
  const msg = (err?.errMsg || err?.message || '').toLowerCase()
  // 微信/uni 常见取消关键字：cancel / canceled / user cancel
  return msg.includes('cancel')
}

export function useOrderPay() {
  const payOrder = async (options: PayOrderOptions): Promise<boolean> => {
    const {
      orderId,
      orderNo,
      onCancel,
      onPaid,
      confirmTitle = '支付确认',
      confirmText,
      payLoadingTitle = '拉起支付中...',
      successToastTitle = '支付成功',
      failToastTitle = '支付失败',
    } = options

    const content = confirmText ?? `确认支付订单 ${orderNo ?? orderId}？`
    const confirmed = await showPayConfirm(confirmTitle, content)
    if (!confirmed) {
      if (onCancel) await onCancel(orderId)
      return false
    }

    uni.showLoading({ title: payLoadingTitle })

    try {
      // 1) 向后端拿预支付参数
      const payParams = await apiWxPrepay(orderId)

      // 2) 调起微信支付
      await new Promise<void>((resolve, reject) => {
        uni.requestPayment({
          provider: 'wxpay',
          timeStamp: payParams.timeStamp,
          nonceStr: payParams.nonceStr,
          package: payParams.package,
          signType: payParams.signType,
          paySign: payParams.paySign,
          success: () => resolve(),
          fail: (err) => reject(err),
        } as any)
      })

      uni.showToast({ title: successToastTitle, icon: 'success' })

      // 3) 支付成功后的回调（注意：订单最终状态以服务端 notify 为准，你可以在 onPaid 里刷新详情）
      if (onPaid) await onPaid(orderId)
      return true
    } catch (e: any) {
      // 用户取消支付：走 onCancel
      if (isPayCancel(e)) {
        if (onCancel) await onCancel(orderId)
        uni.showToast({ title: '已取消支付', icon: 'none' })
        return false
      }

      uni.showToast({
        title: e?.message || e?.errMsg || failToastTitle,
        icon: 'none',
      })
      return false
    } finally {
      uni.hideLoading()
    }
  }

  return { payOrder }
}
