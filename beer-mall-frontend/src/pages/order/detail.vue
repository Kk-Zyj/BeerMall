<template>
  <view class="page-container"
        v-if="order">

    <view class="status-header">
      <view class="status-text">{{ statusText }}</view>

      <view class="status-desc"
            v-if="order.status === 0">
        <text v-if="orderCountdown > 0">剩余 {{ orderCountdownStr }} 自动关闭</text>
        <text v-else>订单已超时关闭</text>
      </view>
      <view class="status-desc"
            v-if="order.status === 10">
        <text>已付款，等待成团中...</text>
      </view>
    </view>
    <view class="fission-card"
          v-if="task">
      <template v-if="task.status === 0">
        <view class="card-top">
          <view class="title">🔥 免单挑战进行中</view>
          <view class="countdown"
                v-if="taskTimeLeft > 0">剩余 {{ taskTimeStr }}</view>
        </view>

        <view class="desc">
          再邀 <text class="highlight">{{ 3 - task.currentCount }}</text> 位好友下单
          (满¥{{ task.targetThreshold }})，本单全额免单！
        </view>

        <progress :percent="(task.currentCount/3)*100"
                  activeColor="#ff4d4f"
                  stroke-width="8"
                  border-radius="8" />
        <view class="progress-tip">当前进度: {{ task.currentCount }}/3</view>

        <button class="action-btn share-btn"
                open-type="share">立即邀请好友助力</button>
      </template>

      <template v-if="task.status === 1">
        <view class="title success">🎉 挑战成功！</view>
        <view class="desc">您已获得免单资格，请添加店主领取返现</view>
        <button class="action-btn success-btn"
                @click="openQrPopup">添加店主微信领钱</button>
      </template>

      <template v-if="task.status === -1 || (task.status === 0 && taskTimeLeft <= 0)">
        <view class="title fail">💔 任务已过期</view>
        <view class="desc">很遗憾，未在3天内完成挑战</view>
        <button class="action-btn disabled">已结束</button>
      </template>

      <template v-if="task.status === 2">
        <view class="title success">✅ 已返现</view>
        <view class="desc">返现金额 ¥{{ task.sourceOrderAmount }} 已到账</view>
      </template>
    </view>
    <view class="group-share-card"
          v-if="order.orderType !== 0 && groupBuy">

      <template v-if="groupBuy.status === 0">
        <view class="group-header">
          <view class="tag">拼团中</view>
          <view class="info">还差 <text class="red">{{ groupBuy.targetCount - groupBuy.currentCount }}</text> 人成团</view>
        </view>

        <view class="group-desc">
          邀请好友参与，全员享受 85 折优惠
          <br />24小时内未成团自动退款
        </view>

        <button class="btn-group"
                open-type="share"
                data-type="group">
          邀请好友85折拼单
        </button>
      </template>

      <template v-if="groupBuy.status === 1">
        <view class="group-header">
          <view class="tag success">拼团成功</view>
          <view class="info">恭喜！商家正在安排发货</view>
        </view>
      </template>

      <template v-if="groupBuy.status === -1">
        <view class="group-header">
          <view class="tag fail">拼团失败</view>
          <view class="info">款项已原路退回</view>
        </view>
      </template>
    </view>

    <view class="custom-mask"
          v-if="showQrModal"
          @click="closeQrPopup"
          @touchmove.stop.prevent>
      <view class="qr-box"
            @click.stop>
        <view class="qr-title">扫码加店主</view>
        <image src="/static/owner_wx.jpg"
               class="qr-img"
               show-menu-by-longpress />
        <view class="qr-tip">
          请发送口令：<text class="code">免单{{ order.orderNo }}</text>
        </view>
        <view class="close-btn"
              @click="closeQrPopup">关闭</view>
      </view>
    </view>

    <view class="card address-card">
      <view class="icon-box">📍</view>
      <view class="info">
        <view class="user">
          <text class="name">{{ order.receiverName }}</text>
          <text class="mobile">{{ order.receiverMobile }}</text>
        </view>
        <view class="addr">{{ order.receiverAddress }}</view>
      </view>
    </view>

    <view class="card goods-card">
      <view class="goods-item"
            v-for="item in order.items"
            :key="item.id">
        <image :src="getImageUrl(item.productImage)"
               class="thumb"
               mode="aspectFill"></image>
        <view class="content">
          <view class="name">{{ item.productName }}</view>
          <view class="spec">{{ item.specName }}</view>
          <view class="price-row">
            <text>¥{{ item.price }}</text>
            <text class="num">x{{ item.quantity }}</text>
          </view>
        </view>
      </view>
    </view>

    <view class="card info-card">
      <view class="row">
        <text class="label">订单编号</text>
        <text class="val">{{ order.orderNo }}</text>
      </view>
      <view class="row">
        <text class="label">下单时间</text>
        <text class="val">{{ formatTime(order.createTime) }}</text>
      </view>
      <view class="row">
        <text class="label">支付方式</text>
        <text class="val">微信支付</text>
      </view>
      <view class="row">
        <text class="label">买家留言</text>
        <text class="val">{{ order.remark || '无' }}</text>
      </view>
    </view>

    <view class="card cost-card">
      <view class="row">
        <text>商品总额</text>
        <text>¥{{ order.productAmount }}</text>
      </view>
      <view class="row">
        <text>运费</text>
        <text>+ ¥{{ order.freightAmount }}</text>
      </view>
      <view class="row total-row">
        <text>实付款</text>
        <text class="total-price">¥{{ order.totalAmount }}</text>
      </view>
    </view>

    <view style="height: 120rpx;"></view>

    <view class="bottom-bar">
      <template v-if="order.status === 0 && countdown > 0">
        <view class="btn outline"
              @click="cancelOrder">取消订单</view>
        <view class="btn primary"
              @click="payOrder">立即支付</view>
      </template>

      <template v-else-if="order.status === -1 || countdown <= 0">
        <view class="btn outline"
              @click="cancelOrder">取消订单</view>
      </template>
    </view>

  </view>
</template>

<script setup lang="ts">
import { ref, computed, onUnmounted } from "vue";
import { onLoad } from "@dcloudio/uni-app";
import { useUserStore } from "@/store/user";
import { onShareAppMessage } from "@dcloudio/uni-app";
import uniPopup from "@dcloudio/uni-ui/lib/uni-popup/uni-popup.vue";

const task = ref<any>(null); // 裂变数据
const groupBuy = ref<any>(null); // 拼团实例数据

// 1. 订单支付倒计时 (Header用)
const orderCountdown = ref(0); // 剩余秒数
const orderCountdownStr = ref(""); // 格式化字符串 (mm:ss)
let orderTimer: any = null;

// 2. 裂变/拼团任务倒计时 (Card用)
const taskTimeLeft = ref(0); // 剩余秒数
const taskTimeStr = ref(""); // 格式化字符串 (dd天hh小时)
let taskTimer: any = null;

const userStore = useUserStore();
const baseUrl = "https://localhost:7252";
const order = ref<any>(null);
const countdown = ref(0); // 剩余秒数
const countdownStr = ref(""); // 格式化后的字符串 (mm:ss)
let timer: any = null;

// 定义一个开关变量
const showQrModal = ref(false);

// 打开弹窗
const openQrPopup = () => {
  showQrModal.value = true;
};

// 关闭弹窗
const closeQrPopup = () => {
  showQrModal.value = false;
};

// 状态字典
const statusMap: Record<number, string> = {
  0: "等待买家付款",
  1: "等待卖家发货",
  2: "卖家已发货",
  3: "交易成功",
  "-1": "交易已取消",
  10: "等待成团",
};

const statusText = computed(() => {
  return order.value ? statusMap[order.value.status] : "";
});

onLoad((options) => {
  if (options && options.id) {
    loadOrderDetail(options.id);
  }
});

const loadOrderDetail = (id: string) => {
  uni.request({
    url: `${baseUrl}/api/order/${id}?userId=${userStore.userInfo.id}`,
    success: (res: any) => {
      if (res.statusCode === 200) {
        order.value = res.data.order || res.data;
        task.value = res.data.task || null;
        groupBuy.value = res.data.groupBuy || null;

        // 启动【订单支付】倒计时 (30分钟)
        if (order.value.status === 0) {
          startOrderTimer(order.value.createTime);
        }

        // 启动【任务/拼团】倒计时 (按过期时间)
        // 优先判断拼团，再判断裂变
        if (groupBuy.value && groupBuy.value.status === 0) {
          startTaskTimer(groupBuy.value.expireTime);
        } else if (task.value && task.value.status === 0) {
          startTaskTimer(task.value.expireTimeStr || task.value.expireTime);
        }
      }
    },
  });
};

// --- 定时器逻辑 A：订单支付 (30分钟限制) ---
const startOrderTimer = (createTimeStr: string) => {
  // 假设订单有效期是 30 分钟 (1800秒)
  // 如果后端有返回 expireTime 最好，没有则用 createTime + 30m 计算
  const createTime = new Date(createTimeStr).getTime();
  const expireTime = createTime + 30 * 60 * 1000;

  const tick = () => {
    const now = new Date().getTime();
    const diff = Math.floor((expireTime - now) / 1000);

    if (diff <= 0) {
      orderCountdown.value = 0;
      // 前端显示超时，实际上应该刷新页面或重调接口确认状态
      clearInterval(orderTimer);
    } else {
      orderCountdown.value = diff;
      const m = Math.floor(diff / 60);
      const s = diff % 60;
      // 补零格式化
      orderCountdownStr.value = `${m.toString().padStart(2, "0")}:${s
        .toString()
        .padStart(2, "0")}`;
    }
  };

  tick(); // 立即执行一次
  if (orderTimer) clearInterval(orderTimer);
  orderTimer = setInterval(tick, 1000);
};

// --- 定时器逻辑 B：任务/拼团 (长时效) ---
const startTaskTimer = (expireTimeStr: string) => {
  const end = new Date(expireTimeStr).getTime();

  const tick = () => {
    const now = new Date().getTime();
    const diff = Math.floor((end - now) / 1000);

    if (diff <= 0) {
      taskTimeLeft.value = 0;
      if (task.value) task.value.status = -1;
      if (groupBuy.value) groupBuy.value.status = -1;
      clearInterval(taskTimer);
    } else {
      taskTimeLeft.value = diff;
      const d = Math.floor(diff / 86400);
      const h = Math.floor((diff % 86400) / 3600);
      const m = Math.floor((diff % 3600) / 60);

      // 格式化：天+小时 或 小时+分
      taskTimeStr.value = d > 0 ? `${d}天${h}小时` : `${h}小时${m}分`;
    }
  };

  tick();
  if (taskTimer) clearInterval(taskTimer);
  taskTimer = setInterval(tick, 60000); // 任务倒计时没必要每秒刷，每分钟即可
};

// 页面销毁时清除所有定时器
onUnmounted(() => {
  if (orderTimer) clearInterval(orderTimer);
  if (taskTimer) clearInterval(taskTimer);
});

// 分享逻辑
onShareAppMessage((res) => {
  // 1. 初始化默认分享配置 (兜底，防止右上角菜单分享报错)
  let shareConfig = {
    title: "BeerMall 精酿啤酒集市",
    path: `/pages/index/index`,
    imageUrl: "/static/logo.png",
  };

  if (res.from === "button") {
    const type = res.target.dataset.type;
    const product = order.value?.items[0];

    // === 情况一：拼团分享 ===
    if (type === "group") {
      const groupId =
        order.value.groupBuyId || (groupBuy.value ? groupBuy.value.id : null);
      const goodsId = product.productId; // 如果是订单拼团，这里可以是任意商品ID或直接不传跳活动页

      // 算出还差几人
      const need = groupBuy.value
        ? groupBuy.value.targetCount - groupBuy.value.currentCount
        : 1;

      shareConfig = {
        title: `还差${need}人！快来85折拼${product.productName}！`,
        // 跳转商品详情页，带上 groupBuyId
        path: `/pages/category/category?id=${goodsId}&groupBuyId=${groupId}`,
        imageUrl: product.productImage,
      };
    }

    // === 情况二：裂变分享 ===
    else if (type === "fission") {
      const target = task.value ? task.value.targetThreshold : 95;
      shareConfig = {
        title: `帮我点一下！满${target}元咱俩一起喝精酿~`,
        path: `/pages/index/index?inviterId=${userStore.userInfo.id}`,
        imageUrl: product.productImage,
      };
    }
  }
  return shareConfig;
});

// 支付 (暂时只是弹窗，后续接微信支付)
const payOrder = () => {
  // 复用模拟支付逻辑
  uni.showModal({
    title: "支付确认",
    content: "确认支付当前订单？",
    success: (res) => {
      if (res.confirm) {
        uni.showLoading({ title: "支付中" });
        uni.request({
          url: `${baseUrl}/api/order/${order.value.id}/pay`, // 调用后端
          method: "POST",
          success: () => {
            uni.hideLoading();
            uni.showToast({ title: "支付成功" });
            // 🔥 支付成功后，重新加载当前页面详情，刷新状态
            loadOrderDetail(order.value.id);
          },
        });
      }
    },
  });
};

const cancelOrder = () => {
  let content = "确定要取消订单吗？";
  if (order.value.status === 1 || order.value.status === 10) {
    content = "当前订单已付款，取消后金额将原路退回。确定取消？";
  }
  uni.showModal({
    content: content,
    success: (res) => {
      if (res.confirm) {
        uni.request({
          url: `${baseUrl}/api/order/${order.value.id}/cancel`,
          method: "POST",
          success: (res: any) => {
            if (res.statusCode === 200) {
              uni.showToast({ title: "订单已取消" });
            } else {
              uni.showToast({ title: "取消失败，请重试" + res.data });
            }
          },
        });
      }
    },
  });
};

// 工具函数
const getImageUrl = (path: string | undefined) => {if (!path) return "/static/logo.png";
  if (path.startsWith("http") || path.startsWith("/static")) return path;
  return baseUrl + path;
};

const formatTime = (t: string) => {
  return t ? t.replace("T", " ").substring(0, 19) : "";
};

// 定义模板ID (需要在微信公众平台后台申请)
// 模板A: 任务完成通知
const TMPL_SUCCESS = "xxxx_success_template_id";
// 模板B: 进度变动通知 (用于回退)
const TMPL_UPDATE = "xxxx_update_template_id";

const requestSubscribe = () => {
  uni.requestSubscribeMessage({
    tmplIds: [TMPL_SUCCESS, TMPL_UPDATE],
    success(res) {
      console.log("授权成功", res);
      // 后端不需要存是否授权，微信发送时会自动判断
    },
    fail(err) {
      console.error("授权失败", err);
    },
  });
};
</script>

<style scoped lang="scss">
.page-container {
  background: #f5f7fa;
  min-height: 100vh;
  padding-bottom: constant(safe-area-inset-bottom);
}

.status-header {
  background: #ff4d4f;
  color: #fff;
  padding: 40rpx 30rpx;
  .status-text {
    font-size: 34rpx;
    font-weight: bold;
    margin-bottom: 10rpx;
  }
  .status-desc {
    font-size: 24rpx;
    opacity: 0.9;
  }
}

.card {
  background: #fff;
  margin: 20rpx;
  padding: 30rpx;
  border-radius: 16rpx;

  &.address-card {
    display: flex;
    align-items: center;
    .icon-box {
      font-size: 36rpx;
      margin-right: 20rpx;
    }
    .info {
      .user {
        font-weight: bold;
        font-size: 28rpx;
        margin-bottom: 10rpx;
        .name {
          margin-right: 20rpx;
        }
      }
      .addr {
        font-size: 26rpx;
        color: #666;
        line-height: 1.4;
      }
    }
  }

  &.goods-card {
    .goods-item {
      display: flex;
      margin-bottom: 20rpx;
      &:last-child {
        margin-bottom: 0;
      }
      .thumb {
        width: 140rpx;
        height: 140rpx;
        border-radius: 8rpx;
        margin-right: 20rpx;
        background: #f8f8f8;
      }
      .content {
        flex: 1;
        display: flex;
        flex-direction: column;
        justify-content: space-between;
        .name {
          font-size: 28rpx;
          line-height: 1.3;
        }
        .spec {
          font-size: 24rpx;
          color: #999;
        }
        .price-row {
          display: flex;
          justify-content: space-between;
          font-weight: bold;
        }
      }
    }
  }

  &.info-card,
  &.cost-card {
    .row {
      display: flex;
      justify-content: space-between;
      margin-bottom: 16rpx;
      font-size: 26rpx;
      color: #666;
      &:last-child {
        margin-bottom: 0;
      }
    }
    .total-row {
      border-top: 1rpx solid #eee;
      padding-top: 20rpx;
      margin-top: 20rpx;
      align-items: center;
      .total-price {
        font-size: 32rpx;
        color: #ff4d4f;
        font-weight: bold;
      }
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
  padding-bottom: calc(20rpx + constant(safe-area-inset-bottom));
  display: flex;
  justify-content: flex-end;
  gap: 20rpx;
  box-shadow: 0 -2rpx 10rpx rgba(0, 0, 0, 0.05);

  .btn {
    padding: 0 30rpx;
    height: 60rpx;
    line-height: 60rpx;
    border-radius: 30rpx;
    font-size: 26rpx;
    &.outline {
      border: 1rpx solid #ccc;
      color: #333;
    }
    &.primary {
      background: #ff4d4f;
      color: #fff;
      border: 1rpx solid #ff4d4f;
    }
  }
}

.fission-card {
  margin: 20rpx;
  background: #fff;
  padding: 30rpx;
  border-radius: 16rpx;
  box-shadow: 0 4rpx 16rpx rgba(255, 77, 79, 0.1);

  .title {
    font-size: 32rpx;
    font-weight: bold;
    margin-bottom: 10rpx;
    &.success {
      color: #07c160;
    }
  }
  .desc {
    font-size: 26rpx;
    color: #666;
    margin-bottom: 20rpx;
    .red {
      color: #ff4d4f;
      font-weight: bold;
    }
  }

  .action-btn {
    margin-top: 20rpx;
    background: #ff4d4f;
    color: #fff;
    border-radius: 40rpx;
    font-size: 28rpx;
    &.success-btn {
      background: #07c160;
    } /* 微信绿 */
    &::after {
      border: none;
    }
  }
}
/* 自定义遮罩层 */
.custom-mask {
  position: fixed;
  top: 0;
  left: 0;
  right: 0;
  bottom: 0;
  background: rgba(0, 0, 0, 0.6); /* 半透明黑色背景 */
  z-index: 9999; /* 保证在最上层 */
  display: flex;
  align-items: center; /* 垂直居中 */
  justify-content: center; /* 水平居中 */
}

/* 原来的 qr-box 样式保持不变，稍微优化一下 */
.qr-box {
  background: #fff;
  padding: 40rpx;
  border-radius: 20rpx;
  text-align: center;
  width: 500rpx;
  // 加上这个防止点击内容穿透
  position: relative;

  .qr-title {
    font-weight: bold;
    font-size: 32rpx;
    margin-bottom: 20rpx;
  }
  .qr-img {
    width: 400rpx;
    height: 400rpx;
    display: block;
    margin: 0 auto 20rpx;
  }
  .qr-tip {
    font-size: 26rpx;
    color: #666;
    .code {
      color: #ff4d4f;
      font-weight: bold;
      user-select: text;
    }
  }
  .close-btn {
    margin-top: 30rpx;
    color: #999;
    font-size: 28rpx;
    padding: 20rpx;
  }
}
// 补充动画
@keyframes pulse {
  0% {
    transform: scale(1);
  }
  50% {
    transform: scale(1.02);
  }
  100% {
    transform: scale(1);
  }
}
.group-share-card {
  margin: 20rpx;
  background: linear-gradient(135deg, #fff 0%, #fffbfb 100%);
  border: 2rpx solid #ffe4e4;
  padding: 30rpx;
  border-radius: 16rpx;
  box-shadow: 0 4rpx 16rpx rgba(255, 77, 79, 0.08);

  .group-header {
    display: flex;
    justify-content: space-between;
    align-items: center;
    margin-bottom: 20rpx;

    .tag {
      background: #ff4d4f;
      color: #fff;
      font-size: 24rpx;
      padding: 6rpx 16rpx;
      border-radius: 8rpx;
      font-weight: bold;
      &.success {
        background: #07c160;
      }
      &.fail {
        background: #999;
      }
    }
    .info {
      font-size: 28rpx;
      color: #333;
      font-weight: bold;
      .red {
        color: #ff4d4f;
        font-size: 32rpx;
        margin: 0 4rpx;
      }
    }
  }

  .group-desc {
    font-size: 26rpx;
    color: #666;
    line-height: 1.6;
    margin-bottom: 30rpx;
    background: #f9f9f9;
    padding: 20rpx;
    border-radius: 8rpx;
  }

  .btn-group {
    background: linear-gradient(90deg, #ff4d4f 0%, #d9363e 100%);
    color: #fff;
    border-radius: 40rpx;
    font-size: 30rpx;
    font-weight: bold;
    box-shadow: 0 6rpx 16rpx rgba(217, 54, 62, 0.3);
    &::after {
      border: none;
    }
  }
}

.bottom-bar {
  /* ... */
  .btn.primary {
    background: #ff4d4f;
    color: #fff;
    border: 1rpx solid #ff4d4f;
  }
}

.info-card .val.highlight {
  color: #ff4d4f;
  font-weight: bold;
}
</style>