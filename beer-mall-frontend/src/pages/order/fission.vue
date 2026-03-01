<template>
  <view class="container">
    <view class="task-card">
      <text class="title">您已消费100元</text>
      <text class="sub">邀请3位好友下单，立返百分百</text>
      <view class="progress">当前进度: 0/3</view>

      <button class="share-btn"
              open-type="share">
        立即邀请好友免单
      </button>
    </view>
  </view>
</template>

<script setup lang="ts">
import { onShareAppMessage } from "@dcloudio/uni-app";
import { useAuthStore } from "@/store/auth";

const userStore = useAuthStore();

// 🔥 核心：监听用户点击分享按钮
onShareAppMessage(() => {
  // 1. 准备参数：把当前用户的 ID 作为邀请码传出去
  const inviterId = userStore.userInfo.id;

  return {
    title: "请你喝精酿！我买单，快来薅羊毛~", // 吸引人的标题
    path: `/pages/index/index?inviterId=${inviterId}`, // 对方点开后的落地页
    imageUrl: "/static/share-cover.jpg", // 自定义封面图
  };
});
</script>

<style scoped>
.share-btn {
  background: #ff4d4f;
  color: #fff;
  border-radius: 50rpx;
  /* 去除默认边框 */
}
</style>