<template>
  <view class="container">
    <view class="header-area">
      <view class="search-bar">
        <text class="search-icon">🔍</text>
        <text class="search-text">搜索商品</text>
      </view>
      <view class="promo-banner">
        <text>🎉 首单享受9折优惠</text>
      </view>
    </view>

    <view class="main-body">
      <scroll-view scroll-y
                   class="left-menu">
        <view v-for="(item) in categories"
              :key="item.id"
              class="menu-item"
              :class="{ 'active': currentCategoryId === item.id }"
              @click="switchCategory(item.id)">
          {{ item.name }}
        </view>
      </scroll-view>

      <scroll-view scroll-y
                   class="right-content">
        <view class="category-title">{{ currentCategoryName }}</view>

        <view class="goods-list">
          <view class="goods-item"
                v-for="goods in currentGoodsList"
                :key="goods.id"
                @click="goToDetail(goods)">
            <image :src="getImageUrl(goods.mainImage)"
                   mode="aspectFill"
                   class="goods-img"></image>

            <view class="goods-info">
              <text class="goods-name">{{ goods.name }}</text>
              <text class="goods-desc">{{ goods.description }}</text>

              <view class="price-action-row">
                <text class="goods-price">¥{{ goods.minPrice }}<text class="unit">/份</text></text>

                <view v-if="goods.skuCount > 1"
                      class="spec-btn"
                      @click.stop="openSkuModal(goods)">
                  选规格
                </view>

                <view v-else
                      class="add-btn"
                      @click.stop="addToCart(goods)">
                  <text class="plus">+</text>
                </view>
              </view>
            </view>
          </view>
        </view>
        <view style="height: 100rpx;"></view>
      </scroll-view>
    </view>

    <view class="cart-bar-overlay"
          v-if="cartStore.totalCount > 0"
          @click="showCartDetail = !showCartDetail">

      <view class="cart-detail-popup"
            v-if="showCartDetail"
            @click.stop>

        <view class="popup-header">
          <text class="title">已选商品</text>
          <view class="clear-btn"
                @click="clearCart">
            <text class="icon">🗑️</text>
            <text>清空</text>
          </view>
        </view>

        <view class="cart-coupon-entry"
              @click.stop="openCouponPopup"
              v-if="couponList.length > 0">
          <view class="left">
            <text class="icon">🏷️</text>
            <text class="text">有 {{ couponList.length }} 张优惠券可领取</text>
          </view>
          <view class="right">
            去领取 <view class="css-arrow"></view>
          </view>
        </view>

        <scroll-view scroll-y
                     class="popup-list">
          <view v-for="item in cartStore.cartList"
                :key="`${item.productId}_${item.skuId}`"
                class="cart-item">
            <view class="item-info">
              <text class="name">{{ item.productName }}</text>
              <text class="spec"
                    v-if="item.specName">{{ item.specName }}</text>
            </view>
            <view class="item-price">¥{{ item.price }}</view>

            <view class="action-box">
              <view class="btn minus"
                    @click="cartStore.addToCart({id: item.productId}, {id: item.skuId}, -1)">-</view>
              <text class="num">{{ item.quantity }}</text>
              <view class="btn plus"
                    @click="cartStore.addToCart({id: item.productId}, {id: item.skuId}, 1)">+</view>
            </view>
          </view>
        </scroll-view>
      </view>

      <view class="cart-bar">
        <view class="cart-icon-wrapper">
          <image src="/static/tabbar/cart-icon.png"
                 class="cart-icon" />
          <view class="badge">{{ cartStore.totalCount }}</view>
        </view>
        <view class="cart-price">
          <text class="price">¥{{ cartStore.totalPrice }}</text>
        </view>
        <view class="checkout-btn"
              @click.stop="goToCheckout">
          去结算
        </view>
      </view>
    </view>

    <view class="mask"
          v-if="showCartDetail"
          @click="showCartDetail = false">
    </view>

    <view v-if="cartStore.totalCount > 0"
          style="height: 120rpx;"></view>

    <view class="modal-overlay"
          v-if="showModal"
          @click="closeModal">
      <view class="modal-content"
            @click.stop>
        <view class="close-btn"
              @click="closeModal">×</view>
        <scroll-view scroll-y
                     class="modal-scroll-area">
          <image :src="getImageUrl(selectedProduct?.mainImage)"
                 mode="aspectFill"
                 class="modal-img" />
          <view class="modal-info">
            <text class="modal-name">{{ selectedProduct?.name }}</text>
            <text class="modal-desc">{{ selectedProduct?.description }}</text>
          </view>
        </scroll-view>

        <view class="modal-footer">
          <view class="price-stepper-row">
            <view class="price-box">
              <text class="currency">¥</text>
              <text class="price">{{ (selectedProduct?.minPrice || 0) * quantity }}</text>
              <text class="unit">/份</text>
            </view>
            <view class="stepper">
              <view class="step-btn minus"
                    @click="quantity > 1 ? quantity-- : null"
                    :class="{ disabled: quantity <= 1 }">-</view>
              <text class="step-num">{{ quantity }}</text>
              <view class="step-btn plus"
                    @click="quantity++">+</view>
            </view>
          </view>
          <view class="add-cart-btn"
                @click="confirmAddToCart">加入购物车</view>
        </view>
      </view>
    </view>

    <view class="custom-popup-container"
          v-if="showCouponModal">
      <view class="custom-mask"
            @click="showCouponModal = false"></view>

      <view class="custom-content slide-up-anim">
        <view class="popup-header">
          <text>领取优惠券</text>
          <text class="close-icon"
                @click="showCouponModal = false">×</text>
        </view>

        <scroll-view scroll-y
                     class="coupon-list-scroll">
          <view class="coupon-item"
                v-for="item in couponList"
                :key="item.id">
            <view class="left">
              <text class="price">¥{{ item.amount }}</text>
              <text class="condition">满{{ item.minPoint }}元可用</text>
            </view>
            <view class="right">
              <view class="name">{{ item.name }}</view>
              <view class="time">
                <text v-if="item.timeType === 1">领券后{{ item.days }}天内有效</text>
                <text v-else>有效期至: {{ item.endTime.substring(0, 10) }}</text>
              </view>
            </view>
            <view class="btn"
                  @click="receiveCoupon(item.id)">领取</view>
          </view>
        </scroll-view>
      </view>
    </view>
  </view>
</template>

<script setup lang="ts">
import { ref, computed, onMounted, nextTick } from "vue";
import { onLoad, onShow } from "@dcloudio/uni-app";
import { useCartStore } from "@/store/cart";
import { useUserStore } from "@/store/user";
import type { Product, Category } from "@/types/product"; // 确保路径正确

const baseUrl = "https://localhost:7252";
const userStore = useUserStore();
const cartStore = useCartStore();

const currentCategoryId = ref(1);
const allProducts = ref<Product[]>([]);
const categories = ref<Category[]>([]);

const showModal = ref(false);
const selectedProduct = ref<any>(null);
const quantity = ref(1);
const showCartDetail = ref(false);

// 优惠券相关状态
const couponList = ref<any[]>([]);

const showCouponModal = ref(false);

// --- 加载数据 ---
const loadCategories = () => {
  uni.request({
    url: `${baseUrl}/api/category/list`,
    success: (res: any) => {
      if (res.statusCode === 200) {
        categories.value = res.data;
        if (categories.value.length > 0)
          currentCategoryId.value = categories.value[0].id;
      }
    },
  });
};

const loadProductList = () => {
  uni.request({
    url: `${baseUrl}/api/product/list`,
    success: (res: any) => {
      if (res.statusCode === 200) allProducts.value = res.data;
    },
  });
};

const loadCoupons = () => {
  uni.request({
    url: `${baseUrl}/api/app/coupon/active`,
    success: (res: any) => {
      if (res.statusCode === 200) couponList.value = res.data;
    },
  });
};

// --- 计算属性 ---
const currentGoodsList = computed(() => {
  return allProducts.value.filter(
    (p) => p.categoryId === currentCategoryId.value
  );
});

const currentCategoryName = computed(() => {
  return (
    categories.value.find((c) => c.id === currentCategoryId.value)?.name || ""
  );
});

// --- 交互逻辑 ---
const switchCategory = (id: number) => {
  currentCategoryId.value = id;
};

const openSkuModal = (item: Product) => {
  uni.showLoading({ title: "加载详情..." });
  uni.request({
    url: `${baseUrl}/api/product/${item.id}`,
    success: (res: any) => {
      uni.hideLoading();
      if (res.statusCode === 200) {
        selectedProduct.value = res.data;
        quantity.value = 1;
        showModal.value = true;
      }
    },
  });
};

const closeModal = () => {
  showModal.value = false;
};

const confirmAddToCart = () => {
  if (!selectedProduct.value) return;
  // TODO: 如果有多规格，需要获取选中的 skuId。这里先假定默认规格
  const defaultSku = selectedProduct.value.skus[0];
  cartStore.addToCart(selectedProduct.value, defaultSku, quantity.value);
  uni.showToast({ title: `已加购 ${quantity.value} 份`, icon: "success" });
  closeModal();
};

const addToCart = (goods: Product) => {
  const skuId = goods.defaultSkuId;
  if (skuId === 0)
    return uni.showToast({ title: "暂无规格数据", icon: "none" });
  cartStore.addToCart(goods, { id: skuId }, 1);
};

const clearCart = () => {
  uni.showModal({
    title: "提示",
    content: "确定清空购物车吗？",
    success: (res) => {
      if (res.confirm) {
        cartStore.clearCart();
        showCartDetail.value = false;
      }
    },
  });
};

const goToDetail = (product: Product) => {
  uni.navigateTo({ url: `/pages/product/detail?id=${product.id}` });
};

const goToCheckout = () => {
  uni.navigateTo({ url: "/pages/order/create" });
};

const getImageUrl = (path: string | undefined) => {
  if (!path) return "/static/logo.png";
  if (path.startsWith("http") || path.startsWith("/static")) return path;
  return baseUrl + path;
};

// --- 优惠券逻辑 ---//
const openCouponPopup = () => {
  // 关掉购物车
  showCartDetail.value = false;

  // 延迟 150ms 避开购物车关闭的动画，然后直接把布尔值设为 true
  setTimeout(() => {
    showCouponModal.value = true;
  }, 150);
};

const receiveCoupon = (couponId: number) => {
  if (!userStore.checkAuth()) return;
  uni.showLoading({ title: "领取中..." });
  uni.request({
    url: `${baseUrl}/api/app/coupon/${couponId}/receive?userId=${userStore.userInfo.id}`,
    method: "POST",
    success: (res: any) => {
      uni.hideLoading();
      if (res.statusCode === 200) {
        uni.showToast({ title: "领取成功", icon: "success" });
      } else {
        uni.showToast({ title: res.data || "领取失败", icon: "none" });
      }
    },
  });
};

onLoad(() => {
  loadProductList();
  loadCategories();
});

onShow(() => {
  cartStore.fetchCart();
  loadCoupons(); // 每次显示页面刷新优惠券列表
});
</script>

<style scoped lang="scss">
/* 保持原有的基础样式不变，如 .container, .header-area 等... */
.container {
  display: flex;
  flex-direction: column;
  height: 100vh;
  background-color: #fff;
}
.header-area {
  padding: 20rpx;
  background-color: #fff;
  border-bottom: 1rpx solid #f5f5f5;
}
.search-bar {
  background-color: #f7f7f7;
  height: 64rpx;
  border-radius: 32rpx;
  display: flex;
  align-items: center;
  padding: 0 20rpx;
  margin-bottom: 10rpx;
}
.promo-banner {
  font-size: 22rpx;
  color: #ff6034;
  background-color: #fff0eb;
  padding: 6rpx 12rpx;
  border-radius: 8rpx;
  display: inline-block;
}

.main-body {
  flex: 1;
  display: flex;
  overflow: hidden;
}
.left-menu {
  width: 180rpx;
  background-color: #f7f8fa;
  height: 100%;
  flex-shrink: 0;
}
.left-menu .menu-item {
  height: 100rpx;
  display: flex;
  align-items: center;
  justify-content: center;
  font-size: 26rpx;
  color: #666;
  position: relative;
}
.left-menu .menu-item.active {
  background-color: #fff;
  font-weight: bold;
  color: #333;
}
.left-menu .menu-item.active::before {
  content: "";
  position: absolute;
  left: 0;
  top: 30rpx;
  bottom: 30rpx;
  width: 8rpx;
  background-color: #000;
  border-radius: 0 4rpx 4rpx 0;
}

.right-content {
  flex: 1;
  min-width: 0;
  background-color: #fff;
  height: 100%;
  padding: 0 10rpx;
}
.category-title {
  font-size: 28rpx;
  font-weight: bold;
  padding: 20rpx 0;
  color: #333;
}
.goods-item {
  display: flex;
  margin-bottom: 40rpx;
}
.goods-img {
  width: 180rpx;
  height: 180rpx;
  border-radius: 12rpx;
  background-color: #eee;
  flex-shrink: 0;
}
.goods-info {
  flex: 1;
  margin-left: 20rpx;
  display: flex;
  flex-direction: column;
  justify-content: space-between;
  min-width: 0;
  overflow: hidden;
}
.goods-name {
  font-size: 30rpx;
  font-weight: bold;
  color: #333;
}
.goods-desc {
  font-size: 22rpx;
  color: #999;
  margin-top: 8rpx;
  overflow: hidden;
  text-overflow: ellipsis;
  white-space: nowrap;
}
.price-action-row {
  display: flex;
  justify-content: space-between;
  align-items: center;
  margin-top: 10rpx;
}
.goods-price {
  font-size: 32rpx;
  font-weight: bold;
  color: #333;
}
.goods-price .unit {
  font-size: 22rpx;
  font-weight: normal;
  color: #999;
}
.add-btn {
  width: 50rpx;
  height: 50rpx;
  background-color: #000;
  border-radius: 50%;
  display: flex;
  align-items: center;
  justify-content: center;
}
.add-btn .plus {
  color: #fff;
  font-size: 40rpx;
  line-height: 40rpx;
  margin-top: -4rpx;
}
.spec-btn {
  background-color: #000;
  color: #fff;
  font-size: 24rpx;
  font-weight: bold;
  padding: 10rpx 24rpx;
  border-radius: 30rpx;
  line-height: 1;
  flex-shrink: 0;
}

/* 弹窗样式 */
.modal-overlay {
  position: fixed;
  top: 0;
  left: 0;
  right: 0;
  bottom: 0;
  background-color: rgba(0, 0, 0, 0.6);
  z-index: 999;
  display: flex;
  justify-content: center;
  align-items: center;
}
.modal-content {
  width: 600rpx;
  height: 80vh;
  background-color: #fff;
  border-radius: 24rpx;
  position: relative;
  display: flex;
  flex-direction: column;
  overflow: hidden;
  box-shadow: 0 10rpx 30rpx rgba(0, 0, 0, 0.2);
}
.close-btn {
  position: absolute;
  top: 20rpx;
  right: 20rpx;
  width: 60rpx;
  height: 60rpx;
  background: rgba(0, 0, 0, 0.5);
  color: #fff;
  border-radius: 50%;
  text-align: center;
  line-height: 55rpx;
  font-size: 40rpx;
  z-index: 10;
}
.modal-scroll-area {
  flex: 1;
  overflow-y: auto;
}
.modal-img {
  width: 100%;
  height: 400rpx;
  background-color: #f0f0f0;
}
.modal-info {
  padding: 30rpx;
}
.modal-name {
  font-size: 40rpx;
  font-weight: bold;
  display: block;
  margin-bottom: 10rpx;
}
.modal-desc {
  font-size: 26rpx;
  color: #999;
  line-height: 1.5;
}
.modal-footer {
  padding: 20rpx 30rpx;
  background: #fff;
  border-top: 1rpx solid #f5f5f5;
}
.price-stepper-row {
  display: flex;
  justify-content: space-between;
  align-items: center;
  margin-bottom: 30rpx;
}
.price-box {
  color: #333;
  font-weight: bold;
  font-size: 40rpx;
}
.price-box .currency {
  font-size: 24rpx;
}
.price-box .unit {
  font-size: 24rpx;
  color: #999;
  font-weight: normal;
}
.stepper {
  display: flex;
  align-items: center;
}
.step-btn {
  width: 60rpx;
  height: 60rpx;
  border-radius: 50%;
  border: 2rpx solid #ddd;
  display: flex;
  justify-content: center;
  align-items: center;
  font-size: 36rpx;
  color: #333;
}
.step-btn.minus.disabled {
  color: #ccc;
  border-color: #eee;
}
.step-btn.plus {
  background-color: #000;
  color: #fff;
  border-color: #000;
}
.step-num {
  margin: 0 30rpx;
  font-size: 32rpx;
  font-weight: bold;
}
.add-cart-btn {
  width: 100%;
  height: 90rpx;
  background-color: #000;
  color: #fff;
  border-radius: 45rpx;
  display: flex;
  justify-content: center;
  align-items: center;
  font-size: 32rpx;
  font-weight: bold;
}

/* 购物车遮罩与容器 */
.mask {
  position: fixed;
  top: 0;
  left: 0;
  right: 0;
  bottom: 0;
  background: rgba(0, 0, 0, 0.4);
  z-index: 800;
}
.cart-bar-overlay {
  position: fixed;
  bottom: 20rpx;
  left: 20rpx;
  right: 20rpx;
  z-index: 900;
}

.cart-detail-popup {
  position: absolute;
  bottom: 110rpx;
  left: 0;
  right: 0;
  background-color: #fff;
  border-radius: 24rpx;
  box-shadow: 0 4rpx 30rpx rgba(0, 0, 0, 0.15);
  overflow: hidden;
  display: flex;
  flex-direction: column;
  max-height: 60vh;
  min-height: 200rpx;
  animation: slideUp 0.3s ease-out;
}
@keyframes slideUp {
  from {
    transform: translateY(20rpx);
    opacity: 0;
  }
  to {
    transform: translateY(0);
    opacity: 1;
  }
}

.popup-header {
  height: 80rpx;
  background-color: #f7f8fa;
  display: flex;
  justify-content: space-between;
  align-items: center;
  padding: 0 30rpx;
  flex-shrink: 0;
}
.popup-header .title {
  font-size: 28rpx;
  font-weight: bold;
  color: #333;
}
.popup-header .clear-btn {
  display: flex;
  align-items: center;
  font-size: 24rpx;
  color: #999;
}

/* 🔥 购物车内的领券横幅 */
.cart-coupon-entry {
  background-color: #fff0eb;
  padding: 16rpx 30rpx;
  display: flex;
  justify-content: space-between;
  align-items: center;
  flex-shrink: 0;
}
.cart-coupon-entry .left {
  display: flex;
  align-items: center;
}
.cart-coupon-entry .left .icon {
  font-size: 28rpx;
  margin-right: 10rpx;
}
.cart-coupon-entry .left .text {
  font-size: 24rpx;
  color: #ff6034;
  font-weight: bold;
}
.cart-coupon-entry .right {
  font-size: 24rpx;
  color: #ff6034;
  display: flex;
  align-items: center;
}
.css-arrow {
  width: 12rpx;
  height: 12rpx;
  border-top: 3rpx solid currentColor;
  border-right: 3rpx solid currentColor;
  transform: rotate(45deg);
  margin-left: 8rpx;
}

.popup-list {
  flex: 1;
  min-height: 0;
  max-height: 40vh;
  overflow-y: auto;
  padding: 0 20rpx;
  box-sizing: border-box;
  background-color: #fff;
}
.cart-item {
  display: flex;
  justify-content: space-between;
  align-items: center;
  padding: 24rpx 0;
  border-bottom: 1rpx solid #f5f5f5;
  padding-right: 10rpx;
}
.cart-item .item-info {
  flex: 1;
  overflow: hidden;
}
.cart-item .item-info .name {
  font-size: 28rpx;
  color: #333;
  display: block;
}
.cart-item .item-info .spec {
  font-size: 22rpx;
  color: #999;
  margin-top: 4rpx;
  display: block;
}
.cart-item .item-price {
  font-size: 30rpx;
  font-weight: bold;
  color: #333;
  margin: 0 30rpx;
  min-width: 80rpx;
  text-align: right;
}
.action-box {
  display: flex;
  align-items: center;
}
.action-box .btn {
  width: 44rpx;
  height: 44rpx;
  border-radius: 50%;
  display: flex;
  justify-content: center;
  align-items: center;
  font-weight: bold;
  font-size: 32rpx;
  line-height: 44rpx;
}
.action-box .btn.minus {
  border: 1rpx solid #ddd;
  color: #333;
}
.action-box .btn.plus {
  background: #000;
  color: #fff;
}
.action-box .num {
  margin: 0 20rpx;
  font-size: 28rpx;
  font-weight: bold;
  min-width: 40rpx;
  text-align: center;
}

.cart-bar {
  background-color: #222;
  height: 100rpx;
  border-radius: 50rpx;
  display: flex;
  align-items: center;
  padding: 0 30rpx;
  box-shadow: 0 10rpx 20rpx rgba(0, 0, 0, 0.3);
  position: relative;
  z-index: 999;
}
.cart-icon-wrapper {
  position: relative;
  margin-top: -30rpx;
  margin-right: 20rpx;
}
.cart-icon {
  width: 90rpx;
  height: 90rpx;
}
.badge {
  position: absolute;
  right: 0;
  top: 0;
  background: #ff4d4f;
  color: #fff;
  font-size: 20rpx;
  padding: 4rpx 12rpx;
  border-radius: 20rpx;
}
.cart-price {
  flex: 1;
  color: #fff;
  font-size: 36rpx;
  font-weight: bold;
}
.checkout-btn {
  background: #1677ff;
  color: #fff;
  font-weight: bold;
  padding: 10rpx 30rpx;
  border-radius: 30rpx;
}

/* 🔥 优惠券弹窗样式 */
.custom-popup-container {
  position: fixed;
  top: 0;
  left: 0;
  right: 0;
  bottom: 0;
  z-index: 1000; /* 层级极高，绝不被遮挡 */
}

.custom-mask {
  position: absolute;
  top: 0;
  left: 0;
  right: 0;
  bottom: 0;
  background: rgba(0, 0, 0, 0.5); /* 半透明黑底 */
}

.custom-content {
  position: absolute;
  bottom: 0;
  left: 0;
  right: 0;
  background: #f5f7fa;
  border-radius: 24rpx 24rpx 0 0;
  padding-bottom: env(safe-area-inset-bottom);
  display: flex;
  flex-direction: column;
}

/* 丝滑的上滑动画 */
.slide-up-anim {
  animation: slideUpBottom 0.3s ease-out forwards;
}

@keyframes slideUpBottom {
  from {
    transform: translateY(100%);
  }
  to {
    transform: translateY(0);
  }
}

.custom-content .popup-header {
  text-align: center;
  font-size: 32rpx;
  font-weight: bold;
  padding: 30rpx 0;
  background: #fff;
  border-radius: 24rpx 24rpx 0 0;
  position: relative;
  color: #333;
}

.custom-content .close-icon {
  position: absolute;
  right: 30rpx;
  top: 50%;
  transform: translateY(-50%);
  font-size: 44rpx;
  color: #999;
  line-height: 1;
  padding: 10rpx; /* 增加点击热区 */
}

.coupon-list-scroll {
  max-height: 60vh;
  padding: 20rpx;
  box-sizing: border-box;
}

.coupon-item {
  display: flex;
  background: #fff;
  border-radius: 12rpx;
  margin-bottom: 20rpx;
  overflow: hidden;
}
.coupon-item .left {
  width: 200rpx;
  background: #ffe4e4;
  color: #ff4d4f;
  display: flex;
  flex-direction: column;
  justify-content: center;
  align-items: center;
  padding: 20rpx;
}
.coupon-item .left .price {
  font-size: 40rpx;
  font-weight: bold;
}
.coupon-item .left .condition {
  font-size: 22rpx;
  margin-top: 10rpx;
}
.coupon-item .right {
  flex: 1;
  padding: 20rpx;
  display: flex;
  flex-direction: column;
  justify-content: center;
}
.coupon-item .right .name {
  font-size: 28rpx;
  font-weight: bold;
  color: #333;
  margin-bottom: 10rpx;
}
.coupon-item .right .time {
  font-size: 22rpx;
  color: #999;
}
.coupon-item .btn {
  align-self: center;
  margin-right: 20rpx;
  background: #ff4d4f;
  color: #fff;
  padding: 10rpx 30rpx;
  border-radius: 30rpx;
  font-size: 24rpx;
}
</style>