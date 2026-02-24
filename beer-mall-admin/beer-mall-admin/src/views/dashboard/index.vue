<template>
  <div class="dashboard-container">
    <el-row :gutter="20"
            class="mb-20">
      <el-col :span="6">
        <el-card shadow="hover"
                 class="data-card">
          <div class="card-header">
            <span>今日营收</span>
            <el-tag type="danger"
                    size="small">日</el-tag>
          </div>
          <div class="card-value">¥ {{ summary.todayRevenue.toFixed(2) }}</div>
        </el-card>
      </el-col>

      <el-col :span="6">
        <el-card shadow="hover"
                 class="data-card">
          <div class="card-header">
            <span>今日付款订单</span>
            <el-tag type="success"
                    size="small">笔</el-tag>
          </div>
          <div class="card-value">{{ summary.todayCount }}</div>
        </el-card>
      </el-col>

      <el-col :span="6">
        <el-card shadow="hover"
                 class="data-card">
          <div class="card-header">
            <span>待发货订单</span>
            <el-tag type="warning"
                    size="small">急</el-tag>
          </div>
          <div class="card-value text-warning">{{ summary.pendingShipmentCount }}</div>
        </el-card>
      </el-col>

      <el-col :span="6">
        <el-card shadow="hover"
                 class="data-card">
          <div class="card-header">
            <span>累计总营收</span>
            <el-tag type="info"
                    size="small">总</el-tag>
          </div>
          <div class="card-value">¥ {{ summary.totalRevenue.toFixed(2) }}</div>
        </el-card>
      </el-col>
    </el-row>

    <el-card shadow="never">
      <template #header>
        <div class="clearfix">
          <span>近 7 天营收趋势</span>
        </div>
      </template>
      <div ref="chartRef"
           style="height: 400px; width: 100%;"></div>
    </el-card>
  </div>
</template>

<script setup>
import { ref, onMounted, onUnmounted } from 'vue'
import request from '@/utils/request'
import * as echarts from 'echarts'

const summary = ref({
  todayRevenue: 0,
  todayCount: 0,
  pendingShipmentCount: 0,
  totalRevenue: 0
})

const chartRef = ref(null)
let myChart = null

// 获取头部概览数据
const loadSummary = async () => {
  try {
    const res = await request.get('/admin/dashboard/summary')
    summary.value = res
  } catch (error) {
    console.error('获取统计数据失败', error)
  }
}

// 获取并绘制折线图
const loadChart = async () => {
  try {
    const res = await request.get('/admin/dashboard/chart')

    // 初始化 ECharts 实例
    if (!myChart) {
      myChart = echarts.init(chartRef.value)
    }

    // 配置图表参数
    const option = {
      tooltip: {
        trigger: 'axis',
        formatter: '{b} <br/> 营收: ¥{c}'
      },
      grid: {
        left: '3%',
        right: '4%',
        bottom: '3%',
        containLabel: true
      },
      xAxis: {
        type: 'category',
        boundaryGap: false,
        data: res.dates // 后端返回的日期数组 ['02-05', '02-06', ...]
      },
      yAxis: {
        type: 'value',
        axisLabel: { formatter: '¥ {value}' }
      },
      series: [
        {
          name: '营收',
          type: 'line',
          smooth: true, // 平滑曲线
          data: res.amounts, // 后端返回的金额数组
          itemStyle: { color: '#ff4d4f' },
          areaStyle: {
            color: new echarts.graphic.LinearGradient(0, 0, 0, 1, [
              { offset: 0, color: 'rgba(255, 77, 79, 0.3)' },
              { offset: 1, color: 'rgba(255, 77, 79, 0)' }
            ])
          }
        }
      ]
    }

    myChart.setOption(option)
  } catch (error) {
    console.error('获取图表数据失败', error)
  }
}

// 监听窗口大小变化，让图表自适应伸缩
const handleResize = () => {
  if (myChart) myChart.resize()
}

onMounted(() => {
  loadSummary()
  loadChart()
  window.addEventListener('resize', handleResize)
})

onUnmounted(() => {
  window.removeEventListener('resize', handleResize)
  if (myChart) myChart.dispose()
})
</script>

<style scoped>
.dashboard-container {
  padding: 10px;
}
.mb-20 {
  margin-bottom: 20px;
}
.data-card {
  border-radius: 8px;
}
.card-header {
  display: flex;
  justify-content: space-between;
  align-items: center;
  color: #666;
  font-size: 14px;
  margin-bottom: 15px;
}
.card-value {
  font-size: 28px;
  font-weight: bold;
  color: #303133;
}
.text-warning {
  color: #e6a23c;
}
</style>