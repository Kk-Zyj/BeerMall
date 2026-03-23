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
  name: "",
  mobile: "",
  province: "",
  city: "",
  district: "",
  detail: "",
  isDefault: false,
});

let cachedPcaData: any = null;

const handleParse = async () => {
  if (!pasteText.value) return;

  uni.showLoading({ title: "识别中..." });

  try {
    if (!cachedPcaData) {
      cachedPcaData = await request<any>("/pca-code.json");
    }

    const result = smartParse(pasteText.value, cachedPcaData);

    if (result.name) form.value.name = result.name;
    if (result.mobile) form.value.mobile = result.mobile;
    if (result.provinceName) form.value.province = result.provinceName;
    if (result.cityName) form.value.city = result.cityName;
    if (result.areaName) form.value.district = result.areaName;
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
  if (!(await userStore.checkAuth(false))) return;

  await apiAddressCreate(form.value);
  uni.showToast({ title: "保存成功" });
  setTimeout(() => uni.navigateBack(), 800);
};

const handleDelete = async () => {
  if (!(await userStore.checkAuth(false))) return;

  await apiAddressDelete(form.value.id);
  uni.showToast({ title: "已删除", icon: "none" });
  setTimeout(() => uni.navigateBack(), 800);
};

const onSwitchChange = (e: any) => {
  form.value.isDefault = e.detail.value;
};

onLoad(async (options) => {
  if (!(await userStore.checkAuth(false))) return;

  if (options && options.id && options.id != "0") {
    apiAddressDetail(Number(options.id)).then((res) => {
      form.value = {
        id: res.id || 0,
        name: res.name || "",
        mobile: res.mobile || "",
        province: res.province || "",
        city: res.city || "",
        district: res.district || "",
        detail: res.detail || "",
        isDefault: !!res.isDefault,
      };
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
      min-height: 120rpx;
    }
    .input-group {
      flex: 1;
      display: flex;
      gap: 12rpx;
      .input-sm {
        flex: 1;
        font-size: 28rpx;
      }
    }
    .region-text {
      flex: 1;
      font-size: 28rpx;
      color: #333;
    }
  }
  .switch-item {
    justify-content: space-between;
  }
}

.footer-btn {
  margin-top: 40rpx;
  display: flex;
  flex-direction: column;
  gap: 20rpx;
  .save-btn {
    background: #000;
    color: #fff;
    border-radius: 44rpx;
  }
  .del-btn {
    background: #fff;
    color: #e34d59;
    border-radius: 44rpx;
    border: 1rpx solid #f0c2c7;
  }
}
</style>