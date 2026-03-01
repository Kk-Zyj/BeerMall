<template>
  <view class="container">
    <view class="header-section">
      <image class="header-bg-image"
             :src="bannerUrl"
             mode="aspectFill" />

      <view class="member-bar">
        <view class="left">
          <text class="diamond-icon">💎</text>
          <text>注册会员 | 登录后尊享更多专属特权</text>
        </view>
        <view class="right-btn">
          <text>注册/登录</text>
        </view>
      </view>
    </view>

    <view class="dashboard-grid">
      <view class="row-top">
        <view class="card big-card hover-effect"
              @click="goToMenu">
          <view class="icon-box big-icon"> 🍺 </view>
          <text class="card-title-en">Order Beer</text>
          <text class="card-title-cn">邮寄</text>
          <text class="card-desc">立即下单，畅享美味</text>
        </view>

        <view class="right-col">
          <view class="card small-card hover-effect"
                @click="goToOrders(0)">
            <view class="content-row">
              <view class="text-group">
                <text class="card-title-en">My Order</text>
                <text class="card-title-cn">我的订单</text>
                <text class="card-desc">查看我的订单</text>
              </view>
              <view class="icon-box small-icon">📋</view>
            </view>
          </view>

          <view class="card small-card hover-effect"
                @click="goToMenu">
            <view class="content-row">
              <view class="text-group">
                <text class="card-title-en">Takeout</text>
                <text class="card-title-cn">同城快送</text>
                <text class="card-desc">下单两小时内送达</text>
              </view>
              <view class="icon-box small-icon">🥡</view>
            </view>
          </view>
        </view>
      </view>

      <view class="row-bottom">
        <view class="card medium-card hover-effect"
              @click="onFeatureClick('充值')">
          <view class="icon-top">⭐</view>
          <text class="card-title-cn">会员充值</text>
          <text class="card-title-en">Member recharge</text>
        </view>

        <view class="card medium-card hover-effect"
              @click="goToUser">
          <view class="icon-top">💳</view>
          <text class="card-title-cn">会员中心</text>
          <text class="card-title-en">Member Center</text>
        </view>
      </view>
    </view>
  </view>
</template>

<script setup lang="ts">
// --- Navigation Commands ---
import { ref } from "vue";
import { onLoad } from "@dcloudio/uni-app";
import { useAuthStore } from "@/store/auth";
import { API_BASE_URL } from "@/config/api";

const userStore = useAuthStore();

// --- ViewModel State ---

// ✅ 这里定义默认图片，或者从后端获取的图片地址
// 你可以换成你自己的本地图片路径，或者网络图片 URL
const bannerUrl = ref(
  "https://via.placeholder.com/800x600?text=Merchant+Banner"
);

// 模拟从后端获取商家配置
const loadStoreSettings = () => {
  // 真实场景：调用 C# API GET /api/store/settings
  // const res = await uni.request(...)
  // bannerUrl.value = res.data.bannerUrl;

  // 这里演示动态改变（你可以换成你刚才上传的那张 "Hi bro" 的图的地址）
  // 假设你的图片放在 backend 的 wwwroot/images/home_banner.jpg
  bannerUrl.value = `${API_BASE_URL}/images/home_banner.jpg`;
};

onLoad((options) => {
  loadStoreSettings();
  // 1. 检查链接里有没有 inviterId
  if (options && options.inviterId) {
    console.log("检测到邀请人ID:", options.inviterId),
      // 2. 将邀请人ID存入本地缓存
      // 为什么不直接调接口绑定？因为B用户可能还没登录！
      // 我们先存起来，等B用户稍后点击"微信一键登录"时，再把这个ID传给后端
      uni.setStorageSync("temp_inviter_id", options.inviterId);
  }
});
// 跳转到点餐页 (Tab页必须用 switchTab)
const goToMenu = () => {
  uni.switchTab({ url: "/pages/category/category" });
};

// 跳转到订单/购物车 (Tab页)
// 跳转订单页
const goToOrders = async (status: number) => {
  // 使用 checkAuth 统一守卫，没登录点击订单也会弹窗
  if (!(await userStore.checkAuth())) return;
  uni.navigateTo({ url: `/pages/order/list?status=${status}` });
};

// 跳转到用户中心 (Tab页)
const goToUser = () => {
  uni.switchTab({ url: "/pages/user/user" });
};

const onFeatureClick = (name: string) => {
  uni.showToast({ title: `${name} 功能开发中`, icon: "none" });
};
</script>

<style scoped lang="scss">
/* --- 布局变量 --- */
$bg-gray: #f5f5f5;
$text-black: #333;
$card-radius: 16rpx;

.container {
  min-height: 100vh;
  background-color: $bg-gray;
  display: flex;
  flex-direction: column;
}
.header-section {
  background-color: #fff;
  padding: 40rpx 30rpx 0 30rpx;
  position: relative;
  overflow: hidden; /* 防止图片放大后溢出 */
  min-height: 400rpx; /* 给一个最小高度，防止图片没加载时塌陷 */

  /* ✅ 新增：背景图片的样式 */
  .header-bg-image {
    position: absolute;
    top: 0;
    left: 0;
    width: 100%;
    height: 100%;
    z-index: 0; /* 放在最底层 */
  }

  /* 原来的内容区域，需要确保在图片上面 */
  .header-content {
    position: relative;
    z-index: 1; /* 浮在图片上 */
    margin-bottom: 40rpx;

    /* 如果你的背景图比较花，可能需要给文字加个阴影才能看清 */
    .greeting-en {
      /* ... 原有样式 ... */
      text-shadow: 0 2px 4px rgba(0, 0, 0, 0.3); /* 加点阴影防看不清 */
    }
    .title-group {
      /* ... 原有样式 ... */
      text-shadow: 0 2px 4px rgba(0, 0, 0, 0.3);
    }
  }
  /* 黑色的会员条 */
  .member-bar {
    background-color: #2c2c2c;
    color: #ffd700; /* 金色文字 */
    border-top-left-radius: 16rpx;
    border-top-right-radius: 16rpx;
    padding: 20rpx 30rpx;
    display: flex;
    justify-content: space-between;
    align-items: center;
    font-size: 24rpx;
    box-shadow: 0 -4rpx 10rpx rgba(0, 0, 0, 0.1);

    .left {
      display: flex;
      align-items: center;
      .diamond-icon {
        margin-right: 10rpx;
      }
    }

    .right-btn {
      background: #fff;
      color: #000;
      padding: 6rpx 20rpx;
      border-radius: 30rpx;
      font-weight: bold;
    }
  }
}

/* 2. Dashboard Grid 布局 */
.dashboard-grid {
  flex: 1;
  padding: 20rpx;
  margin-top: -10rpx; /*稍微往上顶一点，盖住header的缝隙*/

  /* 通用卡片样式 */
  .card {
    background: #fff;
    border-radius: $card-radius;
    padding: 30rpx;
    box-shadow: 0 4rpx 12rpx rgba(0, 0, 0, 0.03);
    box-sizing: border-box;

    &.hover-effect:active {
      transform: scale(0.98); /* 点击时的微缩放效果 */
      background-color: #fafafa;
    }
  }

  /* 文本通用样式 */
  .card-title-en {
    font-size: 24rpx;
    color: #333;
    font-weight: bold;
    display: block;
    margin-bottom: 4rpx;
  }
  .card-title-cn {
    font-size: 36rpx;
    color: #000;
    font-weight: bold;
    display: block;
    margin-bottom: 8rpx;
  }
  .card-desc {
    font-size: 22rpx;
    color: #999;
    display: block;
  }

  /* Row 1 Layout */
  .row-top {
    display: flex;
    justify-content: space-between;
    margin-bottom: 20rpx;
    height: 340rpx; /* 固定高度确保对齐 */

    .big-card {
      width: 48%;
      display: flex;
      flex-direction: column;
      justify-content: center;
      align-items: center;
      text-align: center;

      .big-icon {
        font-size: 80rpx;
        margin-bottom: 20rpx;
      }
      .card-title-cn {
        font-size: 40rpx;
        margin: 10rpx 0;
      }
    }

    .right-col {
      width: 48%;
      display: flex;
      flex-direction: column;
      justify-content: space-between;

      .small-card {
        height: 48%; /* 上下各占一半 */
        display: flex;
        align-items: center;
        padding: 20rpx;

        .content-row {
          width: 100%;
          display: flex;
          justify-content: space-between;
          align-items: center;
        }

        .text-group {
          text-align: left;
        }
        .small-icon {
          font-size: 50rpx;
        }
      }
    }
  }

  /* Row 2 Layout */
  .row-bottom {
    display: flex;
    justify-content: space-between;

    .medium-card {
      width: 48%;
      display: flex;
      flex-direction: column;
      align-items: center;
      text-align: center;
      padding: 40rpx 0;

      .icon-top {
        font-size: 50rpx;
        margin-bottom: 10rpx;
      }
      .card-title-cn {
        font-size: 30rpx;
      }
      .card-title-en {
        font-size: 20rpx;
        color: #999;
        font-weight: normal;
      }
    }
  }
}
</style>
