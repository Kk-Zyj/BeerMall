<template>
  <div class="page-content">
    <el-card shadow="never"
             class="mb-20">
      <el-form :inline="true">
        <el-form-item label="商品名称">
          <el-input v-model="queryParams.keyword"
                    placeholder="请输入..."
                    clearable />
        </el-form-item>
        <el-form-item>
          <el-button type="primary"
                     icon="Search"
                     @click="loadList">查询</el-button>
          <el-button type="success"
                     icon="Plus"
                     @click="openDialog()">新增商品</el-button>
        </el-form-item>
      </el-form>
    </el-card>

    <el-card shadow="never">
      <el-table :data="tableData"
                v-loading="loading"
                border
                stripe>
        <el-table-column prop="id"
                         label="ID"
                         width="80" />
        <el-table-column label="主图"
                         width="100">
          <template #default="{ row }">
            <el-image :src="getImageUrl(row.mainImage)"
                      style="width: 60px; height: 60px; border-radius: 4px;"
                      fit="cover" />
          </template>
        </el-table-column>
        <el-table-column prop="name"
                         label="商品名称"
                         min-width="200" />
        <el-table-column prop="categoryName"
                         label="分类"
                         width="120" />
        <el-table-column label="价格区间"
                         width="150">
          <template #default="{ row }">
            <span style="color: #f56c6c; font-weight: bold;">¥{{ row.minPrice }} 起</span>
          </template>
        </el-table-column>
        <el-table-column prop="totalStock"
                         label="总库存"
                         width="100" />
        <el-table-column label="上架状态"
                         width="100">
          <template #default="{ row }">
            <el-switch v-model="row.isOnShelf"
                       @change="handleShelf(row)" />
          </template>
        </el-table-column>
        <el-table-column label="操作"
                         width="180"
                         fixed="right">
          <template #default="{ row }">
            <el-button type="primary"
                       link
                       @click="openDialog(row.id)">编辑</el-button>
            <el-button type="danger"
                       link>删除</el-button>
          </template>
        </el-table-column>
      </el-table>

      <div class="pagination-box">
        <el-pagination background
                       layout="prev, pager, next"
                       :total="total"
                       @current-change="handlePageChange" />
      </div>
    </el-card>

    <el-dialog v-model="dialogVisible"
               :title="form.id ? '编辑商品' : '新增商品'"
               width="850px"
               top="5vh">
      <el-form :model="form"
               label-width="100px">
        <el-row :gutter="20">
          <el-col :span="12">
            <el-form-item label="商品名称"
                          required>
              <el-input v-model="form.name"
                        placeholder="请输入商品名称" />
            </el-form-item>
          </el-col>
          <el-col :span="12">
            <el-form-item label="商品分类">
              <el-select v-model="form.categoryId"
                         style="width: 100%">
                <el-option label="精酿啤酒"
                           :value="1" />
                <el-option label="进口啤酒"
                           :value="2" />
              </el-select>
            </el-form-item>
          </el-col>
        </el-row>

        <el-form-item label="商品主图"
                      required>
          <el-upload class="avatar-uploader"
                     :action="uploadUrl"
                     :show-file-list="false"
                     :on-success="handleUploadSuccess"
                     :before-upload="beforeUpload">
            <img v-if="form.mainImage"
                 :src="getImageUrl(form.mainImage)"
                 class="avatar" />
            <el-icon v-else
                     class="avatar-uploader-icon">
              <Plus />
            </el-icon>
          </el-upload>
          <div class="el-upload__tip"
               style="margin-left: 15px; color: #999;">
            支持 jpg/png，建议尺寸 800x800，小于 5MB
          </div>
        </el-form-item>
        <el-form-item label="上架状态">
          <el-switch v-model="form.isOnShelf"
                     active-text="立即上架" />
        </el-form-item>

        <el-form-item label="规格明细"
                      required>
          <div style="width: 100%">
            <el-button type="primary"
                       size="small"
                       icon="Plus"
                       @click="addSkuRow"
                       style="margin-bottom: 10px;">添加规格</el-button>
            <el-table :data="form.skus"
                      border
                      size="small">
              <el-table-column label="规格名"
                               width="150">
                <template #default="{ row }"><el-input v-model="row.specName"
                            placeholder="如: 单瓶" /></template>
              </el-table-column>
              <el-table-column label="售价"
                               width="120">
                <template #default="{ row }"><el-input-number v-model="row.price"
                                   :min="0"
                                   :controls="false"
                                   style="width:100%" /></template>
              </el-table-column>
              <el-table-column label="原价"
                               width="120">
                <template #default="{ row }"><el-input-number v-model="row.originalPrice"
                                   :min="0"
                                   :controls="false"
                                   style="width:100%" /></template>
              </el-table-column>
              <el-table-column label="库存"
                               width="100">
                <template #default="{ row }"><el-input-number v-model="row.stock"
                                   :min="0"
                                   :controls="false"
                                   style="width:100%" /></template>
              </el-table-column>
              <el-table-column label="操作"
                               width="60"
                               align="center">
                <template #default="{ $index }">
                  <el-button type="danger"
                             icon="Delete"
                             circle
                             size="small"
                             @click="removeSkuRow($index)" />
                </template>
              </el-table-column>
            </el-table>
          </div>
        </el-form-item>

        <el-form-item label="商品详情">
          <el-input type="textarea"
                    :rows="4"
                    v-model="form.description" />
        </el-form-item>
      </el-form>
      <template #footer>
        <el-button @click="dialogVisible = false">取消</el-button>
        <el-button type="primary"
                   @click="submitForm"
                   :loading="submitting">保存提交</el-button>
      </template>
    </el-dialog>
  </div>
</template>

<script setup>
import { ref, reactive, onMounted } from 'vue'
import request from '@/utils/request'
import { ElMessage } from 'element-plus'
import { Plus } from '@element-plus/icons-vue' // 确保引了图标

// 后端 API 基础地址 (用于图片回显拼接)
const baseApiUrl = 'https://localhost:7252';

// 上传接口地址
const uploadUrl = `${baseApiUrl}/api/upload/image`;

// 上传成功的回调
const handleUploadSuccess = (response, uploadFile) => {
  // 假设后端返回 { url: '/uploads/images/xxx.jpg', success: true }
  if (response.url) {
    form.value.mainImage = response.url;
    ElMessage.success('图片上传成功');
  } else {
    ElMessage.error('图片上传失败');
  }
}

// 上传前的格式/大小校验
const beforeUpload = (file) => {
  const isImage = file.type.startsWith('image/');
  const isLt5M = file.size / 1024 / 1024 < 5;

  if (!isImage) {
    ElMessage.error('只能上传图片文件!');
  }
  if (!isLt5M) {
    ElMessage.error('图片大小不能超过 5MB!');
  }
  return isImage && isLt5M;
}

// 图片回显工具函数 (因为后端存的是 /uploads/...，前端展示要拼上域名)
const getImageUrl = (path) => {
  if (!path) return '';
  if (path.startsWith('http')) return path;
  return baseApiUrl + path;
}

const loading = ref(false)
const tableData = ref([])
const total = ref(0)
const dialogVisible = ref(false)
const submitting = ref(false)

const queryParams = reactive({ keyword: '', page: 1, pageSize: 10 })
const form = ref(initForm())

function initForm () {
  return {
    id: 0,
    name: '',
    categoryId: 1,
    mainImage: 'https://via.placeholder.com/150',
    description: '',
    sort: 0,
    isOnShelf: true,
    skus: [{ id: 0, specName: '单瓶', price: 0, originalPrice: 0, stock: 100, weight: 0 }]
  }
}

// 获取列表
const loadList = async () => {
  loading.value = true
  try {
    const res = await request.get('/admin/product/list', { params: queryParams })
    tableData.value = res.list
    total.value = res.total
  } finally {
    loading.value = false
  }
}

// 翻页
const handlePageChange = (page) => {
  queryParams.page = page
  loadList()
}

// 上下架
const handleShelf = async (row) => {
  try {
    await request.put(`/admin/product/${row.id}/shelf`, row.isOnShelf) // 直接传 bool，request 会处理 Content-Type
    ElMessage.success(row.isOnShelf ? '上架成功' : '已下架')
  } catch {
    row.isOnShelf = !row.isOnShelf
  }
}

// 打开弹窗
const openDialog = async (id = 0) => {
  if (id > 0) {
    const res = await request.get(`/admin/product/${id}`)
    form.value = res
  } else {
    form.value = initForm()
  }
  dialogVisible.value = true
}

// SKU 操作
const addSkuRow = () => form.value.skus.push({ id: 0, specName: '', price: 0, originalPrice: 0, stock: 0 })
const removeSkuRow = (idx) => form.value.skus.splice(idx, 1)

// 提交
const submitForm = async () => {
  if (form.value.skus.length === 0) return ElMessage.warning("请至少添加一个规格")

  submitting.value = true
  try {
    await request.post('/admin/product/save', form.value)
    ElMessage.success('保存成功')
    dialogVisible.value = false
    loadList()
  } finally {
    submitting.value = false
  }
}

onMounted(() => loadList())
</script>

<style scoped>
.page-content {
  background: #fff;
  padding: 20px;
  border-radius: 4px;
}
.mb-20 {
  margin-bottom: 20px;
}
.pagination-box {
  margin-top: 20px;
  text-align: right;
}
.avatar-uploader .el-upload {
  border: 1px dashed var(--el-border-color);
  border-radius: 6px;
  cursor: pointer;
  position: relative;
  overflow: hidden;
  transition: var(--el-transition-duration-fast);
}

.avatar-uploader .el-upload:hover {
  border-color: var(--el-color-primary);
}

.el-icon.avatar-uploader-icon {
  font-size: 28px;
  color: #8c939d;
  width: 120px;
  height: 120px;
  text-align: center;
  border: 1px dashed #dcdfe6;
  border-radius: 6px;
  background-color: #fafafa;
}
.el-icon.avatar-uploader-icon:hover {
  border-color: #409eff;
}

.avatar {
  width: 120px;
  height: 120px;
  display: block;
  object-fit: cover;
  border-radius: 6px;
}
</style>