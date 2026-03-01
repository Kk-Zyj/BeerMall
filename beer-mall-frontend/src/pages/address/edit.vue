<template>
  <view class="page-container">

    <view class="smart-paste-box">
      <textarea v-model="pasteText"
                placeholder="粘贴如：张三 13800000000 北京市朝阳区三里屯路88号"
                class="paste-area"
                auto-height />
      <view class="parse-btn"
            @click="handleParse">自动识别</view>
    </view>

    <view class="form-box">
      <view class="form-item">
        <text class="label">所在地区</text>
        <view class="region-text">
          {{ form.province }} {{ form.city }} {{ form.district }}
        </view>
      </view>
    </view>

    <view class="form-box">
      <view class="form-item">
        <text class="label">联系人</text>
        <input class="input"
               v-model="form.name"
               placeholder="名字" />
      </view>
      <view class="form-item">
        <text class="label">手机号</text>
        <input class="input"
               v-model="form.mobile"
               type="number"
               placeholder="11位手机号" />
      </view>

      <view class="form-item">
        <text class="label">所在地区</text>
        <view class="input-group">
          <input class="input-sm"
                 v-model="form.province"
                 placeholder="省" />
          <input class="input-sm"
                 v-model="form.city"
                 placeholder="市" />
          <input class="input-sm"
                 v-model="form.district"
                 placeholder="区/县" />
        </view>
      </view>

      <view class="form-item">
        <text class="label">详细地址</text>
        <textarea class="input-area"
                  v-model="form.detail"
                  placeholder="街道门牌信息"
                  auto-height />
      </view>

      <view class="form-item switch-item">
        <text class="label">设为默认地址</text>
        <switch :checked="form.isDefault"
                color="#000"
                @change="onSwitchChange" />
      </view>
    </view>

    <view class="footer-btn">
      <button class="save-btn"
              @click="submit">保存地址</button>
      <button class="del-btn"
              v-if="form.id > 0"
              @click="handleDelete">删除</button>
    </view>
  </view>
</template>

<script setup lang="ts">
import { ref } from "vue";
import { onLoad } from "@dcloudio/uni-app";
import { smartParse } from "@/utils/addressParse";
import { useAuthStore } from "@/store/auth";
import { request } from "@/api/request";
import {
  apiAddressCreate,
  apiAddressDelete,
  apiAddressDetail,
} from "@/api/address";
const userStore = useAuthStore();

const pasteText = ref("");
const form = ref({
  id: 0,
  userId: userStore.userInfo.id, // 实际应从 UserStore 获取
  name: "",
  mobile: "",
  province: "",
  city: "",
  district: "",
  detail: "",
  isDefault: false,
});

// 定义一个全局变量缓存数据，防止每次点击都去请求
let cachedPcaData: any = null;

const handleParse = async () => {
  if (!pasteText.value) return;

  uni.showLoading({ title: "识别中..." });

  try {
    // 🔥 1. 获取数据 (如果缓存没数据，就去后端拉)
    if (!cachedPcaData) {
      // 注意：这里 url 是你后端的静态资源地址
      cachedPcaData = await request<any>("/pca-code.json"); // 存入缓存
    }

    // 🔥 2. 开始识别 (传入下载好的数据)
    const result = smartParse(pasteText.value, cachedPcaData);
    // 回填表单
    if (result.name) form.value.name = result.name;
    if (result.mobile) form.value.mobile = result.mobile;

    // 地区回填
    if (result.provinceName) form.value.province = result.provinceName;
    if (result.cityName) form.value.city = result.cityName;
    if (result.areaName) form.value.district = result.areaName;

    // 详细地址回填
    if (result.detail) form.value.detail = result.detail;

    uni.showToast({ title: "识别完成", icon: "success" });
  } catch (e) {
    console.error(e);
    uni.showToast({ title: "识别出错", icon: "none" });
  } finally {
    uni.hideLoading();
  }
};

const submit = async () => {
  await apiAddressCreate(form.value);
  uni.showToast({ title: "保存成功" });
  setTimeout(() => uni.navigateBack(), 800);
};

const handleDelete = async () => {
  await apiAddressDelete(form.value.id);
  uni.showToast({ title: "已删除", icon: "none" });
  setTimeout(() => uni.navigateBack(), 800);
};

const onSwitchChange = (e: any) => {
  form.value.isDefault = e.detail.value;
};

// 加载已有数据
onLoad((options) => {
  if (options && options.id && options.id != "0") {
    apiAddressDetail(Number(options.id)).then((res) => {
      form.value = res;
    });
  }
});
</script>

<style scoped lang="scss">
.page-container {
  padding: 20rpx;
  background: #f5f7fa;
  min-height: 100vh;
}

.smart-paste-box {
  background: #fff;
  padding: 20rpx;
  border-radius: 16rpx;
  margin-bottom: 20rpx;
  position: relative;
  .paste-area {
    width: 100%;
    font-size: 26rpx;
    min-height: 100rpx;
    margin-bottom: 20rpx;
  }
  .parse-btn {
    position: absolute;
    right: 20rpx;
    bottom: 20rpx;
    background: #000;
    color: #fff;
    font-size: 24rpx;
    padding: 10rpx 20rpx;
    border-radius: 30rpx;
    z-index: 9;
  }
}

.form-box {
  background: #fff;
  border-radius: 16rpx;
  padding: 0 30rpx;
  .form-item {
    display: flex;
    align-items: center;
    border-bottom: 1rpx solid #f5f5f5;
    padding: 30rpx 0;
    .label {
      width: 160rpx;
      font-weight: bold;
      font-size: 28rpx;
    }
    .input {
      flex: 1;
      font-size: 28rpx;
    }
    .input-area {
      flex: 1;
      font-size: 28rpx;
      min-height: 80rpx;
    }

    .input-group {
      flex: 1;
      display: flex;
      gap: 10rpx;
      .input-sm {
        flex: 1;
        background: #f9f9f9;
        padding: 10rpx;
        border-radius: 8rpx;
        font-size: 24rpx;
        text-align: center;
      }
    }

    &.switch-item {
      justify-content: space-between;
    }
    &:last-child {
      border-bottom: none;
    }
  }
}

.footer-btn {
  margin-top: 60rpx;
  .save-btn {
    background: #000;
    color: #fff;
    border-radius: 44rpx;
    margin-bottom: 20rpx;
  }
  .del-btn {
    background: #fff;
    color: #ff4d4f;
    border-radius: 44rpx;
  }
}
</style>
