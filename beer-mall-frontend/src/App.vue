<script setup lang="ts">
import { onLaunch, onShow, onHide } from "@dcloudio/uni-app";
import { useAuthStore } from "@/store/auth";

const userStore = useAuthStore();

onLaunch(async () => {
  console.log("App Launch");
  // 启动时先尝试从本地缓存恢复；没有缓存才走微信静默登录
  await userStore.initAuth();
});

onShow(() => {
  console.log("App Show");
});
onHide(() => {
  console.log("App Hide");
});
</script>
<style>
@import "uview-plus/index.scss";
</style>
