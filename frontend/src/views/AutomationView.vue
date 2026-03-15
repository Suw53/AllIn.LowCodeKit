<!-- 自动化配置页：Monaco C# 编辑器 + 登录方案选择 + 脚本执行日志 -->
<script setup lang="ts">
import { ref, computed, onMounted } from 'vue'
import { useRoute, useRouter } from 'vue-router'
import { ElMessage } from 'element-plus'
import { useMenuStore } from '@/stores/menuStore'
import { getAutomationConfig, saveAutomationConfig, runAutomation } from '@/api/automation'
import { getConfigs } from '@/api/globalConfig'
import CodeEditor from '@/components/CodeEditor.vue'

// ────────── 路由 & Store ──────────
const route = useRoute()
const router = useRouter()
const menuStore = useMenuStore()
const menuId = computed(() => Number(route.params.menuId))

const menuName = computed(() => {
  for (const m of menuStore.menuList) {
    const child = m.children.find(c => c.id === menuId.value)
    if (child) return child.name
  }
  return `模块 #${menuId.value}`
})

// ────────── 自动化配置 ──────────
const configName = ref('默认配置')
const scriptCode = ref(DEFAULT_SCRIPT)
const selectedLoginId = ref<number | null>(null)
const saving = ref(false)

async function loadConfig() {
  try {
    const config = await getAutomationConfig(menuId.value)
    configName.value = config.name
    scriptCode.value = config.scriptCode
    selectedLoginId.value = config.loginConfigId
  } catch {
    // 未配置过，使用默认模板
    scriptCode.value = DEFAULT_SCRIPT
  }
}

async function handleSave() {
  saving.value = true
  try {
    await saveAutomationConfig(menuId.value, {
      name: configName.value,
      scriptCode: scriptCode.value,
      loginConfigId: selectedLoginId.value
    })
    ElMessage.success('配置已保存')
  } catch {
    ElMessage.error('保存失败')
  } finally {
    saving.value = false
  }
}

// ────────── 登录方案 ──────────
interface LoginScheme {
  id: string
  name: string
  cdpAddress: string
}

const loginSchemes = ref<LoginScheme[]>([])
const manualCdp = ref('')

const selectedScheme = computed(() =>
  loginSchemes.value.find(s => Number(s.id) === selectedLoginId.value) ?? null
)

const effectiveCdp = computed(() =>
  selectedScheme.value?.cdpAddress ?? manualCdp.value
)

async function loadLoginSchemes() {
  try {
    const configs = await getConfigs('login')
    const found = configs.find(c => c.key === 'schemes')
    if (found?.value) loginSchemes.value = JSON.parse(found.value)
  } catch {
    loginSchemes.value = []
  }
}

// ────────── 执行 ──────────
const running = ref(false)
const runLog = ref('')
const runSuccess = ref<boolean | null>(null)

async function handleRun() {
  const cdp = effectiveCdp.value.trim()
  if (!cdp) {
    ElMessage.warning('请选择登录方案或填写 CDP 地址')
    return
  }
  running.value = true
  runLog.value = ''
  runSuccess.value = null
  try {
    const result = await runAutomation(menuId.value, {
      scriptCode: scriptCode.value,
      cdpAddress: cdp,
      loginConfigId: selectedLoginId.value
    })
    runLog.value = result.output
    runSuccess.value = result.success
  } catch (err) {
    runLog.value = err instanceof Error ? err.message : '执行请求失败'
    runSuccess.value = false
  } finally {
    running.value = false
  }
}

// ────────── 初始化 ──────────
onMounted(async () => {
  await Promise.all([loadConfig(), loadLoginSchemes()])
})

// ────────── 默认脚本模板 ──────────
const DEFAULT_SCRIPT = `// 自动化脚本模板
// 可用变量：
//   Page  - Microsoft.Playwright.IPage，已连接到目标浏览器
//   Log() - 输出日志消息

// 示例：导航到指定页面并获取标题
await Page.GotoAsync("https://example.com");
var title = await Page.TitleAsync();
Log($"页面标题：{title}");

// 示例：查找元素并点击
// await Page.ClickAsync("#submit-btn");
// Log("已点击提交按钮");
`
</script>

<template>
  <div class="automation-page">

    <!-- ── 顶部工具栏 ── -->
    <div class="automation-header">
      <div class="header-left">
        <el-button text icon="ArrowLeft" @click="router.push(`/module/${menuId}`)">
          返回列表
        </el-button>
        <el-divider direction="vertical" />
        <span class="page-title">{{ menuName }} · 自动化配置</span>
      </div>
      <div class="header-right">
        <!-- 登录方案选择 -->
        <el-select
          v-model="selectedLoginId"
          placeholder="选择登录方案"
          size="small"
          clearable
          style="width: 160px;"
        >
          <el-option
            v-for="s in loginSchemes"
            :key="s.id"
            :label="s.name"
            :value="Number(s.id)"
          />
        </el-select>

        <!-- 无方案时手动输入 CDP -->
        <el-input
          v-if="!selectedLoginId"
          v-model="manualCdp"
          placeholder="CDP 地址（如 http://127.0.0.1:9222）"
          size="small"
          style="width: 260px;"
          clearable
        />
        <el-tag v-else type="success" size="small">
          {{ selectedScheme?.cdpAddress }}
        </el-tag>

        <el-button size="small" :loading="saving" icon="Check" @click="handleSave">保存</el-button>
        <el-button
          type="primary"
          size="small"
          :loading="running"
          icon="VideoPlay"
          @click="handleRun"
        >
          {{ running ? '执行中...' : '执行' }}
        </el-button>
      </div>
    </div>

    <!-- ── 主体区域 ── -->
    <div class="automation-body">

      <!-- 左侧：编辑器 -->
      <div class="editor-pane">
        <div class="pane-title">C# 脚本（Playwright）</div>
        <div class="editor-wrap">
          <CodeEditor
            v-model="scriptCode"
            language="csharp"
            height="100%"
          />
        </div>
      </div>

      <!-- 右侧：执行日志 -->
      <div class="log-pane">
        <div class="pane-title">
          执行日志
          <el-tag
            v-if="runSuccess !== null"
            :type="runSuccess ? 'success' : 'danger'"
            size="small"
            style="margin-left: 8px;"
          >
            {{ runSuccess ? '成功' : '失败' }}
          </el-tag>
          <el-button
            v-if="runLog"
            link
            size="small"
            style="margin-left: auto;"
            @click="runLog = ''; runSuccess = null"
          >
            清空
          </el-button>
        </div>
        <div class="log-content">
          <div v-if="running" class="log-running">
            <el-icon class="is-loading"><Loading /></el-icon>
            脚本执行中，请稍候...
          </div>
          <pre v-else-if="runLog" :class="['log-text', { 'log-error': runSuccess === false }]">{{ runLog }}</pre>
          <div v-else class="log-empty">点击「执行」运行脚本，日志将在此显示</div>
        </div>
      </div>

    </div>

  </div>
</template>

<style scoped>
.automation-page {
  height: 100vh;
  display: flex;
  flex-direction: column;
  background: #f0f2f5;
  overflow: hidden;
}

.automation-header {
  height: 52px;
  flex-shrink: 0;
  background: #fff;
  border-bottom: 1px solid #e4e7ed;
  display: flex;
  align-items: center;
  justify-content: space-between;
  padding: 0 16px;
  gap: 12px;
}

.header-left {
  display: flex;
  align-items: center;
  gap: 4px;
}

.header-right {
  display: flex;
  align-items: center;
  gap: 8px;
}

.page-title {
  font-size: 15px;
  font-weight: 600;
  color: #303133;
}

.automation-body {
  flex: 1;
  display: flex;
  gap: 12px;
  padding: 12px 16px;
  overflow: hidden;
}

.editor-pane {
  flex: 3;
  display: flex;
  flex-direction: column;
  background: #fff;
  border-radius: 4px;
  overflow: hidden;
}

.log-pane {
  flex: 2;
  display: flex;
  flex-direction: column;
  background: #fff;
  border-radius: 4px;
  overflow: hidden;
}

.pane-title {
  height: 38px;
  flex-shrink: 0;
  display: flex;
  align-items: center;
  padding: 0 12px;
  font-size: 13px;
  font-weight: 600;
  color: #606266;
  border-bottom: 1px solid #f0f0f0;
  background: #fafafa;
}

.editor-wrap {
  flex: 1;
  overflow: hidden;
}

.log-content {
  flex: 1;
  overflow: hidden;
  display: flex;
  flex-direction: column;
}

.log-running {
  display: flex;
  align-items: center;
  justify-content: center;
  gap: 8px;
  height: 100%;
  color: #909399;
  font-size: 13px;
}

.log-text {
  flex: 1;
  margin: 0;
  padding: 12px;
  font-family: 'Consolas', 'Courier New', monospace;
  font-size: 12px;
  line-height: 1.6;
  color: #303133;
  background: #1e1e1e;
  color: #d4d4d4;
  overflow: auto;
  white-space: pre-wrap;
  word-break: break-all;
}

.log-text.log-error {
  color: #f48771;
}

.log-empty {
  flex: 1;
  display: flex;
  align-items: center;
  justify-content: center;
  font-size: 13px;
  color: #c0c4cc;
}
</style>
