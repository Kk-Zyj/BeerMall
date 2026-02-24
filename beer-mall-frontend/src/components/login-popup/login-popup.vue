<template>
  <view class="popup-mask"
        v-if="userStore.showLoginPopup"
        @touchmove.stop.prevent>
    <view class="popup-content">
      <view class="title">欢迎来到 BeerMall</view>
      <view class="sub-title">为了提供更好的服务，我们需要获取您的手机号进行会员身份验证</view>

      <button class="auth-btn"
              open-type="getPhoneNumber"
              @getphonenumber="onGetPhoneNumber">
        微信一键授权
      </button>

      <view class="cancel-btn"
            @click="close">暂不登录</view>
    </view>
  </view>
</template>

<script setup lang="ts">
import { useUserStore } from "@/store/user";

const userStore = useUserStore();

const close = () => {
  userStore.showLoginPopup = false;
};

const onGetPhoneNumber = async (e: any) => {
  if (e.detail.code) {
    // 用户同意了，拿到了 code，传给 Store 处理
    uni.showLoading({ title: "授权中..." });
    try {
      await userStore.bindMobile(e.detail.code);
      // 授权成功，store 会自动关闭弹窗
    } catch (error) {
      console.error(error);
    } finally {
      uni.hideLoading();
    }
  } else {
    // 用户拒绝了
    uni.showToast({ title: "您取消了授权", icon: "none" });
  }
};
</script>

<style scoped lang="scss">
.popup-mask {
  position: fixed;
  top: 0;
  left: 0;
  right: 0;
  bottom: 0;
  background: rgba(0, 0, 0, 0.6);
  z-index: 999;
  display: flex;
  align-items: center;
  justify-content: center;
}
.popup-content {
  background: #fff;
  width: 600rpx;
  border-radius: 20rpx;
  padding: 40rpx;
  text-align: center;
  .title {
    font-size: 36rpx;
    font-weight: bold;
    margin-bottom: 20rpx;
  }
  .sub-title {
    font-size: 28rpx;
    color: #666;
    margin-bottom: 40rpx;
    line-height: 1.5;
  }
  .auth-btn {
    background: #07c160;
    color: #fff;
    border-radius: 44rpx;
    font-size: 30rpx;
    margin-bottom: 20rpx;
  }
  .cancel-btn {
    font-size: 28rpx;
    color: #999;
    padding: 10rpx;
  }
}
</style>