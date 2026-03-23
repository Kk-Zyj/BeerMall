<template>
  <div class="login-page">
    <el-card class="login-card"
             shadow="hover">
      <template #header>
        <div class="title">BeerMall 管理后台登录</div>
      </template>

      <el-form :model="form"
               @keyup.enter="handleLogin">
        <el-form-item label="账号">
          <el-input v-model="form.userName"
                    placeholder="请输入管理员账号" />
        </el-form-item>

        <el-form-item label="密码">
          <el-input v-model="form.password"
                    type="password"
                    show-password
                    placeholder="请输入管理员密码" />
        </el-form-item>

        <el-form-item>
          <el-button type="primary"
                     style="width: 100%;"
                     :loading="loading"
                     @click="handleLogin">
            登录
          </el-button>
        </el-form-item>
      </el-form>

      <div class="hint">
        默认账号：admin<br />
        默认密码：Admin@123456
      </div>
    </el-card>
  </div>
</template>

<script setup>
import { reactive, ref } from 'vue'
import { useRouter } from 'vue-router'
import { ElMessage } from 'element-plus'
import request, { ADMIN_TOKEN_KEY } from '@/utils/request'

const router = useRouter()
const loading = ref(false)

const form = reactive({
  userName: 'admin',
  password: 'Admin@123456'
})

const handleLogin = async () => {
  if (!form.userName.trim()) {
    ElMessage.error('请输入账号')
    return
  }
  if (!form.password) {
    ElMessage.error('请输入密码')
    return
  }

  loading.value = true
  try {
    const res = await request.post('/admin/auth/login', {
      userName: form.userName,
      password: form.password
    })

    localStorage.setItem(ADMIN_TOKEN_KEY, res.token)
    localStorage.setItem('admin_user', JSON.stringify(res.user || {}))
    ElMessage.success('登录成功')
    router.replace('/')
  } finally {
    loading.value = false
  }
}
</script>

<style scoped>
.login-page {
  min-height: 100vh;
  display: flex;
  align-items: center;
  justify-content: center;
  background: linear-gradient(135deg, #eef2f7, #dfe8f5);
}
.login-card {
  width: 420px;
  border-radius: 14px;
}
.title {
  text-align: center;
  font-size: 20px;
  font-weight: 700;
}
.hint {
  margin-top: 8px;
  color: #909399;
  font-size: 13px;
  line-height: 1.8;
}
</style>