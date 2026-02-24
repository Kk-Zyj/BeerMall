<template>
  <div class="page-content">
    <el-tabs v-model="activeStatus"
             @tab-change="loadList">
      <el-tab-pane label="全部订单"
                   name="-99"></el-tab-pane>
      <el-tab-pane label="待付款"
                   name="0"></el-tab-pane>
      <el-tab-pane label="待发货"
                   name="1"></el-tab-pane>
      <el-tab-pane label="待成团"
                   name="10"></el-tab-pane>
      <el-tab-pane label="已发货"
                   name="2"></el-tab-pane>
    </el-tabs>

    <el-table :data="list"
              v-loading="loading"
              border
              style="margin-top:15px">
      <el-table-column prop="orderNo"
                       label="订单号"
                       width="180" />
      <el-table-column prop="receiverName"
                       label="收货人"
                       width="120" />
      <el-table-column prop="totalAmount"
                       label="金额">
        <template #default="{row}">¥{{ row.totalAmount }}</template>
      </el-table-column>
      <el-table-column prop="status"
                       label="状态"
                       width="100">
        <template #default="{row}">
          <el-tag v-if="row.status===1"
                  type="danger">待发货</el-tag>
          <el-tag v-else-if="row.status===2"
                  type="success">已发货</el-tag>
          <el-tag v-else
                  type="info">{{ row.status }}</el-tag>
        </template>
      </el-table-column>
      <el-table-column prop="createTime"
                       label="下单时间"
                       width="180" />
      <el-table-column label="操作">
        <template #default="{row}">
          <el-button v-if="row.status === 1"
                     type="primary"
                     size="small"
                     @click="handleShip(row)">去发货</el-button>
        </template>
      </el-table-column>
    </el-table>
  </div>
</template>

<script setup>
import { ref, onMounted } from 'vue'
import request from '@/utils/request'
import { ElMessage, ElMessageBox } from 'element-plus'

const activeStatus = ref('1') // 默认看待发货
const list = ref([])
const loading = ref(false)

const loadList = async () => {
  loading.value = true
  try {
    const params = {
      page: 1,
      pageSize: 10
    }

    // 如果 activeStatus 不是 '-99'，传给后端 status 参数
    if (activeStatus.value !== '-99') {
      params.status = parseInt(activeStatus.value)
    } else {
      params.status = -99 // 显式传 -99 对应后端的默认值
    }

    const res = await request.get('/admin/order/list', { params })
    list.value = res.list || []
    // 如果加了分页，别忘了把 res.total 也存下来
  } finally {
    loading.value = false
  }
}

const handleShip = (row) => {
  ElMessageBox.prompt('请输入物流单号', '发货确认', {
    confirmButtonText: '确认发货',
    cancelButtonText: '取消',
  }).then(async ({ value }) => {
    await request.post(`/admin/order/${row.id}/ship`, { trackingNo: value })
    ElMessage.success('发货成功')
    loadList()
  })
}

onMounted(() => {
  // 模拟数据 (因为你的 AdminOrderController.GetList 还没写)
  loadList()
})
</script>
<style scoped>
.page-content {
  background: #fff;
  padding: 20px;
}
</style>