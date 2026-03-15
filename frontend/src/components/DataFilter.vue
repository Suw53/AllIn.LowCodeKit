<!-- 高级筛选面板：字段级条件配置、保存/加载方案 -->
<script setup lang="ts">
import { ref, watch } from 'vue'
import { ElMessage, ElMessageBox } from 'element-plus'
import type { FormField, FilterCondition, FilterScheme } from '@/types'

// ────────── Props / Emits ──────────
const props = defineProps<{
  visible: boolean
  fields: FormField[]
  modelValue: FilterCondition[]
  schemes: FilterScheme[]
}>()

const emit = defineEmits<{
  'update:visible': [v: boolean]
  'update:modelValue': [v: FilterCondition[]]
  apply: []
  'save-scheme': [name: string]
  'delete-scheme': [id: number]
  'load-scheme': [scheme: FilterScheme]
}>()

// ────────── 本地筛选条件（深拷贝，避免影响父组件直到点Apply） ──────────
const localFilters = ref<FilterCondition[]>([])

watch(
  () => props.visible,
  (v) => { if (v) localFilters.value = props.modelValue.map(f => ({ ...f })) },
  { immediate: true }
)

// ────────── 操作符选项 ──────────
const opOptions = [
  { label: '包含', value: 'contains' },
  { label: '等于', value: 'eq' }
]

function addCondition() {
  localFilters.value.push({ field: '', op: 'contains', value: '' })
}

function removeCondition(idx: number) {
  localFilters.value.splice(idx, 1)
}

function handleApply() {
  // 过滤掉未填写的条件
  const valid = localFilters.value.filter(f => f.field && f.value)
  emit('update:modelValue', valid)
  emit('apply')
  emit('update:visible', false)
}

function handleReset() {
  localFilters.value = []
  emit('update:modelValue', [])
  emit('apply')
  emit('update:visible', false)
}

// ────────── 方案条件预览 ──────────
function parseSchemeConditions(scheme: FilterScheme): string[] {
  try {
    const conds = JSON.parse(scheme.config) as FilterCondition[]
    return conds.map(c => {
      const fieldLabel = props.fields.find(f => f.fieldName === c.field)?.label ?? c.field
      const opLabel = c.op === 'eq' ? '等于' : '包含'
      return `${fieldLabel} ${opLabel} "${c.value}"`
    })
  } catch {
    return []
  }
}

// ────────── 保存方案 ──────────
const schemeName = ref('')

async function handleSaveScheme() {
  if (!schemeName.value.trim()) {
    ElMessage.warning('请输入方案名称')
    return
  }
  emit('save-scheme', schemeName.value.trim())
  schemeName.value = ''
}

async function handleDeleteScheme(id: number) {
  await ElMessageBox.confirm('确定删除该筛选方案？', '提示', {
    confirmButtonText: '删除',
    cancelButtonText: '取消',
    type: 'warning'
  })
  emit('delete-scheme', id)
}

function handleLoadScheme(scheme: FilterScheme) {
  // 解析方案条件加载到编辑区，同时立即应用
  try {
    localFilters.value = JSON.parse(scheme.config) as FilterCondition[]
  } catch {
    localFilters.value = []
  }
  // 立即触发应用，让用户看到筛选结果生效
  const valid = localFilters.value.filter(f => f.field && f.value)
  emit('update:modelValue', valid)
  emit('load-scheme', scheme)
  emit('apply')
  emit('update:visible', false)
}
</script>

<template>
  <el-drawer
    :model-value="visible"
    title="高级筛选"
    size="420px"
    direction="rtl"
    @update:model-value="emit('update:visible', $event)"
  >
    <div class="filter-body">

      <!-- ── 已保存方案 ── -->
      <div v-if="schemes.length > 0" class="section">
        <div class="section-title">已保存方案</div>
        <div class="scheme-list">
          <div v-for="s in schemes" :key="s.id" class="scheme-item">
            <el-tooltip placement="right" :show-after="300">
              <template #content>
                <div v-for="(c, i) in parseSchemeConditions(s)" :key="i" style="font-size:12px;">
                  {{ c }}
                </div>
              </template>
              <span class="scheme-name" @click="handleLoadScheme(s)">{{ s.name }}</span>
            </el-tooltip>
            <el-button link type="danger" size="small" @click="handleDeleteScheme(s.id)">删除</el-button>
          </div>
        </div>
      </div>

      <el-divider v-if="schemes.length > 0" />

      <!-- ── 筛选条件 ── -->
      <div class="section">
        <div class="section-header">
          <span class="section-title">筛选条件</span>
          <el-button size="small" type="primary" link @click="addCondition">+ 添加条件</el-button>
        </div>

        <div v-if="localFilters.length === 0" class="empty-hint">暂无筛选条件，点击添加</div>

        <div v-for="(condition, idx) in localFilters" :key="idx" class="condition-row">
          <!-- 字段选择 -->
          <el-select
            v-model="condition.field"
            placeholder="选择字段"
            size="small"
            style="width: 120px; flex-shrink: 0;"
          >
            <el-option
              v-for="f in fields"
              :key="f.fieldName"
              :label="f.label"
              :value="f.fieldName"
            />
          </el-select>

          <!-- 操作符 -->
          <el-select
            v-model="condition.op"
            size="small"
            style="width: 80px; flex-shrink: 0;"
          >
            <el-option
              v-for="op in opOptions"
              :key="op.value"
              :label="op.label"
              :value="op.value"
            />
          </el-select>

          <!-- 值输入 -->
          <el-input
            v-model="condition.value"
            placeholder="筛选值"
            size="small"
            style="flex: 1;"
          />

          <el-button link type="danger" size="small" @click="removeCondition(idx)">
            <el-icon><Delete /></el-icon>
          </el-button>
        </div>
      </div>

      <el-divider />

      <!-- ── 保存为方案 ── -->
      <div class="section">
        <div class="section-title">保存为方案</div>
        <div class="save-row">
          <el-input
            v-model="schemeName"
            placeholder="方案名称"
            size="small"
            style="flex: 1;"
          />
          <el-button size="small" @click="handleSaveScheme">保存</el-button>
        </div>
      </div>
    </div>

    <template #footer>
      <el-button @click="handleReset">清空重置</el-button>
      <el-button type="primary" @click="handleApply">应用筛选</el-button>
    </template>
  </el-drawer>
</template>

<style scoped>
.filter-body {
  padding: 0 4px;
}

.section {
  margin-bottom: 4px;
}

.section-header {
  display: flex;
  align-items: center;
  justify-content: space-between;
  margin-bottom: 10px;
}

.section-title {
  font-size: 13px;
  font-weight: 600;
  color: #303133;
  margin-bottom: 10px;
  display: block;
}

.scheme-list {
  display: flex;
  flex-direction: column;
  gap: 4px;
}

.scheme-item {
  display: flex;
  align-items: center;
  justify-content: space-between;
  padding: 6px 10px;
  background: #f5f7fa;
  border-radius: 4px;
}

.scheme-name {
  font-size: 13px;
  color: #409eff;
  cursor: pointer;
}

.scheme-name:hover {
  text-decoration: underline;
}

.empty-hint {
  font-size: 13px;
  color: #c0c4cc;
  text-align: center;
  padding: 12px 0;
}

.condition-row {
  display: flex;
  align-items: center;
  gap: 6px;
  margin-bottom: 8px;
}

.save-row {
  display: flex;
  gap: 8px;
  align-items: center;
}
</style>
