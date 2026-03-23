// src/store/auth.ts
import { defineStore } from 'pinia'
import { ref } from 'vue'
import { apiLogin, apiBindMobile } from '@/api/auth'
import { ApiError } from '@/api/request'

const STORAGE_KEY = 'userInfo'
const TOKEN_KEY = 'token'

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

  // 是否已经完成初始化（避免重复 silentLogin）
  const inited = ref(false)

  // 防止并发重复登录
  const _loginPromise = ref<Promise<void> | null>(null)

  function _setUser(data: any) {
    userInfo.value = data
    isLogin.value = !!data?.id
    uni.setStorageSync(STORAGE_KEY, data)
  }

  function _setToken(token?: string) {
    if (token) {
      uni.setStorageSync(TOKEN_KEY, token)
    }
  }

  function _clearAuth() {
    userInfo.value = null
    isLogin.value = false
    showLoginPopup.value = false
    uni.removeStorageSync(STORAGE_KEY)
    uni.removeStorageSync(TOKEN_KEY)
  }

  function getToken() {
    return uni.getStorageSync(TOKEN_KEY)
  }

  function loadFromCache() {
    const cache = uni.getStorageSync(STORAGE_KEY)
    const token = uni.getStorageSync(TOKEN_KEY)

    if (cache?.id && token) {
      userInfo.value = cache
      isLogin.value = true
      return true
    }

    // 兼容旧缓存：只要有一半缺失，就都清掉
    if (cache || token) {
      uni.removeStorageSync(STORAGE_KEY)
      uni.removeStorageSync(TOKEN_KEY)
    }

    userInfo.value = null
    isLogin.value = false
    return false
  }

  /**
   * 初始化登录状态：
   * 1. 优先从本地缓存恢复
   * 2. 本地没有时，再走微信 silent login
   */
  async function initAuth() {
    if (inited.value) return
    if (loadFromCache()) {
      inited.value = true
      return
    }

    await silentLogin()
    inited.value = true
  }

  /**
   * 微信静默登录
   * force = true 时，忽略缓存重新登录
   */
  async function silentLogin(force = false) {
    if (!force && loadFromCache()) {
      return
    }

    if (_loginPromise.value) return _loginPromise.value

    _loginPromise.value = (async () => {
      const res = await uni.login({ provider: 'weixin' })
      if (!res?.code) {
        toast('微信登录失败')
        return
      }

      try {
        const data = await apiLogin(res.code)

        const token = data.token
        const user = data.user
        const id = user?.id ?? data.userId

        if (!id) {
          toast('登录失败：缺少用户ID')
          return
        }

        if (!token) {
          toast('登录失败：缺少Token')
          return
        }

        _setToken(token)
        _setUser({
          id,
          name: user?.nickName ?? data.nickName ?? '微信用户',
          avatar:
            user?.avatarUrl ?? data.avatarUrl ?? '/static/default-avatar.png',
          mobile: user?.mobile ?? data.mobile ?? '',
        })
      } catch (e: any) {
        _clearAuth()
        const msg = e instanceof ApiError ? e.message : e?.message || '登录失败'
        toast(msg)
        throw e
      }
    })().finally(() => {
      _loginPromise.value = null
    })

    return _loginPromise.value
  }

  async function bindMobile(phoneCode: string) {
    if (!userInfo.value?.id) {
      await silentLogin()
    }
    if (!userInfo.value?.id) {
      throw new Error('用户未初始化，无法绑定手机号')
    }

    try {
      const res = await apiBindMobile({ code: phoneCode })

      const ok = (res as any)?.success ?? true
      if (!ok) {
        toast('绑定失败')
        return false
      }

      const mobile = res?.user?.mobile ?? (res as any)?.mobile ?? ''
      userInfo.value.mobile = mobile
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

  async function checkAuth(requireMobile = true): Promise<boolean> {
    if (!inited.value) {
      await initAuth()
    }

    if (!userInfo.value?.id) {
      return false
    }

    if (requireMobile && !userInfo.value.mobile) {
      showLoginPopup.value = true
      return false
    }

    return true
  }

  /**
   * 退出登录：
   * 这里只清状态，不再自动重新 silentLogin
   */
  function logout() {
    _clearAuth()
    inited.value = false
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
    inited,
    getToken,
    initAuth,
    silentLogin,
    bindMobile,
    checkAuth,
    logout,
    openLoginPopup,
    closeLoginPopup,
  }
})
