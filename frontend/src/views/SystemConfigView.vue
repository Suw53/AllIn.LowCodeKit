<!-- 系统配置页：个性化（应用设置 + 主题颜色） -->
<script setup lang="ts">
import { ref } from 'vue'
import { ElMessage } from 'element-plus'
import { useThemeStore, THEME_VARS } from '@/stores/themeStore'
import { useAppConfigStore } from '@/stores/appConfigStore'

const themeStore = useThemeStore()
const appConfigStore = useAppConfigStore()

// ────────── 应用设置 ──────────
const appSaving = ref(false)

async function handleSaveApp() {
  appSaving.value = true
  try {
    await appConfigStore.saveAppConfig()
    ElMessage.success('应用设置已保存')
  } catch {
    ElMessage.error('保存失败')
  } finally {
    appSaving.value = false
  }
}

function handleLogoUpload(file: File) {
  if (file.size > 500 * 1024) {
    ElMessage.warning('图片大小不能超过 500KB')
    return false
  }
  const reader = new FileReader()
  reader.onload = (e) => {
    appConfigStore.config.logo = e.target?.result as string
  }
  reader.readAsDataURL(file)
  return false
}

function handleFaviconUpload(file: File) {
  if (file.size > 500 * 1024) {
    ElMessage.warning('图片大小不能超过 500KB')
    return false
  }
  const reader = new FileReader()
  reader.onload = (e) => {
    appConfigStore.config.favicon = e.target?.result as string
  }
  reader.readAsDataURL(file)
  return false
}

// ────────── 主题颜色 ──────────
const themeSaving = ref(false)

async function handleSaveTheme() {
  themeSaving.value = true
  try {
    await themeStore.saveTheme()
    ElMessage.success('主题已保存')
  } catch {
    ElMessage.error('保存失败')
  } finally {
    themeSaving.value = false
  }
}

function handleResetTheme() {
  themeStore.resetToDefault()
}
</script>

<template>
  <div class="system-config-page">
    <div class="page-header">
      <span class="page-title">系统配置</span>
    </div>

    <div class="config-body">
      <!-- 应用设置 -->
      <div class="config-section">
        <div class="section-header">
          <span class="section-title">应用设置</span>
          <el-button type="primary" size="small" :loading="appSaving" @click="handleSaveApp">保存设置</el-button>
        </div>
        <p class="section-desc">配置网站标题、Logo 和图标。</p>

        <el-form label-width="120px" style="max-width: 600px;">
          <el-form-item label="网站标题">
            <el-input v-model="appConfigStore.config.title" placeholder="AllIn LowCode Kit" clearable />
          </el-form-item>
          <el-form-item label="侧边栏名称">
            <el-input v-model="appConfigStore.config.sidebarName" placeholder="AllIn LowCode Kit" clearable />
          </el-form-item>
          <el-form-item label="Logo 图片">
            <div style="display: flex; align-items: center; gap: 12px;">
              <el-upload
                :auto-upload="false"
                :show-file-list="false"
                accept="image/png,image/jpeg,image/svg+xml"
                :before-upload="handleLogoUpload"
              >
                <el-button size="small">选择图片</el-button>
              </el-upload>
              <img v-if="appConfigStore.config.logo" :src="appConfigStore.config.logo" style="height: 40px;" />
              <span v-else style="font-size: 12px; color: #909399;">未上传</span>
            </div>
            <div class="form-hint">支持 PNG、JPG、SVG，大小不超过 500KB</div>
          </el-form-item>
          <el-form-item label="Favicon 图标">
            <div style="display: flex; align-items: center; gap: 12px;">
              <el-upload
                :auto-upload="false"
                :show-file-list="false"
                accept="image/png,image/jpeg,image/x-icon"
                :before-upload="handleFaviconUpload"
              >
                <el-button size="small">选择图标</el-button>
              </el-upload>
              <img v-if="appConfigStore.config.favicon" :src="appConfigStore.config.favicon" style="height: 32px;" />
              <span v-else style="font-size: 12px; color: #909399;">未上传</span>
            </div>
            <div class="form-hint">支持 PNG、JPG、ICO，大小不超过 500KB</div>
          </el-form-item>
        </el-form>
      </div>

      <!-- 主题颜色 -->
      <div class="config-section">
        <div class="section-header">
          <span class="section-title">主题颜色</span>
          <div style="display: flex; gap: 8px;">
            <el-button size="small" @click="handleResetTheme">恢复默认</el-button>
            <el-button type="primary" size="small" :loading="themeSaving" @click="handleSaveTheme">保存主题</el-button>
          </div>
        </div>
        <p class="section-desc">自定义界面主题颜色，修改后实时预览，点击"保存主题"持久化。</p>

        <div class="theme-grid">
          <div
            v-for="tv in THEME_VARS"
            :key="tv.key"
            class="theme-item"
          >
            <span class="theme-label">{{ tv.label }}</span>
            <div class="theme-picker-row">
              <el-color-picker
                :model-value="themeStore.colors[tv.key] ?? tv.defaultValue"
                size="large"
                @update:model-value="themeStore.previewColor(tv.key, $event)"
              />
              <span class="theme-value">{{ themeStore.colors[tv.key] ?? tv.defaultValue }}</span>
            </div>
          </div>
        </div>
      </div>
    </div>
  </div>
</template>

<style scoped>
.system-config-page {
  height: 100vh;
  display: flex;
  flex-direction: column;
  background: #f0f2f5;
  overflow: hidden;
}

.page-header {
  height: 52px;
  flex-shrink: 0;
  background: #fff;
  border-bottom: 1px solid #e4e7ed;
  display: flex;
  align-items: center;
  padding: 0 16px;
}

.page-title {
  font-size: 15px;
  font-weight: 600;
  color: #303133;
}

.config-body {
  flex: 1;
  overflow-y: auto;
  padding: 16px;
}

.config-section {
  background: #fff;
  border-radius: 4px;
  padding: 20px 24px;
  margin-bottom: 16px;
  box-shadow: 0 1px 4px rgba(0, 0, 0, 0.08);
}

.section-header {
  display: flex;
  align-items: center;
  justify-content: space-between;
  margin-bottom: 8px;
}

.section-title {
  font-size: 14px;
  font-weight: 600;
  color: #303133;
}

.section-desc {
  font-size: 12px;
  color: #909399;
  margin-bottom: 16px;
  margin-top: 0;
}

.form-hint {
  font-size: 11px;
  color: #909399;
  margin-top: 4px;
}

.theme-grid {
  display: grid;
  grid-template-columns: repeat(auto-fill, minmax(220px, 1fr));
  gap: 16px;
  max-width: 700px;
}

.theme-item {
  background: #f5f7fa;
  border-radius: 6px;
  padding: 12px 16px;
  display: flex;
  flex-direction: column;
  gap: 10px;
}

.theme-label {
  font-size: 13px;
  color: #606266;
  font-weight: 500;
}

.theme-picker-row {
  display: flex;
  align-items: center;
  gap: 10px;
}

.theme-value {
  font-size: 12px;
  color: #909399;
  font-family: monospace;
}
</style>
