<!-- 统一导入对话框：选文件 + 导入模式选择 + 映射方案 + 预览触发 -->
<script setup lang="ts">
import { ref, computed, watch } from 'vue'
import { ElMessage } from 'element-plus'
import { useModuleStore } from '@/stores/moduleStore'
import { previewImport, parseExcelHeaders, previewImportWithMapping } from '@/api/data'
import { withLoading } from '@/utils/loading'
import ImportMappingDialog from './ImportMappingDialog.vue'
import type { FormField, MappingItem, ImportMappingConfig } from '@/types'
import type { PreviewRow } from '@/api/data'

const props = defineProps<{
  visible: boolean
  menuId: number
  fields: FormField[]
}>()

const emit = defineEmits<{
  'update:visible': [val: boolean]
  /** 预览结果，由 ModuleView 接收后打开 ImportPreviewDialog */
  preview: [rows: PreviewRow[], batchId: string]
}>()

const store = useModuleStore()

// ────────── 文件选择 ──────────
const selectedFile = ref<File | null>(null)
const fileInputRef = ref<HTMLInputElement>()

function triggerFileSelect() {
  fileInputRef.value?.click()
}

function handleFileChange(e: Event) {
  const file = (e.target as HTMLInputElement).files?.[0]
  if (file) selectedFile.value = file
  ;(e.target as HTMLInputElement).value = ''
}

function clearFile() {
  selectedFile.value = null
}

const fileSize = computed(() => {
  if (!selectedFile.value) return ''
  const kb = selectedFile.value.size / 1024
  return kb > 1024 ? `${(kb / 1024).toFixed(1)} MB` : `${kb.toFixed(0)} KB`
})

// ────────── 导入模式 ──────────
const useMappingEnabled = ref(false)
const selectedMappingConfigId = ref<number | null>(null)

// 映射方案列表
const mappingConfigs = computed(() => store.importMappingConfigs)

// 当前选中方案的映射摘要
const selectedMappingItems = computed<MappingItem[]>(() => {
  if (!selectedMappingConfigId.value) return []
  const cfg = mappingConfigs.value.find(c => c.id === selectedMappingConfigId.value)
  if (!cfg) return []
  try { return JSON.parse(cfg.mappings) } catch { return [] }
})

// ────────── 映射连线子对话框 ──────────
const mappingDialogVisible = ref(false)
const importExcelHeaders = ref<string[]>([])
const parsingHeaders = ref(false)

/** 解析表头（编辑/新建连线前需要） */
async function ensureHeaders() {
  if (importExcelHeaders.value.length > 0 || !selectedFile.value) return
  parsingHeaders.value = true
  try {
    const result = await parseExcelHeaders(props.menuId, selectedFile.value)
    importExcelHeaders.value = result.headers
  } finally {
    parsingHeaders.value = false
  }
}

async function openEditMapping() {
  await ensureHeaders()
  mappingDialogVisible.value = true
}

async function openNewMapping() {
  await ensureHeaders()
  // 清空选中方案，打开空白连线
  selectedMappingConfigId.value = null
  mappingDialogVisible.value = true
}

/** 从 ImportMappingDialog 返回后刷新方案列表 */
function handleMappingApply(_mappings: MappingItem[]) {
  // 连线对话框内部已保存方案，刷新列表
  store.fetchImportMappingConfigs(props.menuId)
  mappingDialogVisible.value = false
}

// ────────── 对话框打开时恢复偏好 ──────────
watch(() => props.visible, (val) => {
  if (val) {
    // 恢复偏好
    const pref = store.importPreference
    useMappingEnabled.value = pref?.useMappingEnabled ?? false
    selectedMappingConfigId.value = pref?.lastMappingConfigId ?? null
    // 重置文件和表头
    selectedFile.value = null
    importExcelHeaders.value = []
  }
})

// ────────── 下一步：预览 ──────────
const stepping = ref(false)

/** 生成批次号：BATCH-YYYYMMDD-HHmmss */
function generateBatchId(): string {
  const now = new Date()
  const pad = (n: number) => String(n).padStart(2, '0')
  const d = `${now.getFullYear()}${pad(now.getMonth() + 1)}${pad(now.getDate())}`
  const t = `${pad(now.getHours())}${pad(now.getMinutes())}${pad(now.getSeconds())}`
  return `BATCH-${d}-${t}`
}

async function handleNext() {
  if (!selectedFile.value) {
    ElMessage.warning('请先选择文件')
    return
  }

  stepping.value = true
  try {
    let rows: PreviewRow[]

    if (useMappingEnabled.value && selectedMappingConfigId.value) {
      // 映射模式
      const mappings = selectedMappingItems.value
      if (mappings.length === 0) {
        ElMessage.warning('当前方案无映射规则，请先编辑连线')
        return
      }
      const result = await withLoading(
        () => previewImportWithMapping(props.menuId, selectedFile.value!, mappings),
        '按映射规则解析中…'
      )
      rows = result.rows
    } else {
      // 直接匹配模式
      const result = await withLoading(
        () => previewImport(props.menuId, selectedFile.value!),
        '解析文件中…'
      )
      rows = result.rows
    }

    // 保存偏好
    await store.saveImportPref(props.menuId, useMappingEnabled.value, selectedMappingConfigId.value)

    // 发出预览事件
    const batchId = generateBatchId()
    emit('preview', rows, batchId)
    emit('update:visible', false)
  } catch (err) {
    ElMessage.error(err instanceof Error ? err.message : '解析失败')
  } finally {
    stepping.value = false
  }
}
</script>

<template>
  <el-dialog
    :model-value="visible"
    title="导入 Excel"
    width="520px"
    @update:model-value="$emit('update:visible', $event)"
    destroy-on-close
  >
    <!-- 隐藏文件输入 -->
    <input
      ref="fileInputRef"
      type="file"
      accept=".xlsx,.xls"
      style="display: none"
      @change="handleFileChange"
    />

    <!-- 步骤1：文件选择 -->
    <div class="import-section">
      <div class="section-title">
        <span class="step-badge">1</span>
        <span>选择文件</span>
      </div>

      <!-- 未选文件 -->
      <div v-if="!selectedFile" class="file-upload-area" @click="triggerFileSelect">
        <el-icon :size="32" color="#c0c4cc"><Upload /></el-icon>
        <div style="margin-top: 8px; color: #606266;">点击选择 Excel 文件</div>
        <div style="font-size: 12px; color: #c0c4cc;">支持 .xlsx / .xls</div>
      </div>

      <!-- 已选文件 -->
      <div v-else class="file-selected">
        <div class="file-info">
          <el-icon color="#67c23a" :size="18"><Document /></el-icon>
          <span>{{ selectedFile.name }}</span>
          <span style="color: #909399; font-size: 12px;">({{ fileSize }})</span>
        </div>
        <el-link type="primary" :underline="false" @click="triggerFileSelect">重新选择</el-link>
      </div>
    </div>

    <!-- 步骤2：导入模式 -->
    <div class="import-section">
      <div class="section-title">
        <span class="step-badge">2</span>
        <span>导入方式</span>
      </div>

      <div class="mapping-toggle">
        <el-switch v-model="useMappingEnabled" />
        <span class="toggle-label">启用字段映射</span>
        <span class="toggle-desc">{{ useMappingEnabled ? '使用映射方案转换列' : '按列名直接匹配表单字段' }}</span>
      </div>

      <!-- 映射方案选择区 -->
      <div v-if="useMappingEnabled" class="mapping-config-area">
        <div class="config-row">
          <el-select
            v-model="selectedMappingConfigId"
            placeholder="选择映射方案"
            clearable
            filterable
            style="flex: 1;"
          >
            <el-option
              v-for="cfg in mappingConfigs"
              :key="cfg.id"
              :label="cfg.name"
              :value="cfg.id"
            />
          </el-select>
          <el-button
            :disabled="!selectedFile || !selectedMappingConfigId"
            :loading="parsingHeaders"
            @click="openEditMapping"
          >
            编辑连线
          </el-button>
          <el-button
            :loading="parsingHeaders"
            @click="openNewMapping"
          >
            新建方案
          </el-button>
        </div>

        <!-- 映射摘要预览 -->
        <div v-if="selectedMappingItems.length > 0" class="mapping-summary">
          <div
            v-for="item in selectedMappingItems"
            :key="item.sourceColumn + item.targetField"
            class="mapping-line"
          >
            <span class="source">{{ item.sourceColumn }}</span>
            <span class="arrow">→</span>
            <span class="target">{{ item.targetField }}</span>
            <el-tag v-if="item.transformScript" size="small" type="warning" style="margin-left: 6px;">
              fx
            </el-tag>
          </div>
          <div style="color: #c0c4cc; font-size: 12px; margin-top: 4px;">
            共 {{ selectedMappingItems.length }} 个映射
          </div>
        </div>

        <div v-else-if="selectedMappingConfigId" class="mapping-summary empty">
          该方案暂无映射规则
        </div>
      </div>
    </div>

    <template #footer>
      <el-button @click="$emit('update:visible', false)">取消</el-button>
      <el-button
        type="primary"
        :loading="stepping"
        :disabled="!selectedFile"
        @click="handleNext"
      >
        下一步：预览导入
      </el-button>
    </template>

    <!-- 映射连线子对话框 -->
    <ImportMappingDialog
      v-model:visible="mappingDialogVisible"
      :menu-id="menuId"
      :headers="importExcelHeaders"
      :fields="fields"
      @apply="handleMappingApply"
    />
  </el-dialog>
</template>

<style scoped>
.import-section {
  margin-bottom: 20px;
}

.section-title {
  display: flex;
  align-items: center;
  gap: 8px;
  margin-bottom: 10px;
  font-size: 14px;
  font-weight: 600;
  color: #303133;
}

.step-badge {
  background: #409eff;
  color: #fff;
  border-radius: 50%;
  width: 22px;
  height: 22px;
  display: inline-flex;
  align-items: center;
  justify-content: center;
  font-size: 12px;
  flex-shrink: 0;
}

.file-upload-area {
  border: 2px dashed #dcdfe6;
  border-radius: 6px;
  padding: 24px;
  text-align: center;
  cursor: pointer;
  transition: border-color 0.3s;
}
.file-upload-area:hover {
  border-color: #409eff;
}

.file-selected {
  border: 1px solid #67c23a;
  background: #f0f9eb;
  border-radius: 6px;
  padding: 10px 14px;
  display: flex;
  align-items: center;
  justify-content: space-between;
}
.file-info {
  display: flex;
  align-items: center;
  gap: 6px;
  color: #303133;
  font-size: 13px;
}

.mapping-toggle {
  display: flex;
  align-items: center;
  gap: 10px;
  padding: 10px 14px;
  background: #f5f7fa;
  border-radius: 6px;
}
.toggle-label {
  font-size: 13px;
  font-weight: 500;
  color: #303133;
}
.toggle-desc {
  font-size: 12px;
  color: #909399;
}

.mapping-config-area {
  margin-top: 12px;
  padding: 12px 14px;
  background: #fafafa;
  border: 1px solid #ebeef5;
  border-radius: 6px;
}

.config-row {
  display: flex;
  gap: 8px;
  align-items: center;
}

.mapping-summary {
  margin-top: 10px;
  padding: 8px 10px;
  background: #fff;
  border: 1px solid #ebeef5;
  border-radius: 4px;
  max-height: 160px;
  overflow-y: auto;
}
.mapping-summary.empty {
  color: #c0c4cc;
  font-size: 12px;
  text-align: center;
  padding: 12px;
}

.mapping-line {
  display: flex;
  align-items: center;
  gap: 6px;
  padding: 3px 0;
  font-size: 12px;
}
.mapping-line .source {
  color: #409eff;
  font-weight: 500;
}
.mapping-line .arrow {
  color: #c0c4cc;
}
.mapping-line .target {
  color: #67c23a;
  font-weight: 500;
}
</style>
