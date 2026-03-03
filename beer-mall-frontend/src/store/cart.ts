// src/store/cart.ts
import { defineStore } from 'pinia'
import { ref, computed } from 'vue'
import { useAuthStore } from '@/store/auth'
import { apiGetCart, apiCartAdd, apiClearCart, type CartItem } from '@/api/cart'
import { ApiError } from '@/api/request'

function toast(msg: string) {
  uni.showToast({ title: msg, icon: 'none' })
}

export const useCartStore = defineStore('cart', () => {
  const auth = useAuthStore()

  const cartList = ref<CartItem[]>([])

  const totalCount = computed(() =>
    cartList.value.reduce((sum, it) => sum + (it.quantity || 0), 0),
  )
  const totalPrice = computed(() =>
    cartList.value.reduce(
      (sum, it) => sum + (it.quantity || 0) * (it.price || 0),
      0,
    ),
  )

  async function _ensureUserId(): Promise<number> {
    // 购物车允许影子用户
    const ok = await auth.checkAuth(false)
    if (!ok || !auth.userInfo?.id) throw new Error('用户未初始化')
    return auth.userInfo.id
  }

  async function refresh() {
    const userId = await _ensureUserId()
    const res = await apiGetCart(userId)
    console.log(res.items)

    cartList.value = res?.items ?? []
  }

  async function addToCart(payload: {
    productId: number
    skuId: number
    quantity: number
  }) {
    const userId = await _ensureUserId()
    const res = await apiCartAdd({ userId, ...payload })
    cartList.value = res?.items ?? []
  }

  async function increase(item: CartItem, step = 1) {
    await addToCart({
      productId: item.productId,
      skuId: item.skuId,
      quantity: Math.abs(step),
    })
  }

  async function decrease(item: CartItem, step = 1) {
    await addToCart({
      productId: item.productId,
      skuId: item.skuId,
      quantity: -Math.abs(step),
    })
  }

  /**
   * 改数量：后端无 setQuantity，用 delta 实现
   * delta = newQty - oldQty，走 /api/Cart/add
   */
  async function setQuantity(item: CartItem, newQty: number) {
    newQty = Math.max(0, Math.floor(newQty))
    const oldQty = item.quantity || 0
    const delta = newQty - oldQty
    if (delta === 0) return
    await addToCart({
      productId: item.productId,
      skuId: item.skuId,
      quantity: delta,
    })
  }

  // 删除单条：delta = -当前数量
  async function removeItem(item: CartItem) {
    const qty = item.quantity || 0
    if (qty <= 0) return
    await addToCart({
      productId: item.productId,
      skuId: item.skuId,
      quantity: -qty,
    })
  }

  async function clear() {
    const userId = await _ensureUserId()
    await apiClearCart(userId)
    cartList.value = []
    uni.showToast({ title: '购物车已清空', icon: 'success' })
  }

  // 统一错误包装（页面调用更省心）
  async function safeRun<T>(fn: () => Promise<T>) {
    try {
      return await fn()
    } catch (e: any) {
      const msg = e instanceof ApiError ? e.message : e?.message || '操作失败'
      toast(msg)
      throw e
    }
  }

  return {
    cartList,
    totalCount,
    totalPrice,
    refresh: () => safeRun(refresh),
    addToCart: (p: { productId: number; skuId: number; quantity: number }) =>
      safeRun(() => addToCart(p)),
    increase: (item: CartItem, step = 1) => safeRun(() => increase(item, step)),
    decrease: (item: CartItem, step = 1) => safeRun(() => decrease(item, step)),
    setQuantity: (item: CartItem, newQty: number) =>
      safeRun(() => setQuantity(item, newQty)),
    removeItem: (item: CartItem) => safeRun(() => removeItem(item)),
    clear: () => safeRun(clear),
  }
})
