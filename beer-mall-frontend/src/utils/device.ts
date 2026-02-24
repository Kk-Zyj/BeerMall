export const getDeviceId = () => {
  console.info('获取设备指纹')
  // 1. 先从缓存拿
  let deviceId = uni.getStorageSync('device_fingerprint')

  // 2. 如果没有，生成一个并存入缓存
  if (!deviceId) {
    // 简易 UUID 生成
    deviceId = 'xxxxxxxx-xxxx-4xxx-yxxx-xxxxxxxxxxxx'.replace(
      /[xy]/g,
      function (c) {
        var r = (Math.random() * 16) | 0,
          v = c == 'x' ? r : (r & 0x3) | 0x8
        return v.toString(16)
      },
    )
    uni.setStorageSync('device_fingerprint', deviceId)
  }

  // 3. 返回
  return deviceId
}
