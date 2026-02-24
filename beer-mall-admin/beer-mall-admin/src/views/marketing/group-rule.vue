<template>
  <div class="page-content"
       style="max-width: 600px">
    <el-card header="全场拼团规则配置">
      <el-form :model="form"
               label-width="120px"
               v-loading="loading">
        <el-form-item label="启用活动">
          <el-switch v-model="form.isActive"
                     active-text="开启"
                     inactive-text="关闭" />
        </el-form-item>

        <el-form-item label="成团人数">
          <el-input-number v-model="form.requiredPeople"
                           :min="2"
                           :max="10" />
          <span class="tip">人</span>
        </el-form-item>

        <el-form-item label="折扣力度">
          <el-input-number v-model="form.discountRate"
                           :min="0.1"
                           :max="0.99"
                           :step="0.01" />
          <div class="tip">0.85 代表 85折，0.9 代表 9折</div>
        </el-form-item>

        <el-form-item label="成团时限">
          <el-input-number v-model="form.durationHours"
                           :min="1" />
          <span class="tip">小时 (过期自动退款)</span>
        </el-form-item>

        <el-form-item>
          <el-button type="primary"
                     @click="save"
                     :loading="submitting">保存配置</el-button>
        </el-form-item>
      </el-form>
    </el-card>
  </div>
</template>

<script setup>
import { ref, onMounted } from 'vue'
import request from '@/utils/request'
import { ElMessage } from 'element-plus'

const loading = ref(false)
const submitting = ref(false)
const form = ref({
  isActive: true,
  requiredPeople: 3,
  discountRate: 0.85,
  durationHours: 24
})

// 加载规则 (ProductId=0 代表全场通用)
const loadRule = async () => {
  loading.value = true
  try {
    // 注意：这里需要确保后端 AdminController.GetRule(0) 可用
    const res = await request.get('/admin/0')
    if (res) form.value = res
  } finally {
    loading.value = false
  }
}

const save = async () => {
  submitting.value = true
  try {
    await request.post('/admin/rule/update', form.value)
    ElMessage.success('规则已更新')
  } finally {
    submitting.value = false
  }
}

onMounted(() => loadRule())
</script>

<style scoped>
.page-content {
  padding: 20px;
}
.tip {
  margin-left: 10px;
  color: #999;
  font-size: 12px;
}
</style>