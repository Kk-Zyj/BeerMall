import { apiOrderPay } from "@/api/order";

type MaybePromise = void | Promise<void>;

export interface PayOrderOptions {
  orderId: number;
  orderNo?: string | number;
  onCancel?: (orderId: number) => MaybePromise;
  onPaid?: (orderId: number) => MaybePromise;
  confirmTitle?: string;
  confirmText?: string;
  payLoadingTitle?: string;
  successToastTitle?: string;
  failToastTitle?: string;
}

function showPayConfirm(title: string, content: string): Promise<boolean> {
  return new Promise((resolve) => {
    uni.showModal({
      title,
      content,
      confirmText: "确认支付",
      cancelText: "稍后支付",
      success: (res) => resolve(!!res.confirm),
      fail: () => resolve(false),
    });
  });
}

export function useOrderPay() {
  const payOrder = async (options: PayOrderOptions): Promise<boolean> => {
    const {
      orderId,
      orderNo,
      onCancel,
      onPaid,
      confirmTitle = "支付确认",
      confirmText,
      payLoadingTitle = "支付中...",
      successToastTitle = "支付成功",
      failToastTitle = "支付失败",
    } = options;

    const content = confirmText ?? `确认支付订单 ${orderNo ?? orderId}？`;
    const confirmed = await showPayConfirm(confirmTitle, content);
    if (!confirmed) {
      if (onCancel) await onCancel(orderId);
      return false;
    }

    uni.showLoading({ title: payLoadingTitle });
    try {
      await apiOrderPay(orderId);
      uni.showToast({ title: successToastTitle, icon: "success" });
      if (onPaid) await onPaid(orderId);
      return true;
    } catch (e: any) {
      uni.showToast({ title: e?.message || failToastTitle, icon: "none" });
      return false;
    } finally {
      uni.hideLoading();
    }
  };

  return { payOrder };
}
