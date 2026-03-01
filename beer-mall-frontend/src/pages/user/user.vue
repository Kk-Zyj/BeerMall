<template>
  <view class="page-container">

    <view class="header-section"
          @click="handleUserClick">
      <view class="user-box">
        <image :src="userStore.userInfo?.avatar || '/static/default-avatar.png'"
               class="avatar"
               mode="aspectFill" />
        <view class="info">
          <text class="nickname">
            {{ userStore.userInfo?.mobile ? (userStore.userInfo?.name || '微信用户') : '点击登录/注册' }}
          </text>

          <text class="sub-text">
            {{ userStore.userInfo?.mobile ? userStore.userInfo.mobile : '登录后享受更多权益' }}
          </text>
        </view>
      </view>
      <view class="css-arrow"></view>
    </view>

    <view class="order-card">
      <view class="card-header"
            @click="goToOrderList(0)">
        <text class="title">我的订单</text>
        <text class="more">全部订单 ></text>
      </view>
      <view class="status-grid">
        <view class="status-item"
              @click="goToOrderList(1)">
          <text class="icon">💳</text>
          <text class="label">待付款</text>
        </view>
        <view class="status-item"
              @click="goToOrderList(2)">
          <text class="icon">📦</text>
          <text class="label">待发货</text>
        </view>
        <view class="status-item"
              @click="goToOrderList(3)">
          <text class="icon">🚚</text>
          <text class="label">待收货</text>
        </view>
      </view>
    </view>

    <view class="menu-list">
      <view class="menu-item"
            @click="goToAddress">
        <view class="left">
          <text class="icon">📍</text>
          <text class="label">地址管理</text>
        </view>
        <view class="css-arrow"></view>
      </view>

      <button class="menu-item contact-btn"
              open-type="contact">
        <view class="left">
          <text class="icon">🎧</text>
          <text class="label">联系客服</text>
        </view>
        <view class="css-arrow"></view>
      </button>

      <view class="menu-item"
            @click="handleLogout"
            v-if="userStore.userInfo?.mobile">
        <view class="left">
          <text class="icon">🚪</text>
          <text class="label">退出登录</text>
        </view>
        <view class="css-arrow"></view>
      </view>
    </view>

    <login-popup />

  </view>
</template>

<script setup lang="ts">
import { useAuthStore } from "@/store/auth";
// 🔥 引入组件 (请确保路径正确)
import LoginPopup from "@/components/login-popup/login-popup.vue";

const userStore = useAuthStore();

// 点击头部逻辑
const handleUserClick = async () => {
  // checkAuth 会自动判断：
  // 1. 如果没手机号 -> 自动把 showLoginPopup 设为 true -> 弹窗
  // 2. 如果有手机号 -> 返回 true -> 进入下面逻辑
  if (await userStore.checkAuth()) {
    // 已是正式会员，跳转去修改个人资料(头像/昵称)
    uni.navigateTo({ url: "/pages/my/profile" });
  }
};

// 退出登录
const handleLogout = () => {
  uni.showModal({
    title: "提示",
    content: "确定退出登录吗？",
    success: (res) => {
      // 退出后，界面会自动变回 "点击登录/注册"
      if (res.confirm) userStore.logout();
    },
  });
};

// 跳转订单页
const goToOrderList = async (status: number) => {
  // 使用 checkAuth 统一守卫，没登录点击订单也会弹窗
  if (!(await userStore.checkAuth())) return;
  uni.navigateTo({ url: `/pages/order/list?status=${status}` });
};

// 跳转地址页
const goToAddress = async () => {
  if (!(await userStore.checkAuth())) return;
  uni.navigateTo({ url: "/pages/address/list" });
};
</script>

<style scoped lang="scss">
.page-container {
  min-height: 100vh;
  background-color: #f5f7fa;
  padding: 30rpx;
}

/* 头部用户信息 */
.header-section {
  display: flex;
  justify-content: space-between;
  align-items: center;
  background-color: #fff;
  padding: 40rpx 30rpx;
  border-radius: 20rpx;
  margin-bottom: 30rpx;

  .user-box {
    display: flex;
    align-items: center;
    .avatar {
      width: 120rpx;
      height: 120rpx;
      border-radius: 50%;
      background: #eee;
      border: 2rpx solid #fff; /* 增加一点白边更有质感 */
    }
    .info {
      margin-left: 24rpx;
      .nickname {
        font-size: 36rpx;
        font-weight: bold;
        color: #333;
        display: block;
      }
      .sub-text {
        font-size: 24rpx;
        color: #999;
        margin-top: 8rpx;
        display: block;
      }
    }
  }
  .arrow {
    color: #ccc;
    font-size: 32rpx; /* 稍微大一点 */
  }
}

/* 订单卡片 */
.order-card {
  background: #fff;
  border-radius: 20rpx;
  padding: 30rpx;
  margin-bottom: 30rpx;

  .card-header {
    display: flex;
    justify-content: space-between;
    align-items: center;
    padding-bottom: 30rpx;
    border-bottom: 1rpx solid #f9f9f9;
    .title {
      font-weight: bold;
      font-size: 30rpx;
    }
    .more {
      font-size: 24rpx;
      color: #999;
    }
  }

  .status-grid {
    display: flex;
    justify-content: space-around;
    padding-top: 30rpx;
    .status-item {
      display: flex;
      flex-direction: column;
      align-items: center;
      .icon {
        font-size: 48rpx;
        margin-bottom: 10rpx;
      }
      .label {
        font-size: 24rpx;
        color: #666;
      }
    }
  }
}

/* 菜单列表 */
.menu-list {
  background: #fff;
  border-radius: 20rpx;
  overflow: hidden;

  .menu-item {
    display: flex;
    justify-content: space-between;
    align-items: center;
    padding: 30rpx;
    border-bottom: 1rpx solid #f9f9f9;
    background: #fff;

    /* 去除 button 默认样式 */
    &::after {
      border: none;
    }
    &.contact-btn {
      line-height: normal;
      font-size: inherit;
      text-align: left;
      margin: 0;
      border-radius: 0;
    }

    .left {
      display: flex;
      align-items: center;
      .icon {
        margin-right: 20rpx;
        font-size: 32rpx;
      }
      .label {
        font-size: 28rpx;
        color: #333;
      }
    }
    .arrow {
      color: #ccc;
      font-size: 24rpx;
    }
  }
}

/* 🔥 核心：通用 CSS 箭头样式 */
.css-arrow {
  width: 16rpx;
  height: 16rpx;
  border-top: 3rpx solid #ccc; /* 上边框 */
  border-right: 3rpx solid #ccc; /* 右边框 */
  transform: rotate(45deg); /* 旋转45度 */
  margin-left: 10rpx;
}
</style>