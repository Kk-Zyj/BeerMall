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
import { onShow, onLoad } from "@dcloudio/uni-app";
import { apiAddressList } from "@/api/address";
import { useAuthStore } from "@/store/auth";

const userStore = useAuthStore();
const list = ref<any[]>([]);
const isSelectionMode = ref(false);

onShow(async () => {
  if (!(await userStore.checkAuth(false))) return;
  loadList();
});

const loadList = async () => {
  try {
    const res = await apiAddressList();
    list.value = res || [];
  } catch (e) {
    console.error("加载地址失败", e);
  }
};

const goToEdit = (id: number) => {
  uni.navigateTo({ url: `/pages/address/edit?id=${id}` });
};

onLoad((options) => {
  if (options && options.mode === "select") {
    isSelectionMode.value = true;
  }
});

const selectAddress = (item: any) => {
  if (isSelectionMode.value) {
    uni.$emit("selectAddress", item);
    uni.navigateBack();
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