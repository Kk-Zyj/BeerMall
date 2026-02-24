<template>
  <view class="page-container"
        v-if="product">

    <image :src="getImageUrl(product.mainImage)"
           mode="aspectFill"
           class="hero-image" />

    <view class="content-area">
      <view class="header-section">
        <text class="title">{{ product.name }}</text>
        <view class="tags">
          <text class="tag">{{ product.categoryName || '精选' }}</text>
        </view>
        <view class="member-tip"
              v-if="currentSku">
          <text class="label">入会可享</text>
          <text class="price">¥{{ (currentSku.price * 0.8).toFixed(1) }}</text>
        </view>
        <text class="desc">{{ product.description }}</text>
      </view>

      <view class="spec-section">
        <text class="section-title">规格</text>
        <view class="sku-list">
          <view v-for="sku in product.skus"
                :key="sku.id"
                class="sku-item"
                :class="{ 'active': currentSku?.id === sku.id }"
                @click="selectSku(sku)">
            {{ sku.specName }}
          </view>
        </view>
      </view>

      <view class="detail-section">
        <text class="section-title">详情</text>
        <view class="rich-content">
          <up-parse :content="product.content"></up-parse>
        </view>
      </view>
    </view>

    <view class="bottom-bar-placeholder"></view>
    <view class="custom-bottom-bar">

      <view class="left-info">
        <view class="price-box">
          <text class="currency">¥</text>
          <text class="price">{{ currentSku?.price }}</text>
          <text class="unit">/{{ unitName }}</text>
        </view>
        <text class="selected-tip"
              v-if="currentSku">{{ currentSku.specName }}</text>
      </view>

      <view class="right-actions">
        <view class="stepper">
          <view class="step-btn minus"
                @click="quantity > 1 ? quantity-- : null">-</view>
          <text class="step-num">{{ quantity }}</text>
          <view class="step-btn plus"
                @click="quantity++">+</view>
        </view>

        <view class="add-btn"
              @click="addToCart">加入购物车</view>
      </view>
    </view>

  </view>
</template>

<script setup lang="ts">
import { ref, computed } from "vue";
import { onLoad } from "@dcloudio/uni-app";
import type { ProductDetail, Sku } from "@/types/product";
import { useCartStore } from "@/store/cart";

const product = ref<ProductDetail | null>(null);
const currentSku = ref<Sku | null>(null);
const quantity = ref(1);
const baseUrl = "https://localhost:7252";
const cartStore = useCartStore();

// 计算属性：尝试从规格名中提取单位（例如 "330ml/杯" 提取 "杯"）
// 如果没有，默认显示 "份"
const unitName = computed(() => {
  if (!currentSku.value) return "份";
  const name = currentSku.value.specName;
  if (name.includes("/")) return name.split("/")[1];
  return "份";
});

// 加载数据
const loadProductDetail = (id: string) => {
  uni.request({
    url: `${baseUrl}/api/product/${id}`,
    method: "GET",
    success: (res: any) => {
      if (res.statusCode === 200) {
        product.value = res.data;
        // 🔥 初始化：默认选中第一个规格
        if (product.value?.skus?.length > 0) {
          selectSku(product.value.skus[0]);
        }
      }
    },
  });
};

// 选择规格 (Command)
const selectSku = (sku: Sku) => {
  currentSku.value = sku;
  quantity.value = 1; // 切换规格后重置数量
};

// 加入购物车
const addToCart = async () => {
  if (!currentSku.value) return;

  // 1. 等待后端 API 执行完毕 (关键！确保数据库已经存进去了)
  await cartStore.addToCart(product.value!, currentSku.value!, quantity.value);

  // 2. 显示成功提示
  uni.showToast({
    title: "已加入购物车",
    icon: "success",
    duration: 1500, // 提示框停留时间
  });

  // 3. 延迟 800ms 后自动返回上一页 (菜单页)
  // 这样用户能看清 "已加入" 的勾，体验更好
  setTimeout(() => {
    uni.navigateBack({
      delta: 1, // 返回层数，1代表返回上一页
    });
  }, 800);
};

// 图片处理
const getImageUrl = (path: string | undefined) => {
  if (!path) return "";
  if (path.startsWith("http")) return path;
  if (path.startsWith("/static")) return path;
  return baseUrl + path;
};

onLoad((options) => {
  if (options && options.id) loadProductDetail(options.id);
});
</script>

<style scoped lang="scss">
.page-container {
  padding-bottom: 120rpx; /* 底部留白 */
  background-color: #fff;
}

.hero-image {
  width: 100%;
  height: 600rpx; /* 图片高度 */
  background-color: #f8f8f8;
}

.content-area {
  padding: 30rpx;
}

/* 标题区 */
.header-section {
  margin-bottom: 40rpx;
  .title {
    font-size: 40rpx;
    font-weight: bold;
    color: #000;
    margin-bottom: 10rpx;
    display: block;
  }

  .tags {
    margin-bottom: 20rpx;
    .tag {
      font-size: 24rpx;
      color: #666;
      background: #f0f0f0;
      padding: 4rpx 12rpx;
      border-radius: 4rpx;
      margin-right: 10rpx;
    }
  }

  .member-tip {
    display: inline-flex;
    align-items: center;
    background: #fff0e0;
    color: #d68e57;
    font-size: 24rpx;
    padding: 6rpx 12rpx;
    border-radius: 8rpx;
    margin-bottom: 20rpx;
    .price {
      font-weight: bold;
      margin-left: 6rpx;
    }
  }

  .desc {
    font-size: 26rpx;
    color: #666;
    line-height: 1.5;
    display: block;
  }
}

/* 规格选择区 (UI 重点) */
.spec-section {
  margin-bottom: 40rpx;
  .section-title {
    font-size: 30rpx;
    font-weight: bold;
    margin-bottom: 20rpx;
    display: block;
  }

  .sku-list {
    display: flex;
    flex-wrap: wrap;

    .sku-item {
      padding: 16rpx 30rpx;
      margin-right: 20rpx;
      margin-bottom: 20rpx;
      font-size: 28rpx;
      border-radius: 8rpx; /* 方一点的圆角，像截图那样 */

      /* 默认状态：灰底，无框 */
      background-color: #f7f8fa;
      color: #333;
      border: 2rpx solid transparent;

      /* 选中状态：白底，黑框，黑字 */
      &.active {
        background-color: #fff;
        border-color: #000;
        color: #000;
        font-weight: bold;
      }
    }
  }
}

.detail-section {
  .section-title {
    font-size: 30rpx;
    font-weight: bold;
    margin-bottom: 20rpx;
    display: block;
  }
  .rich-content {
    font-size: 28rpx;
    color: #333;
    line-height: 1.6;
  }
}

/* 底部栏 (UI 重点) */
.bottom-bar-placeholder {
  height: 120rpx;
  padding-bottom: env(safe-area-inset-bottom);
}
.custom-bottom-bar {
  position: fixed;
  bottom: 0;
  left: 0;
  right: 0;
  background: #fff;
  border-top: 1rpx solid #eee;
  padding: 20rpx 30rpx;
  padding-bottom: calc(20rpx + env(safe-area-inset-bottom));
  display: flex;
  justify-content: space-between;
  align-items: center;
  z-index: 99;

  .left-info {
    .price-box {
      color: #000;
      font-weight: bold;
      .currency {
        font-size: 24rpx;
      }
      .price {
        font-size: 44rpx;
      }
      .unit {
        font-size: 24rpx;
        color: #999;
        font-weight: normal;
        margin-left: 4rpx;
      }
    }
    .selected-tip {
      font-size: 22rpx;
      color: #999;
      display: block;
    }
  }

  .right-actions {
    display: flex;
    align-items: center;

    .stepper {
      display: flex;
      align-items: center;
      margin-right: 30rpx;
      .step-btn {
        width: 50rpx;
        height: 50rpx;
        border: 2rpx solid #ddd;
        border-radius: 50%;
        display: flex;
        align-items: center;
        justify-content: center;
        font-size: 30rpx;
        &.plus {
          background: #000;
          color: #fff;
          border-color: #000;
        }
      }
      .step-num {
        margin: 0 20rpx;
        font-weight: bold;
        font-size: 30rpx;
      }
    }

    .add-btn {
      background: #000;
      color: #fff;
      padding: 0 40rpx;
      height: 80rpx;
      line-height: 80rpx;
      border-radius: 40rpx;
      font-weight: bold;
      font-size: 28rpx;
    }
  }
}
</style>