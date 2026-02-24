<template>
  <view class="page-container">
    <view class="address-list">
      <view class="item"
            v-for="item in list"
            :key="item.id">
        <view class="info"
              @click="selectAddress(item)">
          <view class="row1">
            <text class="name">{{ item.name }}</text>
            <text class="mobile">{{ item.mobile }}</text>
            <text class="tag"
                  v-if="item.isDefault">默认</text>
          </view>
          <view class="row2">
            {{ item.province }}{{ item.city }}{{ item.district }} {{ item.detail }}
          </view>
        </view>
        <view class="edit-btn"
              @click.stop="goToEdit(item.id)">
          <text>编辑</text>
        </view>
      </view>
    </view>

    <view class="add-btn-wrap">
      <button class="add-btn"
              @click="goToEdit(0)">+ 新建收货地址</button>
    </view>
  </view>
</template>

<script setup lang="ts">
import { ref } from "vue";
import { onShow } from "@dcloudio/uni-app";
import { onLoad } from "@dcloudio/uni-app";
import { useUserStore } from "@/store/user";

const userStore = useUserStore();
const list = ref<any[]>([]);
const baseUrl = "https://localhost:7252";

// 每次进入页面刷新数据
onShow(() => {
  loadList();
});

const loadList = () => {
  // 假设 userId = 1
  console.info("Loading address list for user ID:", userStore.userInfo.id);
  uni.request({
    url: `${baseUrl}/api/address?userId=${userStore.userInfo.id}`,
    success: (res: any) => {
      list.value = res.data;
    },
  });
};

const goToEdit = (id: number) => {
  uni.navigateTo({ url: `/pages/address/edit?id=${id}` });
};

const isSelectionMode = ref(false); // 是否是选择模式

onLoad((options) => {
  // 检查 URL 参数，如果有 mode=select，说明是来选地址的
  if (options && options.mode === "select") {
    isSelectionMode.value = true;
  }
});

const selectAddress = (item: any) => {
  if (isSelectionMode.value) {
    // 1. 发送事件给上一页
    uni.$emit("selectAddress", item);
    // 2. 返回
    uni.navigateBack();
  } else {
    // 之前的逻辑：可能是什么都不做，或者去编辑
  }
};
</script>

<style scoped lang="scss">
.page-container {
  padding-bottom: 120rpx;
  background: #f5f7fa;
  min-height: 100vh;
}
.address-list {
  padding: 20rpx;
  .item {
    background: #fff;
    padding: 30rpx;
    margin-bottom: 20rpx;
    border-radius: 16rpx;
    display: flex;
    justify-content: space-between;
    align-items: center;
    .info {
      flex: 1;
      .row1 {
        margin-bottom: 10rpx;
        font-size: 30rpx;
        font-weight: bold;
        .mobile {
          margin-left: 20rpx;
          color: #666;
          font-weight: normal;
          font-size: 28rpx;
        }
        .tag {
          font-size: 20rpx;
          background: #000;
          color: #fff;
          padding: 2rpx 8rpx;
          border-radius: 4rpx;
          margin-left: 10rpx;
        }
      }
      .row2 {
        font-size: 26rpx;
        color: #666;
        line-height: 1.4;
      }
    }
    .edit-btn {
      padding-left: 30rpx;
      border-left: 1rpx solid #eee;
      color: #999;
      font-size: 26rpx;
    }
  }
}
.add-btn-wrap {
  position: fixed;
  bottom: 0;
  left: 0;
  right: 0;
  padding: 20rpx;
  background: #fff;
  .add-btn {
    background: #000;
    color: #fff;
    border-radius: 44rpx;
    font-size: 30rpx;
  }
}
</style>