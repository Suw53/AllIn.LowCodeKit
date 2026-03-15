<script setup lang="ts">
import { ref, onMounted } from 'vue'
import http from '@/api/http'
import type { HealthResult } from '@/types'

const backendStatus = ref<'checking' | 'ok' | 'error'>('checking')
const backendVersion = ref('')

onMounted(async () => {
  try {
    const res = await http.get<HealthResult>('/api/health')
    backendStatus.value = 'ok'
    backendVersion.value = (res as unknown as HealthResult).version
  } catch {
    backendStatus.value = 'error'
  }
})
</script>

<template>
  <div class="home-view">
    <div class="welcome-card">
      <div class="app-title">AllIn LowCode Kit</div>
      <div class="app-subtitle">低代码数据管理 · 自动化平台</div>
      <el-divider />
      <div class="status-row">
        <span class="status-label">后端服务</span>
        <el-tag v-if="backendStatus === 'checking'" type="info" size="small">检测中...</el-tag>
        <el-tag v-else-if="backendStatus === 'ok'" type="success" size="small">
          已连接 &nbsp;v{{ backendVersion }}
        </el-tag>
        <el-tag v-else type="danger" size="small">未连接（端口 5000）</el-tag>
      </div>
      <div class="status-row">
        <span class="status-label">数据库</span>
        <el-tag v-if="backendStatus === 'ok'" type="success" size="small">SQLite 就绪</el-tag>
        <el-tag v-else type="info" size="small">—</el-tag>
      </div>
      <el-divider />
      <div class="hint">请从左侧菜单选择功能模块开始使用</div>
    </div>
  </div>
</template>

<style scoped>
.home-view {
  display: flex;
  align-items: center;
  justify-content: center;
  height: 100%;
  background: #f0f2f5;
}

.welcome-card {
  background: #fff;
  border-radius: 8px;
  padding: 40px 60px;
  text-align: center;
  box-shadow: 0 2px 12px rgba(0, 0, 0, 0.08);
  min-width: 380px;
}

.app-title {
  font-size: 22px;
  font-weight: 600;
  color: #001529;
  margin-bottom: 8px;
}

.app-subtitle {
  font-size: 13px;
  color: #999;
}

.status-row {
  display: flex;
  align-items: center;
  justify-content: space-between;
  margin-bottom: 10px;
}

.status-label {
  font-size: 13px;
  color: #555;
}

.hint {
  font-size: 13px;
  color: #bbb;
  margin-top: 4px;
}
</style>
