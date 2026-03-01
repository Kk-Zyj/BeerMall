// src/store/auth.ts
import { defineStore } from 'pinia'
import { ref } from 'vue'
import { apiLogin, apiBindMobile } from '@/api/auth'
import { ApiError } from '@/api/request'

const STORAGE_KEY = 'userInfo'

function toast(msg: string) {
  uni.showToast({ title: msg, icon: 'none' })
}

export const useAuthStore = defineStore('auth', () => {
  const userInfo = ref<{
    id: number
    name?: string
    avatar?: string
    mobile?: string
  } | null>(null)
  const isLogin = ref(false)
  const showLoginPopup = ref(false)

  // 防止 silentLogin 并发
  const _loginPromise = ref<Promise<void> | null>(null)

  function _setUser(data: any) {
    userInfo.value = data
    isLogin.value = !!data?.id
    uni.setStorageSync(STORAGE_KEY, data)
  }

  function loadFromCache() {
    const cache = uni.getStorageSync(STORAGE_KEY)
    if (cache?.id) {
      userInfo.value = cache
      isLogin.value = true
      return true
    }
    return false
  }

  async function silentLogin() {
    if (_loginPromise.value) return _loginPromise.value

    _loginPromise.value = (async () => {
      if (loadFromCache()) return

      const res = await uni.login({ provider: 'weixin' })
      if (!res?.code) {
        toast('微信登录失败')
        return
      }

      try {
        const data = await apiLogin(res.code)
        const id = data.userId ?? data.id
        if (!id) {
          toast('登录失败：缺少用户ID')
          return
        }
        _setUser({
          id,
          name: '微信用户',
          avatar: '/static/default-avatar.png',
          mobile: data.mobile ?? '',
        })
      } catch (e: any) {
        const msg = e instanceof ApiError ? e.message : e?.message || '登录失败'
        toast(msg)
        throw e
      }
    })().finally(() => {
      _loginPromise.value = null
    })

    return _loginPromise.value
  }

  // 绑定手机号：你页面 getPhoneNumber 成功后拿到的 phoneCode 传进来
  async function bindMobile(phoneCode: string) {
    if (!userInfo.value?.id) await silentLogin()
    if (!userInfo.value?.id) throw new Error('用户未初始化，无法绑定手机号')

    try {
      const res = await apiBindMobile({
        userId: userInfo.value.id,
        code: phoneCode,
      })

      // 后端可能返回：{ success, mobile } 或者直接给 mobile/isMobileVerified
      const ok = (res as any)?.success ?? true
      if (!ok) {
        toast('绑定失败')
        return false
      }

      userInfo.value.mobile = (res as any)?.mobile ?? userInfo.value.mobile
      uni.setStorageSync(STORAGE_KEY, userInfo.value)
      showLoginPopup.value = false
      uni.showToast({ title: '授权成功', icon: 'success' })
      return true
    } catch (e: any) {
      const msg =
        e instanceof ApiError ? e.message : e?.message || '绑定手机号失败'
      toast(msg)
      throw e
    }
  }

  /**
   * 检查：是否满足访问条件
   * - requireMobile=true：下单/支付等必须手机号
   * - requireMobile=false：只要影子用户
   */
  async function checkAuth(requireMobile = true): Promise<boolean> {
    if (!userInfo.value?.id) {
      toast('正在初始化...')
      await silentLogin()
    }
    if (!userInfo.value?.id) return false

    if (requireMobile && !userInfo.value.mobile) {
      showLoginPopup.value = true
      return false
    }
    return true
  }

  // 退出：清本地缓存；后端没 logout 接口，所以仅本地处理
  async function logout() {
    userInfo.value = null
    isLogin.value = false
    showLoginPopup.value = false
    uni.removeStorageSync(STORAGE_KEY)

    // 退出后回到影子用户（体验更顺：还能浏览/加购）
    await silentLogin()
    toast('已退出登录')
  }

  function openLoginPopup() {
    showLoginPopup.value = true
  }
  function closeLoginPopup() {
    showLoginPopup.value = false
  }

  return {
    userInfo,
    isLogin,
    showLoginPopup,
    silentLogin,
    bindMobile,
    checkAuth,
    logout,
    openLoginPopup,
    closeLoginPopup,
  }
})
