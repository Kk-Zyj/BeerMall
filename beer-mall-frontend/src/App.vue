<script setup lang="ts">
import { onLaunch, onShow, onHide } from "@dcloudio/uni-app";
import { useAuthStore } from "@/store/auth";

const userStore = useAuthStore();

onLaunch(() => {
  console.log("App Launch");
  // 启动即登录，建立影子账号
  userStore.silentLogin();
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
