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
        <div class="breadcrumb">当前位置：{{ $route.meta.title }}</div>
      </el-header>
      <el-main class="main">
        <router-view />
      </el-main>
    </el-container>
  </el-container>
</template>

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
  padding: 0 20px;
}
.main {
  background: #f0f2f5;
  padding: 20px;
}
</style>