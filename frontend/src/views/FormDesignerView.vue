<!-- 表单设计器页面：可视化设计（拖拽字段）+ 代码模式（Monaco C#），双向同步必填校验逻辑 -->
<script setup lang="ts">
import { ref, computed, watch, nextTick, onMounted } from 'vue'
import { useRoute, useRouter } from 'vue-router'
import draggable from 'vuedraggable'
import { ElMessage, ElMessageBox } from 'element-plus'
import { useMenuStore } from '@/stores/menuStore'
import {
  useFormTemplateStore,
  buildDefaultCode,
  syncRequiredToCode,
  parseRequiredFromCode
} from '@/stores/formTemplateStore'
import CodeEditor from '@/components/CodeEditor.vue'
import FormFieldItem from '@/components/FormFieldItem.vue'
import type { FormField } from '@/types'

// ────────── 路由 & 基础数据 ──────────
const route = useRoute()
const router = useRouter()
const menuStore = useMenuStore()
const store = useFormTemplateStore()

const menuId = computed(() => Number(route.params.menuId))
const menuName = computed(() => {
  for (const m of menuStore.menuList) {
    const child = m.children.find(c => c.id === menuId.value)
    if (child) return child.name
  }
  return `菜单 #${menuId.value}`
})

// ────────── 本地状态 ──────────
/** 模板名称（本地编辑态） */
const templateName = ref('')
/** 当前编辑的字段列表（本地副本，避免直接改 store） */
const fields = ref<FormField[]>([])
/** 代码编辑器的内容（本地编辑态） */
const codeLogic = ref(buildDefaultCode())
/** 当前激活的 Tab */
const activeTab = ref<'visual' | 'code'>('visual')
/** 用于自动展开新添加字段的 ref map */
const fieldItemRefs = ref<(InstanceType<typeof FormFieldItem> | null)[]>([])

// ────────── 模板加载 ──────────
onMounted(async () => {
  await store.loadByMenu(menuId.value)
  syncFromStore()
})

watch(() => store.template, () => syncFromStore())

function syncFromStore() {
  const t = store.template
  if (t) {
    templateName.value = t.name
    fields.value = t.fields.map(f => ({ ...f }))
    codeLogic.value = t.codeLogic || buildDefaultCode()
  } else {
    templateName.value = menuName.value
    fields.value = []
    codeLogic.value = buildDefaultCode()
  }
}

// ────────── Tab 切换 & 双向同步 ──────────
function handleTabChange(tab: 'visual' | 'code') {
  if (tab === 'code' && activeTab.value === 'visual') {
    // Visual → Code：重建 AUTO 必填区块
    codeLogic.value = syncRequiredToCode(codeLogic.value, fields.value)
  }
  if (tab === 'visual' && activeTab.value === 'code') {
    // Code → Visual：解析 AUTO 区块更新 isRequired
    const required = parseRequiredFromCode(codeLogic.value)
    fields.value = fields.value.map(f => ({
      ...f,
      isRequired: required.has(f.fieldName)
    }))
  }
  activeTab.value = tab
}

// ────────── 字段操作 ──────────

/** 拖拽时的临时 id 计数器 */
let tempIdCounter = -1

/** 从字段面板拖拽时 clone 出新字段对象 */
function cloneFieldType(original: { type: 'Text' | 'Select'; label: string }) {
  return {
    id: tempIdCounter--,
    templateId: store.template?.id ?? 0,
    fieldName: '',
    label: original.label,
    fieldType: original.type,
    options: original.type === 'Select' ? '[]' : undefined,
    isRequired: false,
    remark: '',
    columnOrder: fields.value.length + 1,
    sort: fields.value.length
  } as FormField
}

/** 从画布拖拽区 @add 事件：新字段拖入后自动展开属性面板 */
async function onFieldAdded(evt: { newIndex: number }) {
  await nextTick()
  fieldItemRefs.value[evt.newIndex]?.openForEdit()
}

function addField(type: 'Text' | 'Select') {
  const label = type === 'Text' ? '文本框' : '下拉框'
  const newField = cloneFieldType({ type, label })
  fields.value.push(newField)
  nextTick(() => {
    const idx = fields.value.length - 1
    fieldItemRefs.value[idx]?.openForEdit()
  })
}

function updateField(index: number, updated: FormField) {
  fields.value[index] = updated
  // 字段 isRequired 变化时同步代码 AUTO 区块
  codeLogic.value = syncRequiredToCode(codeLogic.value, fields.value)
}

function deleteField(index: number) {
  fields.value.splice(index, 1)
  codeLogic.value = syncRequiredToCode(codeLogic.value, fields.value)
}

// ────────── 保存 ──────────
async function save() {
  if (!templateName.value.trim()) {
    ElMessage.warning('请输入模板名称')
    return
  }
  // 校验字段基本属性
  for (const f of fields.value) {
    if (!f.fieldName.trim() || !f.label.trim()) {
      ElMessage.warning(`第 ${fields.value.indexOf(f) + 1} 个字段的"字段名"和"标签名"不能为空`)
      return
    }
  }
  const latestCode = syncRequiredToCode(codeLogic.value, fields.value)
  const fieldsSnapshot = fields.value.map(f => ({ ...f }))
  const nameSnapshot = templateName.value.trim()

  try {
    // upsert：单次请求，后端自动处理创建或更新
    await store.save(menuId.value, nameSnapshot, fieldsSnapshot, latestCode)
    codeLogic.value = latestCode
    ElMessage.success('保存成功')
  } catch (e: unknown) {
    ElMessage.error(e instanceof Error ? e.message : '保存失败')
  }
}

// ────────── 导出 ──────────
async function doExport() {
  if (!store.template) {
    // 未保存过，先导出本地状态
    const data = {
      name: templateName.value,
      codeLogic: syncRequiredToCode(codeLogic.value, fields.value),
      fields: fields.value
    }
    downloadJson(data, `${templateName.value || 'form'}-template.json`)
    return
  }
  try {
    const data = await store.doExport()
    if (data) downloadJson(data, `${data.name}-template.json`)
  } catch (e: unknown) {
    ElMessage.error(e instanceof Error ? e.message : '导出失败')
  }
}

function downloadJson(data: unknown, filename: string) {
  const blob = new Blob([JSON.stringify(data, null, 2)], { type: 'application/json' })
  const url = URL.createObjectURL(blob)
  const a = document.createElement('a')
  a.href = url
  a.download = filename
  a.click()
  URL.revokeObjectURL(url)
}

// ────────── 导入 ──────────
const importInputRef = ref<HTMLInputElement>()

function triggerImport() {
  importInputRef.value?.click()
}

async function handleImportFile(e: Event) {
  const file = (e.target as HTMLInputElement).files?.[0]
  if (!file) return
  ;(e.target as HTMLInputElement).value = ''

  try {
    const text = await file.text()
    const data = JSON.parse(text)

    if (!data.name || !Array.isArray(data.fields)) {
      ElMessage.error('JSON 格式不正确，缺少 name 或 fields')
      return
    }

    await ElMessageBox.confirm(
      `导入将覆盖当前菜单「${menuName.value}」的表单设计，确定继续？`,
      '导入确认',
      { confirmButtonText: '确定', cancelButtonText: '取消', type: 'warning' }
    )

    await store.doImport(menuId.value, data)
    syncFromStore()
    ElMessage.success('导入成功')
  } catch (err) {
    if (err !== 'cancel') {
      ElMessage.error(err instanceof Error ? err.message : '导入失败')
    }
  }
}

// ────────── 字段面板数据（拖拽源） ──────────
const fieldTypePalette = [
  { type: 'Text' as const, label: '文本框', icon: 'EditPen' },
  { type: 'Select' as const, label: '下拉框', icon: 'ArrowDown' }
]
</script>

<template>
  <div class="designer-page">

    <!-- ── 顶部工具栏 ── -->
    <div class="designer-header">
      <div class="header-left">
        <el-button link icon="ArrowLeft" @click="router.back()">返回</el-button>
        <span class="page-title">表单设计器</span>
        <el-tag type="info" size="small">{{ menuName }}</el-tag>
      </div>
      <div class="header-center">
        <el-input
          v-model="templateName"
          placeholder="模板名称"
          size="small"
          style="width: 220px;"
        />
      </div>
      <div class="header-right">
        <el-button size="small" icon="Download" @click="doExport">导出</el-button>
        <el-button size="small" icon="Upload" @click="triggerImport">导入</el-button>
        <el-button
          size="small"
          type="primary"
          icon="Check"
          :loading="store.saving"
          @click="save"
        >
          保存
        </el-button>
      </div>
    </div>

    <!-- 隐藏文件输入（导入） -->
    <input
      ref="importInputRef"
      type="file"
      accept=".json"
      style="display:none"
      @change="handleImportFile"
    />

    <!-- ── Tab 切换 ── -->
    <div class="tab-bar">
      <div
        class="tab-item"
        :class="{ active: activeTab === 'visual' }"
        @click="handleTabChange('visual')"
      >
        <el-icon><Grid /></el-icon> 可视化设计
      </div>
      <div
        class="tab-item"
        :class="{ active: activeTab === 'code' }"
        @click="handleTabChange('code')"
      >
        <el-icon><Monitor /></el-icon> 代码模式
      </div>
    </div>

    <!-- ── 主体内容 ── -->
    <div class="designer-body">

      <!-- 加载骨架 -->
      <div v-if="store.loading" style="padding: 24px;">
        <el-skeleton :rows="5" animated />
      </div>

      <!-- ── 可视化设计 Tab ── -->
      <template v-else-if="activeTab === 'visual'">
        <div class="visual-layout">

          <!-- 左侧字段类型面板 -->
          <div class="palette">
            <div class="palette-title">字段类型</div>
            <!-- 拖拽源：clone 模式，不改变原列表 -->
            <draggable
              :list="fieldTypePalette"
              :group="{ name: 'fields', pull: 'clone', put: false }"
              :sort="false"
              :clone="cloneFieldType"
              item-key="type"
              class="palette-list"
            >
              <template #item="{ element }">
                <div class="palette-item">
                  <el-icon><component :is="element.icon" /></el-icon>
                  {{ element.label }}
                </div>
              </template>
            </draggable>
            <div class="palette-divider" />
            <div class="palette-hint">拖拽或点击添加</div>
            <el-button
              size="small"
              icon="EditPen"
              style="width:100%; margin-bottom:6px;"
              @click="addField('Text')"
            >
              文本框
            </el-button>
            <el-button
              size="small"
              icon="ArrowDown"
              style="width:100%;"
              @click="addField('Select')"
            >
              下拉框
            </el-button>
          </div>

          <!-- 右侧字段画布 -->
          <div class="canvas">
            <div v-if="fields.length === 0" class="canvas-empty">
              <el-empty description="从左侧拖入字段，或点击按钮快速添加" :image-size="80" />
            </div>

            <draggable
              v-else
              v-model="fields"
              :group="{ name: 'fields', pull: false, put: true }"
              item-key="id"
              handle=".drag-handle"
              :animation="150"
              class="field-list"
              @add="onFieldAdded"
            >
              <template #item="{ element, index }">
                <FormFieldItem
                  :ref="(el) => { fieldItemRefs[index] = el as InstanceType<typeof FormFieldItem> }"
                  :model-value="element"
                  :index="index"
                  @update:model-value="updateField(index, $event)"
                  @delete="deleteField(index)"
                />
              </template>
            </draggable>

            <!-- 空画布也需要支持拖入 -->
            <draggable
              v-if="fields.length === 0"
              v-model="fields"
              :group="{ name: 'fields', pull: false, put: true }"
              item-key="id"
              class="field-list-empty-drop"
              @add="onFieldAdded"
            >
              <template #item="{ }"><span /></template>
            </draggable>
          </div>
        </div>
      </template>

      <!-- ── 代码模式 Tab ── -->
      <template v-else>
        <div class="code-layout">
          <div class="code-hint">
            <el-alert
              type="info"
              :closable="false"
              show-icon
            >
              <template #title>
                <code>AUTO:REQUIRED</code> 区块由必填配置自动生成，请勿手动修改该区块内容。切换至可视化设计时将自动解析必填状态。
              </template>
            </el-alert>
          </div>
          <CodeEditor
            v-model="codeLogic"
            language="csharp"
            height="100%"
            class="code-editor-wrap"
          />
        </div>
      </template>

    </div>
  </div>
</template>

<style scoped>
.designer-page {
  height: 100vh;
  display: flex;
  flex-direction: column;
  background: #f0f2f5;
  overflow: hidden;
}

/* ── 顶部工具栏 ── */
.designer-header {
  height: 52px;
  flex-shrink: 0;
  background: #fff;
  border-bottom: 1px solid #e4e7ed;
  display: flex;
  align-items: center;
  padding: 0 16px;
  gap: 12px;
}

.header-left {
  display: flex;
  align-items: center;
  gap: 8px;
  flex: 1;
}

.page-title {
  font-size: 15px;
  font-weight: 600;
  color: #303133;
}

.header-center {
  flex-shrink: 0;
}

.header-right {
  display: flex;
  align-items: center;
  gap: 8px;
  flex: 1;
  justify-content: flex-end;
}

/* ── Tab 栏 ── */
.tab-bar {
  display: flex;
  height: 40px;
  flex-shrink: 0;
  background: #fff;
  border-bottom: 1px solid #e4e7ed;
  padding: 0 16px;
  gap: 0;
}

.tab-item {
  display: flex;
  align-items: center;
  gap: 5px;
  padding: 0 16px;
  font-size: 13px;
  color: #606266;
  cursor: pointer;
  border-bottom: 2px solid transparent;
  transition: color 0.15s, border-color 0.15s;
  margin-bottom: -1px;
}

.tab-item:hover {
  color: #409eff;
}

.tab-item.active {
  color: #409eff;
  border-bottom-color: #409eff;
  font-weight: 500;
}

/* ── 主体 ── */
.designer-body {
  flex: 1;
  overflow: hidden;
  display: flex;
  flex-direction: column;
}

/* ── 可视化布局 ── */
.visual-layout {
  display: flex;
  height: 100%;
  overflow: hidden;
  gap: 0;
}

/* 左侧字段面板 */
.palette {
  width: 140px;
  flex-shrink: 0;
  background: #fff;
  border-right: 1px solid #e4e7ed;
  padding: 12px 10px;
  display: flex;
  flex-direction: column;
  gap: 6px;
  overflow-y: auto;
}

.palette-title {
  font-size: 12px;
  font-weight: 600;
  color: #909399;
  text-transform: uppercase;
  letter-spacing: 0.5px;
  margin-bottom: 4px;
}

.palette-list {
  display: flex;
  flex-direction: column;
  gap: 6px;
  min-height: 10px;
}

.palette-item {
  display: flex;
  align-items: center;
  gap: 6px;
  padding: 7px 10px;
  border: 1px dashed #c0c4cc;
  border-radius: 4px;
  font-size: 13px;
  color: #606266;
  cursor: grab;
  background: #fafafa;
  transition: border-color 0.15s, color 0.15s, background 0.15s;
  user-select: none;
}

.palette-item:hover {
  border-color: #409eff;
  color: #409eff;
  background: #ecf5ff;
}

.palette-divider {
  height: 1px;
  background: #e4e7ed;
  margin: 4px 0;
}

.palette-hint {
  font-size: 11px;
  color: #c0c4cc;
  text-align: center;
}

/* 右侧字段画布 */
.canvas {
  flex: 1;
  overflow-y: auto;
  padding: 16px;
  position: relative;
}

.canvas-empty {
  display: flex;
  align-items: center;
  justify-content: center;
  height: 100%;
}

.field-list {
  display: flex;
  flex-direction: column;
  gap: 8px;
  min-height: 40px;
}

.field-list-empty-drop {
  position: absolute;
  inset: 0;
  opacity: 0;
}

/* ── 代码模式布局 ── */
.code-layout {
  display: flex;
  flex-direction: column;
  height: 100%;
  padding: 12px 16px 16px;
  gap: 10px;
  overflow: hidden;
}

.code-hint {
  flex-shrink: 0;
}

.code-editor-wrap {
  flex: 1;
  min-height: 0;
}
</style>
