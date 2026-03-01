<template>
  <view class="page-container">

    <view class="empty-state"
          v-if="cartStore.totalCount === 0">
      <image src="src:🛒"
             mode="widthFix"
             class="empty-img" />
      <text class="empty-text">购物车还是空的</text>
      <text class="empty-sub">快去来杯精酿解解渴吧</text>

      <view class="go-btn"
            @click="goToMenu">去逛逛</view>
    </view>

    <view class="cart-content"
          v-else>

      <view class="top-bar">
        <text class="count">共 {{ cartStore.totalCount }} 件商品</text>
        <text class="clear-btn"
              @click="handleClear">清空</text>
      </view>

      <scroll-view scroll-y
                   class="goods-list">
        <view class="cart-item"
              v-for="item in cartStore.cartList"
              :key="item.itemId">

          <image :src="getImageUrl(item.image)"
                 mode="aspectFill"
                 class="item-img" />

          <view class="item-info">
            <view class="title-row">
              <text class="name">{{ item.productName }}</text>
              <text class="spec"
                    v-if="item.specName">{{ item.specName }}</text>
            </view>

            <view class="price-row">
              <text class="price">¥{{ item.price }}</text>

              <view class="stepper">
                <view class="step-btn minus"
                      @click="updateQuantity(item, -1)">-</view>
                <text class="step-num">{{ item.quantity }}</text>
                <view class="step-btn plus"
                      @click="updateQuantity(item, 1)">+</view>
              </view>
            </view>
          </view>
        </view>

        <view style="height: 120rpx;"></view>
      </scroll-view>

      <view class="bottom-bar">
        <view class="total-info">
          <text class="label">合计:</text>
          <text class="price">¥{{ cartStore.totalPrice }}</text>
        </view>
        <view class="checkout-btn"
              @click="goToCheckout">
          去结算
        </view>
      </view>
    </view>

  </view>
</template>

<script setup lang="ts">
import { onShow } from "@dcloudio/uni-app";
import { useCartStore } from "@/store/cart";
import { API_BASE_URL } from "@/config/api";

const cartStore = useCartStore();

// 每次进入页面，从数据库拉取最新数据
onShow(() => {
  cartStore.refresh();
});

// --- 事件处理 ---

// 1. 跳转去点餐页
const goToMenu = () => {
  // 注意：因为 'pages/category/category' 是 TabBar 页面，必须用 switchTab
  uni.switchTab({
    url: "/pages/category/category",
  });
};

// 2. 加减数量 (直接调用 Store，Store 会调用 API)
const updateQuantity = (item: any, change: number) => {
  // 构造 store 需要的参数格式
  cartStore.addToCart({
    productId: item.productId,
    skuId: item.skuId,
    quantity: change,
  });
};

// 3. 清空购物车
const handleClear = () => {
  uni.showModal({
    title: "提示",
    content: "确定要清空购物车吗？",
    success: (res) => {
      if (res.confirm) {
        cartStore.clear();
      }
    },
  });
};

// 4. 去结算
const goToCheckout = () => {
  uni.navigateTo({
    url: "/pages/order/create", // 下一步我们要做的页面
  });
};

// 图片处理工具
const getImageUrl = (path: string | undefined) => {
  if (!path) return "";
  if (path.startsWith("http")) return path;
  if (path.startsWith("/static")) return path;
  return API_BASE_URL + path;
};
</script>

<style scoped lang="scss">
.page-container {
  min-height: 100vh;
  background-color: #f5f7fa;
  display: flex;
  flex-direction: column;
}

/* --- 空状态样式 --- */
.empty-state {
  display: flex;
  flex-direction: column;
  align-items: center;
  justify-content: center;
  padding-top: 200rpx;

  .empty-img {
    width: 300rpx;
    margin-bottom: 40rpx;
  }

  .empty-text {
    font-size: 32rpx;
    color: #333;
    font-weight: bold;
    margin-bottom: 16rpx;
  }

  .empty-sub {
    font-size: 26rpx;
    color: #999;
    margin-bottom: 60rpx;
  }

  .go-btn {
    width: 240rpx;
    height: 80rpx;
    line-height: 80rpx;
    text-align: center;
    background-color: #000;
    color: #fff;
    border-radius: 40rpx;
    font-weight: bold;
    font-size: 28rpx;
    box-shadow: 0 10rpx 20rpx rgba(0, 0, 0, 0.15);
  }
}

/* --- 列表状态样式 --- */
.cart-content {
  flex: 1;
  display: flex;
  flex-direction: column;
  height: 100vh;
}

.top-bar {
  padding: 20rpx 30rpx;
  display: flex;
  justify-content: space-between;
  align-items: center;
  font-size: 26rpx;
  color: #666;
  background-color: #fff;
  border-bottom: 1rpx solid #f5f5f5;
  .clear-btn {
    color: #999;
  }
}

.goods-list {
  flex: 1;
  padding: 20rpx;
  box-sizing: border-box;

  .cart-item {
    background-color: #fff;
    border-radius: 20rpx;
    padding: 20rpx;
    margin-bottom: 20rpx;
    display: flex;
    align-items: center;

    .item-img {
      width: 140rpx;
      height: 140rpx;
      border-radius: 12rpx;
      background-color: #f8f8f8;
      flex-shrink: 0;
    }

    .item-info {
      flex: 1;
      margin-left: 20rpx;
      height: 140rpx;
      display: flex;
      flex-direction: column;
      justify-content: space-between;

      .title-row {
        .name {
          font-size: 30rpx;
          font-weight: bold;
          color: #333;
          display: block;
        }
        .spec {
          font-size: 22rpx;
          color: #999;
          background: #f7f8fa;
          padding: 2rpx 10rpx;
          border-radius: 6rpx;
          display: inline-block;
          margin-top: 8rpx;
        }
      }

      .price-row {
        display: flex;
        justify-content: space-between;
        align-items: flex-end;

        .price {
          font-size: 34rpx;
          font-weight: bold;
          color: #333;
        }

        .stepper {
          display: flex;
          align-items: center;
          .step-btn {
            width: 44rpx;
            height: 44rpx;
            border-radius: 50%;
            display: flex;
            justify-content: center;
            align-items: center;
            font-size: 32rpx;
            &.minus {
              border: 2rpx solid #ddd;
              color: #666;
            }
            &.plus {
              background: #000;
              color: #fff;
              border: 2rpx solid #000;
            }
          }
          .step-num {
            width: 60rpx;
            text-align: center;
            font-weight: bold;
            font-size: 28rpx;
          }
        }
      }
    }
  }
}

/* 底部结算栏 */
.bottom-bar {
  position: fixed;
  bottom: 0;
  left: 0;
  right: 0;
  background-color: #fff;
  padding: 20rpx 30rpx;
  padding-bottom: calc(20rpx + env(safe-area-inset-bottom));
  display: flex;
  justify-content: space-between;
  align-items: center;
  box-shadow: 0 -2rpx 10rpx rgba(0, 0, 0, 0.05);
  z-index: 99;

  .total-info {
    display: flex;
    align-items: flex-end;
    .label {
      font-size: 28rpx;
      color: #333;
      margin-right: 10rpx;
      margin-bottom: 4rpx;
    }
    .price {
      font-size: 48rpx;
      font-weight: bold;
      color: #000;
      line-height: 1;
    }
  }

  .checkout-btn {
    background-color: #000;
    color: #fff;
    padding: 0 60rpx;
    height: 80rpx;
    line-height: 80rpx;
    border-radius: 40rpx;
    font-weight: bold;
    font-size: 30rpx;
  }
}
</style>
