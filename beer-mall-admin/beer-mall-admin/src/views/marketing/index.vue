<template>
  <div class="page-content">
    <el-card>
      <el-tabs v-model="activeTab">
        <el-tab-pane label="拼团规则"
                     name="group-rule">
          <GroupRule />
        </el-tab-pane>
        <el-tab-pane label="优惠券"
                     name="coupon">
          <Coupon />
        </el-tab-pane>
      </el-tabs>
    </el-card>
  </div>
</template>

<script setup>
import { ref } from 'vue'
import GroupRule from './group-rule.vue'
import Coupon from './coupon.vue'

const activeTab = ref('group-rule')
</script>

<style scoped>
.page-content {
  padding: 20px;
}
</style>
