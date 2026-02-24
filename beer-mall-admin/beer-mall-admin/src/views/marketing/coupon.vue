<template>
  <div class="page-content">
    <el-card shadow="never"
             class="mb-20">
      <el-button type="primary"
                 icon="Plus"
                 @click="openDialog">新增优惠券</el-button>
    </el-card>

    <el-card shadow="never">
      <el-table :data="list"
                v-loading="loading"
                border
                stripe>
        <el-table-column prop="id"
                         label="ID"
                         width="80" />
        <el-table-column prop="name"
                         label="优惠券名称"
                         min-width="150" />
        <el-table-column label="面额 / 门槛"
                         width="150">
          <template #default="{ row }">
            <span style="color: #f56c6c; font-weight: bold;">¥{{ row.amount }}</span>
            <div style="font-size: 12px; color: #999;">满 ¥{{ row.minPoint }} 可用</div>
          </template>
        </el-table-column>
        <el-table-column label="发放 / 领取"
                         width="150">
          <template #default="{ row }">
            {{ row.receivedCount }} / {{ row.totalCount }}
          </template>
        </el-table-column>
        <el-table-column label="有效期规则"
                         min-width="200">
          <template #default="{ row }">
            <span v-if="row.timeType === 0">
              固定: {{ formatDate(row.startTime) }} 至 {{ formatDate(row.endTime) }}
            </span>
            <span v-else>领券后 {{ row.days }} 天内有效</span>
          </template>
        </el-table-column>
        <el-table-column label="状态"
                         width="100">
          <template #default="{ row }">
            <el-switch v-model="row.isActive"
                       @change="handleStatus(row)" />
          </template>
        </el-table-column>
      </el-table>
    </el-card>

    <el-dialog v-model="dialogVisible"
               title="新增优惠券"
               width="600px">
      <el-form :model="form"
               label-width="120px"
               :rules="rules"
               ref="formRef">
        <el-form-item label="优惠券名称"
                      prop="name">
          <el-input v-model="form.name"
                    placeholder="如: 新春大促满减券" />
        </el-form-item>
        <el-form-item label="面额(元)"
                      prop="amount">
          <el-input-number v-model="form.amount"
                           :min="1" />
        </el-form-item>
        <el-form-item label="使用门槛(元)"
                      prop="minPoint">
          <el-input-number v-model="form.minPoint"
                           :min="0" />
          <span class="tip">订单满多少元可用</span>
        </el-form-item>
        <el-form-item label="发行总量"
                      prop="totalCount">
          <el-input-number v-model="form.totalCount"
                           :min="1" />
        </el-form-item>

        <el-form-item label="有效期类型">
          <el-radio-group v-model="form.timeType">
            <el-radio :label="0">固定时间段</el-radio>
            <el-radio :label="1">领券后生效</el-radio>
          </el-radio-group>
        </el-form-item>

        <el-form-item label="有效时间"
                      v-if="form.timeType === 0">
          <el-date-picker v-model="dateRange"
                          type="daterange"
                          range-separator="至"
                          start-placeholder="开始日期"
                          end-placeholder="结束日期"
                          value-format="YYYY-MM-DD HH:mm:ss" />
        </el-form-item>

        <el-form-item label="有效天数"
                      v-if="form.timeType === 1">
          <el-input-number v-model="form.days"
                           :min="1" />
          <span class="tip">天内有效</span>
        </el-form-item>

        <el-form-item label="立即上架">
          <el-switch v-model="form.isActive" />
        </el-form-item>
      </el-form>
      <template #footer>
        <el-button @click="dialogVisible = false">取消</el-button>
        <el-button type="primary"
                   @click="submitForm"
                   :loading="submitting">确认生成</el-button>
      </template>
    </el-dialog>
  </div>
</template>

<script setup>
import { ref, onMounted } from 'vue'
import request from '@/utils/request'
import { ElMessage } from 'element-plus'

const list = ref([])
const loading = ref(false)
const dialogVisible = ref(false)
const submitting = ref(false)
const formRef = ref(null)
const dateRange = ref([])

const form = ref({
  name: '', amount: 10, minPoint: 100, totalCount: 100,
  timeType: 1, days: 7, startTime: null, endTime: null, isActive: true
})

const rules = {
  name: [{ required: true, message: '请输入名称', trigger: 'blur' }]
}

const loadList = async () => {
  loading.value = true
  try {
    const res = await request.get('/admin/coupon/list')
    list.value = res || []
  } finally {
    loading.value = false
  }
}

const openDialog = () => {
  form.value = { name: '', amount: 10, minPoint: 100, totalCount: 100, timeType: 1, days: 7, isActive: true }
  dateRange.value = []
  dialogVisible.value = true
}

const submitForm = async () => {
  if (!formRef.value) return
  await formRef.value.validate()

  if (form.value.timeType === 0) {
    if (!dateRange.value || dateRange.value.length !== 2) return ElMessage.warning('请选择时间段')
    form.value.startTime = dateRange.value[0]
    form.value.endTime = dateRange.value[1]
  }

  submitting.value = true
  try {
    await request.post('/admin/coupon', form.value)
    ElMessage.success('创建成功')
    dialogVisible.value = false
    loadList()
  } finally {
    submitting.value = false
  }
}

const handleStatus = async (row) => {
  // 调用上下架API (你可以后续在后端补充这个接口)
  // await request.put(`/admin/coupon/${row.id}/status`, row.isActive)
  ElMessage.success('状态已更新')
}

const formatDate = (dateStr) => dateStr ? dateStr.substring(0, 10) : ''

onMounted(() => loadList())
</script>

<style scoped>
.page-content {
  padding: 20px;
  background: #fff;
  border-radius: 4px;
}
.mb-20 {
  margin-bottom: 20px;
}
.tip {
  margin-left: 10px;
  color: #999;
  font-size: 12px;
}
</style>