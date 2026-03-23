<template>
  <view class="page-container">

    <view class="notice-card">
      <view class="notice-title">温馨提示：</view>
      <view class="notice-item">1: 发货后不支持7天无理由退货；</view>
      <view class="notice-item">2: 收到货后请先查验外包装是否异常；</view>
      <view class="notice-item">3: 酒水在运输途中可能会有少量渗漏，不作为售后处理。</view>
    </view>

    <view class="address-card"
          @click="chooseAddress">
      <template v-if="deliveryType !== 'self'">
        <view class="has-address"
              v-if="currentAddress">
          <view class="addr-icon">📍</view>
          <view class="addr-info">
            <view class="row1">
              <text class="city">{{ currentAddress.province }} {{ currentAddress.city }}</text>
            </view>
            <view class="detail">{{ currentAddress.district }} {{ currentAddress.detail }}</view>
            <view class="user">{{ currentAddress.name }} {{ currentAddress.mobile }}</view>
          </view>
          <view class="css-arrow"></view>
        </view>
        <view class="no-address"
              v-else>
          <text>请选择收货地址</text>
          <view class="css-arrow"></view>
        </view>
      </template>

      <template v-else>
        <view class="has-address">
          <view class="addr-icon">🏢</view>
          <view class="addr-info">
            <view class="detail">请选择自提仓库 (功能开发中)</view>
            <view class="user">默认: 北京总仓</view>
          </view>
          <view class="css-arrow"></view>
        </view>
      </template>
    </view>

    <view class="goods-card">
      <view class="goods-item"
            v-for="item in cartStore.cartList"
            :key="item.itemId">
        <image :src="getImageUrl(item.image)"
               class="thumb"
               mode="aspectFill"></image>
        <view class="info">
          <text class="name">{{ item.productName }}</text>
          <text class="spec"
                v-if="item.specName">{{ item.specName }}</text>
          <view class="price-row">
            <text class="price">¥{{ item.price }}</text>
            <text class="num">x {{ item.quantity }}</text>
          </view>
        </view>
      </view>
    </view>

    <view class="option-card">
      <view class="row-item">
        <text class="label">配送方式</text>
        <view class="radio-group">
          <view class="radio-item"
                :class="{ active: deliveryType === 'express' }"
                @click="deliveryType = 'express'">顺丰快递</view>
          <view class="radio-item"
                :class="{ active: deliveryType === 'local' }"
                @click="deliveryType = 'local'">同城配送</view>
          <view class="radio-item"
                :class="{ active: deliveryType === 'self' }"
                @click="deliveryType = 'self'">仓库自提</view>
        </view>
      </view>

      <view class="row-item input-row">
        <text class="label">订单备注</text>
        <input class="input"
               v-model="remark"
               placeholder="建议留言前先与客服沟通" />
      </view>
    </view>

    <view class="cost-row"
          @click="openCouponList">
      <text>优惠券</text>
      <view class="right-box"
            v-if="orderType === 0">
        <text class="red discount-num"
              v-if="selectedCoupon">- ¥{{ selectedCoupon.amount }}</text>
        <text class="gray-text"
              v-else-if="availableCoupons.length > 0">
          {{ availableCoupons.length }}张可用
        </text>
        <text class="gray-text"
              v-else>无可用</text>
        <view class="css-arrow"></view>
      </view>
      <view class="right-box"
            v-else>
        <text class="gray-text">拼团不可用券</text>
      </view>
    </view>

    <uni-popup ref="selectCouponPopup"
               type="bottom">
      <view class="popup-box">
        <view class="popup-header">选择优惠券</view>
        <scroll-view scroll-y
                     class="coupon-list">
          <view class="coupon-item"
                v-for="item in availableCoupons"
                :key="item.id"
                @click="selectCoupon(item)">
            <view class="left">
              <text class="price">¥{{ item.amount }}</text>
              <text class="condition">满{{ item.minPoint }}元可用</text>
            </view>
            <view class="right">
              <view class="name">{{ item.name }}</view>
              <view class="time">有效期至: {{ item.expireTime.substring(0, 10) }}</view>
            </view>
            <view class="checked"
                  v-if="selectedCoupon && selectedCoupon.id === item.id">✓</view>
          </view>

          <view class="no-use-btn"
                @click="selectCoupon(null)">不使用优惠券</view>
        </scroll-view>
      </view>
    </uni-popup>

    <view style="height: 240rpx;"></view>

    <view class="footer-fixed">

      <view class="buy-type-tabs">
        <view class="tab-item"
              :class="{ active: orderType === 0, disabled: isJoinGroupMode }"
              @click="switchType(0)">
          <view class="row-1">单独购买</view>
          <view class="row-2">¥{{ normalFinalPrice }}</view>
        </view>

        <view class="tab-item group-tab"
              :class="{ active: orderType === 1 || orderType === 2 }"
              @click="switchType(1)">
          <view class="row-1">
            拼团购买 <text class="tag">{{ groupRule && groupRule.discountRate ? (groupRule.discountRate * 10).toFixed(1) : '--' }}折</text>
          </view>
          <view class="row-2">¥{{ groupFinalPrice }}</view>
        </view>
      </view>

      <view class="bottom-bar">
        <view class="left">
          <text class="label">实付:</text>
          <text class="price">¥{{ currentPayPrice }}</text>

          <text class="discount"
                v-if="orderType !== 0">
            已省 ¥{{ (parseFloat(normalFinalPrice) - parseFloat(groupFinalPrice)).toFixed(2) }}
          </text>
        </view>

        <view class="submit-btn"
              :class="{ 'group-btn': orderType !== 0 }"
              @click="submitOrder">
          {{ submitBtnText }}
        </view>
      </view>
    </view>
    <login-popup />
  </view>
</template>

<script setup lang="ts">
import LoginPopup from "@/components/login-popup/login-popup.vue";
import { ref, computed, watch } from "vue";
import { onShow, onLoad } from "@dcloudio/uni-app";
import { useCartStore } from "@/store/cart";
import { useAuthStore } from "@/store/auth";
import { getDeviceId } from "@/utils/device";
import type { GroupByRule } from "@/types/groupByRule";
import { useOrderPay } from "@/composables/useOrderPay";
import { API_BASE_URL } from "@/config/api";
import { apiGetGroupRule } from "@/api/admin";
import { apiMyAvailableCoupons } from "@/api/coupon";
import { apiAddressList } from "@/api/address";
import { apiCreateOrder } from "@/api/order";

const cartStore = useCartStore();
const { payOrder } = useOrderPay();

const currentAddress = ref<any>(null);
const deliveryType = ref("express");
const remark = ref("");
const userStore = useAuthStore();
const targetGroupBuyId = ref<any>(null); // 目标团ID

// 拼团相关状态
const isJoinGroupMode = ref(false); // 是否是“被邀请参团”模式
const groupRule = ref<GroupByRule>();

// 订单类型：0普通、1开团、2参团
const orderType = ref(0);

const freightPrice = computed(() => {
  if (deliveryType.value === "self") return 0;
  if (cartStore.totalPrice >= 299) return 0;
  if (deliveryType.value === "express") return 10.0;
  if (deliveryType.value === "local") return 12.0;
  return 0;
});

// =========================
// 优惠券（优化版）
// =========================
type CouponItem = {
  id: number; // userCouponId
  name: string;
  amount: number;
  minPoint: number;
  expireTime: string;
};

const availableCoupons = ref<CouponItem[]>([]);
const selectedCoupon = ref<CouponItem | null>(null);
const selectCouponPopup = ref<any>(null);

const userTouchedCoupon = ref(false); // 用户是否手动选过券
const couponLoading = ref(false);

// 用于避免重复请求：同一金额只拉一次（除非 force）
let lastCouponKey = "";
let inflightCouponPromise: Promise<CouponItem[]> | null = null;

function pickBestCoupon(list: CouponItem[]) {
  // 优先：优惠金额最大；若相同：门槛 minPoint 更低优先；若还相同：过期更晚优先
  const sorted = [...list].sort((a, b) => {
    if (b.amount !== a.amount) return b.amount - a.amount;
    if (a.minPoint !== b.minPoint) return a.minPoint - b.minPoint;
    return (b.expireTime || "").localeCompare(a.expireTime || "");
  });
  return sorted[0] ?? null;
}

// 订单“商品金额”（后端校验优惠券门槛用 productAmount，不含运费）
// 你后端 OrderController 的 coupon 校验是：productAmount < MinPoint => 不可用
const couponOrderAmount = computed(() => cartStore.totalPrice);

async function loadMyCoupons(force = false) {
  // 拼团不可用券：清空选择
  if (orderType.value !== 0) {
    availableCoupons.value = [];
    if (selectedCoupon.value) {
      selectedCoupon.value = null;
    }
    return;
  }

  const uid = userStore.userInfo?.id;
  if (!uid) return;

  const amount = couponOrderAmount.value;
  const key = `${uid}:${Math.round(amount * 100)}`;

  if (!force && key === lastCouponKey && availableCoupons.value.length > 0) {
    return;
  }

  if (inflightCouponPromise) {
    await inflightCouponPromise;
    return;
  }

  couponLoading.value = true;
  lastCouponKey = key;

  inflightCouponPromise = (async () => {
    try {
      const list = (await apiMyAvailableCoupons(uid, amount)) as any[];

      // 兼容你接口返回结构：这里假设已经是扁平字段（create.vue 原本就是这样用的）
      const normalized: CouponItem[] = (list || []).map((x: any) => ({
        id: x.id,
        name: x.name,
        amount: Number(x.amount ?? 0),
        minPoint: Number(x.minPoint ?? 0),
        expireTime: String(x.expireTime ?? ""),
      }));

      availableCoupons.value = normalized;

      // 如果当前选中的券已经不在可用列表里：自动取消
      if (selectedCoupon.value) {
        const stillOk = normalized.some(
          (c) => c.id === selectedCoupon.value!.id
        );
        if (!stillOk) {
          selectedCoupon.value = null;
          if (userTouchedCoupon.value) {
            uni.showToast({
              title: "所选优惠券已不可用，已为你取消",
              icon: "none",
            });
          }
        }
      }

      // 未手动选择过：自动选最优券
      if (
        !userTouchedCoupon.value &&
        !selectedCoupon.value &&
        normalized.length > 0
      ) {
        selectedCoupon.value = pickBestCoupon(normalized);
      }

      return normalized;
    } catch (e: any) {
      uni.showToast({ title: e?.message || "加载优惠券失败", icon: "none" });
      availableCoupons.value = [];
      // 不强制清掉已选券，避免瞬间抖动（除非你想更严格）
      return [];
    } finally {
      couponLoading.value = false;
      inflightCouponPromise = null;
    }
  })();

  await inflightCouponPromise;
}

// 打开与选择券
const openCouponList = async () => {
  if (orderType.value !== 0) {
    return uni.showToast({ title: "拼团不可用券", icon: "none" });
  }

  // 先确保列表最新
  await loadMyCoupons(false);

  if (availableCoupons.value.length === 0) {
    return uni.showToast({ title: "暂无可用券", icon: "none" });
  }
  selectCouponPopup.value.open();
};

const selectCoupon = (item: CouponItem | null) => {
  userTouchedCoupon.value = true;
  selectedCoupon.value = item;
  selectCouponPopup.value.close();
};

// 普通购买最终价（含运费 - 优惠）
const normalFinalPrice = computed(() => {
  const discount = selectedCoupon.value ? selectedCoupon.value.amount : 0;
  const total = cartStore.totalPrice + freightPrice.value - discount;
  return (total > 0 ? total : 0.01).toFixed(2);
});

// 拼团购买价格 = (商品 + 运费) * 折扣率（你现有逻辑保留）
const groupFinalPrice = computed(() => {
  const baseTotal = cartStore.totalPrice + freightPrice.value;
  const discountRate = groupRule.value?.discountRate ?? 1;
  const total = baseTotal * discountRate;
  return (total > 0 ? total : 0).toFixed(2);
});

// 当前实际支付价格（底部显示）
const currentPayPrice = computed(() => {
  if (orderType.value === 0) return normalFinalPrice.value;
  return groupFinalPrice.value;
});

// 按钮文案
const submitBtnText = computed(() => {
  if (orderType.value === 0) return "提交订单";
  if (orderType.value === 2) return "立即参团";
  return "发起拼单";
});

// 切换购买类型
const switchType = (type: number) => {
  if (isJoinGroupMode.value && type === 0) {
    return uni.showToast({ title: "参团模式下不可切换", icon: "none" });
  }

  if (type === 1) {
    orderType.value = isJoinGroupMode.value ? 2 : 1;
  } else {
    orderType.value = 0;
  }

  // 切到拼团：清空券（并提示一次）
  if (orderType.value !== 0) {
    if (selectedCoupon.value) selectedCoupon.value = null;
    if (availableCoupons.value.length) availableCoupons.value = [];
    userTouchedCoupon.value = false;
  } else {
    // 切回普通：重新拉券并自动选最优（如果用户没手选）
    loadMyCoupons(true);
  }
};

// 监听金额变化：自动刷新券（防止券列表与金额不同步）
let couponTimer: any = null;
watch(
  () => [couponOrderAmount.value, orderType.value],
  () => {
    if (couponTimer) clearTimeout(couponTimer);
    couponTimer = setTimeout(() => {
      loadMyCoupons(false);
    }, 250);
  },
  { immediate: false }
);

onLoad(async (options) => {
  try {
    groupRule.value = await apiGetGroupRule();
  } catch (err) {
    console.error("获取规则失败:", err);
  }

  if (options && (options as any).groupBuyId) {
    targetGroupBuyId.value = (options as any).groupBuyId;
    orderType.value = 2;
    isJoinGroupMode.value = true;
  }
});

onShow(async () => {
  if (!(await userStore.checkAuth(false))) return;

  if (!currentAddress.value) {
    await loadDefaultAddress();
  }

  uni.$once("selectAddress", (addr: any) => {
    currentAddress.value = addr;
  });

  loadMyCoupons(true);
});

const loadDefaultAddress = async () => {
  try {
    const list = await apiAddressList();
    if (list && list.length > 0) {
      const def = list.find((a: any) => a.isDefault) || list[0];
      currentAddress.value = def;
    }
  } catch (e: any) {
    uni.showToast({ title: e?.message || "加载地址失败", icon: "none" });
  }
};

const chooseAddress = () => {
  if (deliveryType.value === "self") {
    uni.showToast({ title: "自提暂不支持选择其他仓库", icon: "none" });
    return;
  }
  uni.navigateTo({ url: "/pages/address/list?mode=select" });
};

const submitOrder = async () => {
  if (!(await userStore.checkAuth(true))) return;
  if (deliveryType.value !== "self" && !currentAddress.value) {
    uni.showToast({ title: "请选择收货地址", icon: "none" });
    return;
  }

  const postData = {
    addressId: currentAddress.value?.id || 0,
    deliveryMethod: deliveryType.value,
    remark: remark.value,
    deviceId: getDeviceId(),
    orderType: orderType.value,
    groupBuyId: targetGroupBuyId.value || null,
    userCouponId: selectedCoupon.value ? selectedCoupon.value.id : 0,
  };

  uni.showLoading({ title: "正在下单..." });

  try {
    const res: any = await apiCreateOrder(postData);
    const orderId = res?.orderId;
    if (!orderId) throw new Error("下单失败");

    await payOrder({
      orderId,
      orderNo: orderId,
      onCancel: () => redirectToDetail(orderId),
      onPaid: async () => {
        await new Promise((resolve) => setTimeout(resolve, 1200));
        redirectToDetail(orderId);
      },
    });
  } catch (e: any) {
    uni.showToast({ title: e?.message || "下单失败", icon: "none" });
  } finally {
    uni.hideLoading();
  }
};
// 统一跳转函数
const redirectToDetail = (orderId: number) => {
  uni.redirectTo({ url: `/pages/order/detail?id=${orderId}` });
};

const getImageUrl = (path: string | undefined) => {
  if (!path) return "/static/logo.png";
  if (path.startsWith("http") || path.startsWith("/static")) return path;
  return API_BASE_URL + path;
};
</script>
<style scoped lang="scss">
.page-container {
  padding: 20rpx;
  background: #f5f7fa;
  min-height: 100vh;
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

.notice-card,
.address-card,
.goods-card,
.option-card,
.cost-card {
  background: #fff;
  border-radius: 16rpx;
  padding: 30rpx;
  margin-bottom: 20rpx;
}

.notice-card {
  background: #fffbe6;
  .notice-title {
    font-weight: bold;
    font-size: 28rpx;
    color: #fa8c16;
    margin-bottom: 10rpx;
  }
  .notice-item {
    font-size: 24rpx;
    color: #666;
    line-height: 1.5;
  }
}

.address-card {
  display: flex;
  align-items: center;
  min-height: 100rpx;
  .has-address,
  .no-address {
    display: flex;
    align-items: center;
    width: 100%;
  }
  .addr-icon {
    font-size: 40rpx;
    margin-right: 20rpx;
  }
  .addr-info {
    flex: 1;
    .row1 {
      font-weight: bold;
      font-size: 30rpx;
      margin-bottom: 8rpx;
    }
    .detail {
      font-size: 32rpx;
      font-weight: bold;
      color: #333;
      margin-bottom: 8rpx;
      line-height: 1.4;
    }
    .user {
      font-size: 26rpx;
      color: #666;
    }
  }
  .no-address {
    font-size: 32rpx;
    font-weight: bold;
    justify-content: space-between;
  }
}

.goods-item {
  display: flex;
  margin-bottom: 20rpx;
  &:last-child {
    margin-bottom: 0;
  }
  .thumb {
    width: 120rpx;
    height: 120rpx;
    border-radius: 8rpx;
    background: #f8f8f8;
    margin-right: 20rpx;
  }
  .info {
    flex: 1;
    display: flex;
    flex-direction: column;
    justify-content: space-between;
    .name {
      font-size: 28rpx;
      font-weight: bold;
    }
    .spec {
      font-size: 22rpx;
      color: #999;
    }
    .price-row {
      display: flex;
      justify-content: space-between;
      .price {
        font-weight: bold;
        font-size: 30rpx;
      }
      .num {
        color: #999;
        font-size: 26rpx;
      }
    }
  }
}

.option-card {
  .row-item {
    margin-bottom: 30rpx;
    .label {
      font-weight: bold;
      font-size: 28rpx;
      margin-bottom: 20rpx;
      display: block;
    }
    .radio-group {
      display: flex;
      gap: 20rpx;
      .radio-item {
        border: 1rpx solid #eee;
        padding: 10rpx 30rpx;
        border-radius: 30rpx;
        font-size: 26rpx;
        color: #666;
        &.active {
          border-color: #000;
          background: #000;
          color: #fff;
        }
      }
    }
    &.input-row {
      margin-bottom: 0;
      display: flex;
      align-items: center;
      justify-content: space-between;
      .label {
        margin-bottom: 0;
      }
      .input {
        text-align: right;
        font-size: 26rpx;
        flex: 1;
        margin-left: 20rpx;
      }
    }
  }
}

.cost-card {
  .cost-row {
    display: flex;
    justify-content: space-between;
    margin-bottom: 20rpx;
    font-size: 28rpx;
    color: #333;
    .red {
      color: #ff4d4f;
    }
    .right-box {
      display: flex;
      align-items: center;
      .discount-num {
        margin-right: 8rpx;
        font-weight: bold;
      }
      .gray-text {
        font-size: 26rpx;
        color: #999;
        margin-right: 8rpx;
      }
    }
  }
  .total-row {
    display: flex;
    justify-content: space-between;
    margin-top: 30rpx;
    border-top: 1rpx solid #eee;
    padding-top: 30rpx;
    font-weight: bold;
    .big-price {
      font-size: 36rpx;
      color: #ff4d4f;
    }
  }
}

.bottom-bar {
  position: fixed;
  bottom: 0;
  left: 0;
  right: 0;
  background: #fff;
  padding: 20rpx 30rpx;
  padding-bottom: calc(20rpx + env(safe-area-inset-bottom));
  display: flex;
  justify-content: space-between;
  align-items: center;
  box-shadow: 0 -2rpx 10rpx rgba(0, 0, 0, 0.05);
  z-index: 99;
  .left {
    font-size: 26rpx;
    color: #666;
    .price {
      font-size: 40rpx;
      font-weight: bold;
      color: #ff4d4f;
      margin: 0 10rpx;
    }
    .discount {
      font-size: 22rpx;
      color: #ff4d4f;
    }
  }
  .submit-btn {
    background: #000;
    color: #fff;
    padding: 0 60rpx;
    height: 80rpx;
    line-height: 80rpx;
    border-radius: 40rpx;
    font-weight: bold;
    font-size: 30rpx;
  }
}
/* 🔥🔥 底部固定栏样式 (修复重叠问题) 🔥🔥 */
.footer-fixed {
  display: flex;
  flex-direction: column; /* 关键：强制垂直排列 */
  position: fixed;
  bottom: 0;
  left: 0;
  right: 0;
  z-index: 100;
  background: #fff;
  box-shadow: 0 -4rpx 16rpx rgba(0, 0, 0, 0.08);

  /* 适配刘海屏底部安全区 */
  padding-bottom: constant(safe-area-inset-bottom);
  padding-bottom: env(safe-area-inset-bottom);
}

/* 1. 购买模式切换 Tabs */
.buy-type-tabs {
  display: flex;
  height: 90rpx; /* 稍微调小一点高度，显得紧凑 */
  border-bottom: 1rpx solid #f5f5f5;
  background: #fff;

  .tab-item {
    flex: 1;
    display: flex;
    flex-direction: column;
    justify-content: center;
    align-items: center;
    transition: all 0.3s;
    position: relative;

    .row-1 {
      font-size: 24rpx;
      color: #666;
      margin-bottom: 2rpx;
      display: flex;
      align-items: center;
      .tag {
        background: #ff4d4f;
        color: #fff;
        font-size: 18rpx;
        padding: 0 6rpx;
        border-radius: 4rpx;
        margin-left: 6rpx;
      }
    }
    .row-2 {
      font-size: 28rpx;
      font-weight: bold;
      color: #333;
    }

    /* 选中态 */
    &.active {
      background: #fffafa; /* 极淡的红色背景 */
      .row-1 {
        color: #ff4d4f;
      }
      .row-2 {
        color: #ff4d4f;
      }

      /* 底部红色指示条 */
      &::after {
        content: "";
        position: absolute;
        bottom: 0;
        left: 50%;
        transform: translateX(-50%);
        width: 60rpx;
        height: 4rpx;
        background: #ff4d4f;
        border-radius: 4rpx;
      }
    }

    &.disabled {
      opacity: 0.5;
      background: #eee;
    }
  }
}

/* 2. 底部结算条 */
.bottom-bar {
  position: relative; /* 关键：取消 fixed，跟随父容器排列 */
  display: flex;
  justify-content: space-between;
  align-items: center;
  padding: 10rpx 30rpx; /* 减小内边距 */
  height: 100rpx;
  background: #fff;

  .left {
    display: flex;
    align-items: baseline;
    .label {
      font-size: 26rpx;
      color: #333;
    }
    .price {
      font-size: 44rpx;
      color: #ff4d4f;
      font-weight: bold;
      margin: 0 12rpx 0 6rpx;
      font-family: Arial;
    }
    .discount {
      font-size: 20rpx;
      color: #ff4d4f;
      background: #ffe4e4;
      padding: 2rpx 8rpx;
      border-radius: 8rpx;
    }
  }

  .submit-btn {
    background: #333;
    color: #fff;
    width: 220rpx;
    height: 72rpx;
    line-height: 72rpx;
    text-align: center;
    border-radius: 36rpx;
    font-size: 28rpx;
    font-weight: bold;

    &.group-btn {
      background: linear-gradient(90deg, #ff4d4f 0%, #d9363e 100%);
      box-shadow: 0 4rpx 10rpx rgba(217, 54, 62, 0.3);
    }
  }
}

.popup-box {
  background: #f5f7fa;
  border-radius: 20rpx 20rpx 0 0;
  padding-bottom: env(safe-area-inset-bottom);
  .popup-header {
    text-align: center;
    font-size: 32rpx;
    font-weight: bold;
    padding: 30rpx 0;
    background: #fff;
  }
  .coupon-list {
    max-height: 60vh;
    padding: 20rpx;
    box-sizing: border-box;
  }
}

.coupon-item {
  display: flex;
  background: #fff;
  border-radius: 12rpx;
  margin-bottom: 20rpx;
  overflow: hidden;
  position: relative;

  .left {
    width: 200rpx;
    background: #ffe4e4;
    color: #ff4d4f;
    display: flex;
    flex-direction: column;
    justify-content: center;
    align-items: center;
    padding: 20rpx;
    .price {
      font-size: 40rpx;
      font-weight: bold;
    }
    .condition {
      font-size: 22rpx;
      margin-top: 10rpx;
    }
  }
  .right {
    flex: 1;
    padding: 20rpx;
    display: flex;
    flex-direction: column;
    justify-content: center;
    .name {
      font-size: 28rpx;
      font-weight: bold;
      color: #333;
      margin-bottom: 10rpx;
    }
    .time {
      font-size: 22rpx;
      color: #999;
    }
  }
  .checked {
    position: absolute;
    right: 20rpx;
    top: 50%;
    transform: translateY(-50%);
    color: #ff4d4f;
    font-size: 36rpx;
    font-weight: bold;
  }
  .btn {
    align-self: center;
    margin-right: 20rpx;
    background: #ff4d4f;
    color: #fff;
    padding: 10rpx 30rpx;
    border-radius: 30rpx;
    font-size: 24rpx;
  }
}

.no-use-btn {
  text-align: center;
  padding: 20rpx;
  font-size: 28rpx;
  color: #666;
  background: #fff;
  border-radius: 12rpx;
  margin-top: 30rpx;
}
</style>

