import { defineStore } from 'pinia'
import { ref, computed } from 'vue'
import { useUserStore } from '@/store/user'

export const useCartStore = defineStore('cart', () => {
  const baseUrl = 'https://localhost:7252'
  const cartList = ref<any[]>([]) // 购物车列表

  // 计算属性：总数量
  const totalCount = computed(() => {
    return cartList.value.reduce((sum, item) => sum + item.quantity, 0)
  })

  // 计算属性：总金额
  const totalPrice = computed(() => {
    return cartList.value.reduce(
      (sum, item) => sum + item.quantity * item.price,
      0,
    )
  })

  // Action: 获取最新购物车
  const fetchCart = async () => {
    console.log('当前用户信息:', userStore.userInfo)
    // 如果是 undefined，说明上面第1步的字段映射没做好
    if (!userStore.userInfo?.id) {
      console.error('用户ID不存在，无法获取购物车')
      return
    }
    uni.request({
      url: `${baseUrl}/api/cart?userId=${userStore.userInfo.id}`,
      success: (res: any) => {
        cartList.value = res.data.items || []
      },
    })
  }

  const userStore = useUserStore()
  // Action: 加购/减购
  const addToCart = async (product: any, sku: any, quantity: number) => {
    const realUserId = userStore.userInfo.id
    console.info('realUserId:', realUserId)
    uni.request({
      url: `${baseUrl}/api/cart/add`,
      method: 'POST',
      data: {
        userId: realUserId, // 实际开发从 userStore 获取
        productId: product.id,
        skuId: sku.id,
        quantity: quantity,
      },
      success: (res: any) => {
        // 后端直接返回了最新的购物车结构，直接覆盖本地
        cartList.value = res.data.items
      },
    })
  }

  // Action: 清空购物车
  const clearCart = () => {
    uni.request({
      url: `${baseUrl}/api/cart/clear?userId=${userStore.userInfo.id}`,
      method: 'DELETE',
      success: (res: any) => {
        if (res.statusCode === 200) {
          // 1. 清空本地 Pinia 状态 (界面会瞬间变为空状态)
          cartList.value = []

          // 2. 提示用户
          uni.showToast({ title: '购物车已清空', icon: 'success' })
        } else {
          uni.showToast({ title: '清空失败', icon: 'none' })
        }
      },
      fail: () => {
        uni.showToast({ title: '网络错误', icon: 'none' })
      },
    })
  }

  return { cartList, totalCount, totalPrice, fetchCart, addToCart, clearCart }
})
