<!-- 全局配置页：登录方案 / Playwright路径 / 个性化主题 -->
<script setup lang="ts">
import { ref, onMounted } from 'vue'
import { useRoute } from 'vue-router'
import { ElMessage } from 'element-plus'
import { getConfigs, setConfig } from '@/api/globalConfig'
import { useThemeStore, THEME_VARS } from '@/stores/themeStore'
import { useAppConfigStore } from '@/stores/appConfigStore'

const route = useRoute()
const activeTab = ref((route.query.tab as string) || 'login')
const themeStore = useThemeStore()
const appConfigStore = useAppConfigStore()

// ────────── 登录方案 ──────────
interface LoginScheme {
  id: string
  name: string
  cdpAddress: string
}

const loginSchemes = ref<LoginScheme[]>([])
const loginSaving = ref(false)
const addLoginDialogVisible = ref(false)
const editingScheme = ref<LoginScheme | null>(null)
const loginForm = ref({ name: '', cdpAddress: '' })

async function loadLoginSchemes() {
  const configs = await getConfigs('login')
  const found = configs.find(c => c.key === 'schemes')
  if (found?.value) {
    try { loginSchemes.value = JSON.parse(found.value) } catch { loginSchemes.value = [] }
  } else {
    loginSchemes.value = []
  }
}

async function saveLoginSchemes() {
  loginSaving.value = true
  try {
    await setConfig('login', 'schemes', JSON.stringify(loginSchemes.value))
    ElMessage.success('保存成功')
  } catch {
    ElMessage.error('保存失败')
  } finally {
    loginSaving.value = false
  }
}

function openAddLogin() {
  editingScheme.value = null
  loginForm.value = { name: '', cdpAddress: '' }
  addLoginDialogVisible.value = true
}

function openEditLogin(scheme: LoginScheme) {
  editingScheme.value = scheme
  loginForm.value = { name: scheme.name, cdpAddress: scheme.cdpAddress }
  addLoginDialogVisible.value = true
}

async function confirmLoginForm() {
  if (!loginForm.value.name.trim()) {
    ElMessage.warning('请输入方案名称')
    return
  }
  if (editingScheme.value) {
    editingScheme.value.name = loginForm.value.name
    editingScheme.value.cdpAddress = loginForm.value.cdpAddress
  } else {
    loginSchemes.value.push({
      id: Date.now().toString(),
      name: loginForm.value.name,
      cdpAddress: loginForm.value.cdpAddress
    })
  }
  addLoginDialogVisible.value = false
  await saveLoginSchemes()
}

async function deleteLoginScheme(id: string) {
  loginSchemes.value = loginSchemes.value.filter(s => s.id !== id)
  await saveLoginSchemes()
}

// ────────── Playwright 配置 ──────────
const playwrightConfig = ref({ executablePath: '', playwrightPath: '' })
const playwrightSaving = ref(false)

async function loadPlaywrightConfig() {
  const configs = await getConfigs('playwright')
  playwrightConfig.value.executablePath = configs.find(c => c.key === 'executablePath')?.value ?? ''
  playwrightConfig.value.playwrightPath = configs.find(c => c.key === 'playwrightPath')?.value ?? ''
}

async function savePlaywrightConfig() {
  playwrightSaving.value = true
  try {
    await Promise.all([
      setConfig('playwright', 'executablePath', playwrightConfig.value.executablePath, '浏览器可执行文件路径'),
      setConfig('playwright', 'playwrightPath', playwrightConfig.value.playwrightPath, 'Playwright安装目录路径')
    ])
    ElMessage.success('保存成功')
  } catch {
    ElMessage.error('保存失败')
  } finally {
    playwrightSaving.value = false
  }
}

// ────────── 个性化主题 ──────────
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

function handleLogoChange(file: any) {
  console.log('Logo 文件选择触发:', file)
  const rawFile = file.raw
  if (!rawFile) {
    console.error('无法获取文件对象')
    return
  }
  if (rawFile.size > 500 * 1024) {
    ElMessage.warning('图片大小不能超过 500KB')
    return
  }
  const reader = new FileReader()
  reader.onload = (e) => {
    const result = e.target?.result as string
    appConfigStore.config.logo = result
    console.log('Logo 已读取，长度:', result.length, '预览:', result.substring(0, 50) + '...')
  }
  reader.onerror = () => {
    console.error('图片读取失败')
    ElMessage.error('图片读取失败')
  }
  reader.readAsDataURL(rawFile)
}

function handleFaviconChange(file: any) {
  console.log('Favicon 文件选择触发:', file)
  const rawFile = file.raw
  if (!rawFile) {
    console.error('无法获取文件对象')
    return
  }
  if (rawFile.size > 500 * 1024) {
    ElMessage.warning('图片大小不能超过 500KB')
    return
  }
  const reader = new FileReader()
  reader.onload = (e) => {
    const result = e.target?.result as string
    appConfigStore.config.favicon = result
    console.log('Favicon 已读取，长度:', result.length, '预览:', result.substring(0, 50) + '...')
  }
  reader.onerror = () => {
    console.error('图片读取失败')
    ElMessage.error('图片读取失败')
  }
  reader.readAsDataURL(rawFile)
}

// ────────── 初始化 ──────────
onMounted(async () => {
  await Promise.all([loadLoginSchemes(), loadPlaywrightConfig()])
})
</script>

<template>
  <div class="global-config-page">
    <div class="page-header">
      <span class="page-title">全局配置</span>
    </div>

    <div class="config-body">
      <el-tabs v-model="activeTab" tab-position="left" class="config-tabs">

        <!-- 自动化配置分组 -->
        <el-tab-pane disabled>
          <template #label>
            <span style="font-weight: 600; color: #909399; font-size: 12px;">自动化配置</span>
          </template>
        </el-tab-pane>

        <!-- ── 登录配置 ── -->
        <el-tab-pane label="  登录配置" name="login">
          <div class="tab-content">
            <div class="section-header">
              <span class="section-title">登录方案</span>
              <el-button type="primary" size="small" icon="Plus" @click="openAddLogin">新增方案</el-button>
            </div>
            <p class="section-desc">配置浏览器CDP连接地址，供自动化流程调用已登录的浏览器实例。</p>

            <el-empty v-if="loginSchemes.length === 0" description="暂无登录方案" :image-size="60" />

            <div v-else class="scheme-table">
              <el-table :data="loginSchemes" border size="small">
                <el-table-column prop="name" label="方案名称" min-width="140" />
                <el-table-column prop="cdpAddress" label="CDP 连接地址" min-width="220">
                  <template #default="{ row }">
                    <el-tag type="info" size="small">{{ row.cdpAddress || '未配置' }}</el-tag>
                  </template>
                </el-table-column>
                <el-table-column label="操作" width="120" fixed="right">
                  <template #default="{ row }">
                    <el-button link type="primary" size="small" @click="openEditLogin(row)">编辑</el-button>
                    <el-button link type="danger" size="small" @click="deleteLoginScheme(row.id)">删除</el-button>
                  </template>
                </el-table-column>
              </el-table>
            </div>
          </div>
        </el-tab-pane>

        <!-- ── Playwright 配置 ── -->
        <el-tab-pane label="  Playwright" name="playwright">
          <div class="tab-content">
            <div class="section-header">
              <span class="section-title">Playwright 路径配置</span>
            </div>
            <p class="section-desc">配置 Playwright 浏览器自动化所需的文件路径。</p>

            <el-form label-width="160px" style="max-width: 600px;">
              <el-form-item label="浏览器可执行路径">
                <el-input
                  v-model="playwrightConfig.executablePath"
                  placeholder="如 C:\Program Files\Chrome\chrome.exe"
                  clearable
                />
              </el-form-item>
              <el-form-item label="Playwright 安装目录">
                <el-input
                  v-model="playwrightConfig.playwrightPath"
                  placeholder="如 C:\Users\xxx\.cache\ms-playwright"
                  clearable
                />
              </el-form-item>
              <el-form-item>
                <el-button type="primary" :loading="playwrightSaving" @click="savePlaywrightConfig">
                  保存
                </el-button>
              </el-form-item>
            </el-form>
          </div>
        </el-tab-pane>

        <!-- 系统配置分组 -->
        <el-tab-pane disabled>
          <template #label>
            <span style="font-weight: 600; color: #909399; font-size: 12px;">系统配置</span>
          </template>
        </el-tab-pane>

        <!-- ── 个性化 ── -->
        <el-tab-pane label="  个性化" name="theme">
          <div class="tab-content">

            <!-- 应用设置 -->
            <div class="section-header">
              <span class="section-title">应用设置</span>
              <el-button type="primary" size="small" :loading="appSaving" @click="handleSaveApp">保存设置</el-button>
            </div>
            <p class="section-desc">配置网站标题、Logo 和图标。</p>

            <el-form label-width="120px" style="max-width: 600px; margin-bottom: 32px;">
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
                    :on-change="handleLogoChange"
                  >
                    <el-button size="small">选择图片</el-button>
                  </el-upload>
                  <div v-if="appConfigStore.config.logo" style="display: flex; align-items: center; gap: 8px;">
                    <img :src="appConfigStore.config.logo" style="height: 40px; max-width: 200px; object-fit: contain; border: 1px solid #dcdfe6; border-radius: 4px; padding: 4px;" />
                    <el-button size="small" text type="danger" @click="appConfigStore.config.logo = ''">清除</el-button>
                  </div>
                  <span v-else style="font-size: 12px; color: #909399;">未上传（支持 PNG、JPG、SVG，大小不超过 500KB）</span>
                </div>
              </el-form-item>
              <el-form-item label="Favicon 图标">
                <div style="display: flex; align-items: center; gap: 12px;">
                  <el-upload
                    :auto-upload="false"
                    :show-file-list="false"
                    accept="image/png,image/jpeg,image/x-icon"
                    :on-change="handleFaviconChange"
                  >
                    <el-button size="small">选择图标</el-button>
                  </el-upload>
                  <div v-if="appConfigStore.config.favicon" style="display: flex; align-items: center; gap: 8px;">
                    <img :src="appConfigStore.config.favicon" style="height: 32px; max-width: 200px; object-fit: contain; border: 1px solid #dcdfe6; border-radius: 4px; padding: 4px;" />
                    <el-button size="small" text type="danger" @click="appConfigStore.config.favicon = ''">清除</el-button>
                  </div>
                  <span v-else style="font-size: 12px; color: #909399;">未上传（支持 PNG、JPG、ICO，大小不超过 500KB）</span>
                </div>
              </el-form-item>
            </el-form>

            <!-- 主题颜色 -->
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
        </el-tab-pane>

      </el-tabs>
    </div>

    <!-- ── 新增/编辑登录方案对话框 ── -->
    <el-dialog
      v-model="addLoginDialogVisible"
      :title="editingScheme ? '编辑登录方案' : '新增登录方案'"
      width="420px"
    >
      <el-form label-width="100px">
        <el-form-item label="方案名称" required>
          <el-input v-model="loginForm.name" placeholder="如：企业微信账号" />
        </el-form-item>
        <el-form-item label="CDP 地址">
          <el-input v-model="loginForm.cdpAddress" placeholder="如：http://127.0.0.1:9222" />
          <div class="form-hint">启动浏览器时添加 --remote-debugging-port=9222 参数</div>
        </el-form-item>
      </el-form>
      <template #footer>
        <el-button @click="addLoginDialogVisible = false">取消</el-button>
        <el-button type="primary" @click="confirmLoginForm">确定</el-button>
      </template>
    </el-dialog>
  </div>
</template>

<style scoped>
.global-config-page {
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
  overflow: hidden;
  padding: 16px;
}

.config-tabs {
  height: 100%;
  background: #fff;
  border-radius: 4px;
  box-shadow: 0 1px 4px rgba(0, 0, 0, 0.08);
}

.config-tabs :deep(.el-tabs__content) {
  height: 100%;
  overflow-y: auto;
}

.config-tabs :deep(.el-tab-pane) {
  height: 100%;
}

.tab-content {
  padding: 20px 24px;
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

.scheme-table {
  max-width: 700px;
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

.form-hint {
  font-size: 11px;
  color: #909399;
  margin-top: 4px;
}
</style>
