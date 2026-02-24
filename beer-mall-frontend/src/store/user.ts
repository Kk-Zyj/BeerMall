import { defineStore } from 'pinia'
import { ref } from 'vue'

export const useUserStore = defineStore('user', () => {
  const userInfo = ref<any>(null)
  const isLogin = ref(false)
  // 控制全局登录弹窗的显示/隐藏
  const showLoginPopup = ref(false)

  // 1. 静默登录 (App启动时调用)
  // 只负责拿 OpenId 换 UserId，不管有没有手机号
  const silentLogin = async () => {
    // 如果缓存里已经有且不过期，可以先用缓存的
    const storageUser = uni.getStorageSync('userInfo')
    if (storageUser) {
      console.info('从缓存中获取用户信息')
      userInfo.value = storageUser
      isLogin.value = true
      return
    }

    const res = await uni.login({ provider: 'weixin' })
    if (!res.code) {
      console.info('微信登录失败')
      return
    }

    uni.request({
      url: 'https://localhost:7252/api/auth/login',
      method: 'POST',
      data: { Code: res.code },
      success: (apiRes: any) => {
        if (apiRes.statusCode === 200) {
          const data = apiRes.data
          // 映射数据
          userInfo.value = {
            id: data.userId || data.id,
            name: data.nickName || '微信用户',
            avatar: data.avatarUrl || '/static/default-avatar.png',
            mobile: data.mobile || '', // 🔥 关键：后端要返回 mobile 字段
          }
          isLogin.value = true
          uni.setStorageSync('userInfo', userInfo.value)
          console.log('静默登录成功，影子账号ID:', userInfo.value.id)
        }
      },
    })
  }

  // 2. 绑定手机号 (用户点击弹窗按钮后调用)
  const bindMobile = async (phoneCode: string) => {
    return new Promise((resolve, reject) => {
      uni.request({
        url: 'https://localhost:7252/api/auth/bind-mobile',
        method: 'POST',
        data: {
          UserId: userInfo.value.id, // 用影子账号ID去绑定
          Code: phoneCode,
        },
        success: (res: any) => {
          if (res.statusCode === 200) {
            // 更新本地状态
            userInfo.value.mobile = res.data.mobile
            uni.setStorageSync('userInfo', userInfo.value) // 更新缓存

            const inviterId = uni.getStorageSync('temp_inviter_id')
            if (inviterId) {
              // 告诉后端：我是被 inviterId 邀请来的，请绑定关系
              res.data.inviterId = inviterId
            }
            showLoginPopup.value = false // 关闭弹窗
            uni.showToast({ title: '授权成功', icon: 'success' })
            resolve(true)
          } else {
            uni.showToast({ title: '绑定失败', icon: 'none' })
            reject(false)
          }
        },
        fail: () => reject(false),
      })
    })
  }

  // 3. 权限检查卫士
  // 返回 true 表示通过，返回 false 表示拦截并弹窗
  const checkAuth = (): boolean => {
    console.info('开始校验')
    // 第一层：是否静默登录成功（有影子ID）
    if (!isLogin.value || !userInfo.value?.id) {
      console.info('用户未登录或登录失败')
      silentLogin() // 尝试亡羊补牢
      uni.showToast({ title: '正在初始化...', icon: 'none' })
      return false
    }

    // 第二层：是否有手机号
    if (!userInfo.value.mobile) {
      console.info('用户未绑定手机号')
      // 🔥 没有手机号，唤起自定义弹窗
      showLoginPopup.value = true
      return false
    }

    return true
  }

  // 退出登录
  const logout = async () => {
    // 1. 清除前端状态
    userInfo.value = null
    isLogin.value = false
    showLoginPopup.value = false // 关掉可能存在的弹窗

    // 2. 清除本地缓存
    uni.removeStorageSync('userInfo')

    // 3. 🔥 关键步骤：退出后，立刻执行静默登录
    // 这样用户就从 "正式会员" 变成了 "影子用户" (有 ID，无手机号)
    // 用户依然可以加购，但去结算时会被要求再次授权
    await silentLogin()

    uni.showToast({ title: '已退出登录', icon: 'none' })
  }

  return {
    userInfo,
    isLogin,
    showLoginPopup,
    silentLogin,
    bindMobile,
    checkAuth,
    logout,
  }
})
