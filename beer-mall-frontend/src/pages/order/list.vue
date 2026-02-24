<template>
  <view class="page-container">

    <view class="tabs">
      <view class="tab-item"
            v-for="(tab, index) in tabs"
            :key="index"
            :class="{ active: currentTab === index }"
            @click="switchTab(index)">
        {{ tab }}
        <view class="line"
              v-if="currentTab === index"></view>
      </view>
    </view>

    <scroll-view scroll-y
                 class="order-list"
                 @scrolltolower="loadMore">

      <view class="order-card"
            v-for="order in orderList"
            :key="order.id"
            @click="goToDetail(order.id)">
        <view class="card-header">
          <text class="no">订单号：{{ order.orderNo }}</text>
          <text class="status">{{ getStatusText(order.status) }}</text>
        </view>

        <view class="card-body">
          <view class="single-goods"
                v-if="order.items.length === 1">
            <image :src="getImageUrl(order.items[0].productImage)"
                   mode="aspectFill"
                   class="thumb"></image>
            <view class="info">
              <view class="name">{{ order.items[0].productName }}</view>
              <view class="spec">{{ order.items[0].specName }}</view>
            </view>
            <view class="qty">x{{ order.items[0].quantity }}</view>
          </view>

          <scroll-view scroll-x
                       class="multi-goods"
                       v-else>
            <image v-for="(item, idx) in order.items"
                   :key="idx"
                   :src="getImageUrl(item.productImage)"
                   mode="aspectFill"
                   class="thumb"></image>
          </scroll-view>
        </view>

        <view class="card-footer">
          <view class="total">
            共{{ getTotalCount(order.items) }}件商品，实付 <text class="price">¥{{ order.totalAmount }}</text>
          </view>

          <view class="actions"
                @click.stop>
            <view class="btn outline"
                  v-if="order.status === 0"
                  @click="cancelOrder(order)">取消订单</view>
            <view class="btn primary"
                  v-if="order.status === 0"
                  @click="payOrder(order)">去支付</view>
            <view class="btn outline"
                  v-if="order.status === 2">查看物流</view>
            <view class="btn primary"
                  v-if="order.status === 2">确认收货</view>
          </view>
        </view>
      </view>

      <view class="empty"
            v-if="!loading && orderList.length === 0">
        <text>暂无相关订单</text>
      </view>

    </scroll-view>
  </view>
</template>

<script setup lang="ts">
import { ref } from "vue";
import { onLoad, onShow } from "@dcloudio/uni-app";
import { useUserStore } from "@/store/user";

const userStore = useUserStore();
const baseUrl = "https://localhost:7252";

const tabs = ["全部", "待付款", "待发货", "待收货"];
const currentTab = ref(0);
const orderList = ref<any[]>([]);
const loading = ref(false);

onLoad((options) => {
  // 接收上个页面传来的 status 参数
  if (options && options.status) {
    currentTab.value = Number(options.status);
  }
});

onShow(() => {
  if (userStore.checkAuth()) {
    loadData();
  }
});

const switchTab = (index: number) => {
  currentTab.value = index;
  loadData();
};

const loadData = () => {
  loading.value = true;
  orderList.value = []; // 先清空，防止闪烁

  uni.request({
    // 这里的 currentTab 正好对应后端接口的 type 参数
    url: `${baseUrl}/api/order/list?userId=${userStore.userInfo.id}&type=${currentTab.value}`,
    success: (res: any) => {
      if (res.statusCode === 200) {
        orderList.value = res.data;
      }
    },
    complete: () => {
      loading.value = false;
    },
  });
};

// 跳转详情
const goToDetail = (id: number) => {
  uni.navigateTo({ url: `/pages/order/detail?id=${id}` });
};

// 支付 (复用之前的逻辑)
const payOrder = (order: any) => {
  // 这里简写，实际应该调用之前的 handlePay 逻辑或者跳去详情页支付
  uni.navigateTo({ url: `/pages/order/detail?id=${order.id}` });
};

const cancelOrder = (order: any) => {
  uni.showModal({
    content: "确认取消？",
    success: (res) => {
      if (res.confirm) {
        // 调用取消接口...
        uni.showToast({ title: "已取消" });
        loadData(); // 刷新列表
      }
    },
  });
};

// 辅助函数
const getStatusText = (status: number) => {
  const map: Record<number, string> = {
    0: "待付款",
    1: "待发货",
    2: "待收货",
    3: "已完成",
    "-1": "已取消",
    10: "等待成团",
  };
  return map[status] || "未知状态";
};
const getTotalCount = (items: any[]) =>
  items.reduce((sum, i) => sum + i.quantity, 0);

// 工具函数
const getImageUrl = (path: string | undefined) => {
  if (!path) return "/static/logo.png";
  if (path.startsWith("http") || path.startsWith("/static")) return path;
  return baseUrl + path;
};

const loadMore = () => {
  // 暂不实现分页，后续可扩展
  console.log("触底加载更多");
};
</script>

<style scoped lang="scss">
.page-container {
  background: #f5f7fa;
  height: 100vh;
  display: flex;
  flex-direction: column;
}

/* Tabs 样式 */
.tabs {
  background: #fff;
  display: flex;
  justify-content: space-around;
  padding: 20rpx 0;
  .tab-item {
    font-size: 28rpx;
    color: #666;
    position: relative;
    padding-bottom: 10rpx;
    &.active {
      color: #000;
      font-weight: bold;
      font-size: 30rpx;
    }
    .line {
      position: absolute;
      bottom: 0;
      left: 50%;
      transform: translateX(-50%);
      width: 40rpx;
      height: 6rpx;
      background: #ff4d4f;
      border-radius: 3rpx;
    }
  }
}

/* 列表样式 */
.order-list {
  flex: 1;
  padding: 20rpx;
  box-sizing: border-box;
}

.order-card {
  background: #fff;
  border-radius: 16rpx;
  padding: 30rpx;
  margin-bottom: 20rpx;

  .card-header {
    display: flex;
    justify-content: space-between;
    border-bottom: 1rpx solid #f9f9f9;
    padding-bottom: 20rpx;
    margin-bottom: 20rpx;
    .no {
      color: #666;
      font-size: 26rpx;
    }
    .status {
      color: #ff4d4f;
      font-size: 26rpx;
    }
  }

  .card-body {
    .single-goods {
      display: flex;
      .thumb {
        width: 140rpx;
        height: 140rpx;
        border-radius: 8rpx;
        margin-right: 20rpx;
        background: #f8f8f8;
      }
      .info {
        flex: 1;
        .name {
          font-size: 28rpx;
          margin-bottom: 10rpx;
        }
        .spec {
          font-size: 24rpx;
          color: #999;
        }
      }
      .qty {
        color: #999;
        font-size: 26rpx;
      }
    }
    .multi-goods {
      white-space: nowrap;
      .thumb {
        width: 140rpx;
        height: 140rpx;
        border-radius: 8rpx;
        margin-right: 20rpx;
        display: inline-block;
        background: #f8f8f8;
      }
    }
  }

  .card-footer {
    margin-top: 20rpx;
    text-align: right;
    .total {
      font-size: 26rpx;
      color: #333;
      margin-bottom: 20rpx;
      .price {
        font-size: 32rpx;
        font-weight: bold;
      }
    }

    .actions {
      display: flex;
      justify-content: flex-end;
      gap: 20rpx;
      .btn {
        padding: 0 30rpx;
        height: 56rpx;
        line-height: 56rpx;
        border-radius: 28rpx;
        font-size: 24rpx;
        &.outline {
          border: 1rpx solid #ccc;
          color: #666;
        }
        &.primary {
          border: 1rpx solid #ff4d4f;
          color: #ff4d4f;
          background: #fff;
        }
      }
    }
  }
}

.empty {
  text-align: center;
  color: #999;
  margin-top: 200rpx;
  font-size: 28rpx;
}
</style>