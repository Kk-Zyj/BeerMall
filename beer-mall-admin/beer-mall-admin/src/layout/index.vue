<template>
  <el-container class="layout-container">
    <el-aside width="200px"
              class="aside">
      <div class="logo">🍺 BeerMall Admin</div>
      <el-menu router
               :default-active="$route.path"
               background-color="#304156"
               text-color="#bfcbd9"
               active-text-color="#409EFF">
        <el-menu-item index="/product">
          <el-icon>
            <Goods />
          </el-icon>
          <span>商品管理</span>
        </el-menu-item>
        <el-menu-item index="/order">
          <el-icon>
            <List />
          </el-icon>
          <span>订单管理</span>
        </el-menu-item>
        <el-menu-item index="/marketing">
          <el-icon>
            <Present />
          </el-icon>
          <span>营销配置</span>
        </el-menu-item>
        <el-menu-item index="/dashboard">
          <el-icon>
            <PieChart />
          </el-icon>
          <span>数据看板</span>
        </el-menu-item>
      </el-menu>
    </el-aside>

    <el-container>
      <el-header class="header">
        <div class="breadcrumb">当前位置：{{ route.meta.title }}</div>
        <div class="header-right">
          <span class="admin-name">{{ adminName }}</span>
          <el-button link
                     type="danger"
                     @click="logout">退出登录</el-button>
        </div>
      </el-header>
      <el-main class="main">
        <router-view />
      </el-main>
    </el-container>
  </el-container>
</template>

<script setup>
import { computed } from 'vue'
import { useRoute, useRouter } from 'vue-router'
import { ElMessage } from 'element-plus'

const route = useRoute()
const router = useRouter()

const adminName = computed(() => {
  try {
    const raw = localStorage.getItem('admin_user')
    const user = raw ? JSON.parse(raw) : null
    return user?.userName || 'admin'
  } catch {
    return 'admin'
  }
})

const logout = () => {
  localStorage.removeItem('admin_token')
  localStorage.removeItem('admin_user')
  ElMessage.success('已退出登录')
  router.replace('/login')
}
</script>

<style scoped>
.layout-container {
  height: 100vh;
}
.aside {
  background-color: #304156;
  color: #fff;
}
.logo {
  height: 60px;
  line-height: 60px;
  text-align: center;
  font-size: 18px;
  font-weight: bold;
  background: #2b3a4d;
}
.header {
  background: #fff;
  border-bottom: 1px solid #ddd;
  display: flex;
  align-items: center;
  justify-content: space-between;
  padding: 0 20px;
}
.header-right {
  display: flex;
  align-items: center;
  gap: 12px;
}
.admin-name {
  color: #606266;
  font-size: 14px;
}
.main {
  background: #f0f2f5;
  padding: 20px;
}
</style>