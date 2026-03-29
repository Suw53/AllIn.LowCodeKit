<!-- 自动化配置页：登录方案 + Playwright路径 -->
<script setup lang="ts">
import { ref, onMounted } from 'vue'
import { ElMessage } from 'element-plus'
import { getConfigs, setConfig } from '@/api/globalConfig'

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

// ────────── 初始化 ──────────
onMounted(async () => {
  await Promise.all([loadLoginSchemes(), loadPlaywrightConfig()])
})
</script>

<template>
  <div class="automation-config-page">
    <div class="page-header">
      <span class="page-title">自动化配置</span>
    </div>

    <div class="config-body">
      <!-- 登录配置 -->
      <div class="config-section">
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

      <!-- Playwright 配置 -->
      <div class="config-section">
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
    </div>

    <!-- 新增/编辑登录方案对话框 -->
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
.automation-config-page {
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

.scheme-table {
  max-width: 700px;
}

.form-hint {
  font-size: 11px;
  color: #909399;
  margin-top: 4px;
}
</style>
