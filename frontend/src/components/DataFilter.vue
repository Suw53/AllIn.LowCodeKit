<!-- 高级筛选面板：字段级条件配置、保存/加载/更新方案 -->
<script setup lang="ts">
import { ref, computed, watch } from 'vue'
import { ElMessage, ElMessageBox } from 'element-plus'
import type { FormField, FilterCondition, FilterScheme } from '@/types'

// ────────── Props / Emits ──────────
const props = defineProps<{
  visible: boolean
  fields: FormField[]
  modelValue: FilterCondition[]
  schemes: FilterScheme[]
  /** 保存方案 loading（由父组件控制） */
  schemeSaving?: boolean
  /** 更新方案 loading（由父组件控制） */
  schemeUpdating?: boolean
  /** 正在删除的方案 ID（由父组件控制，用于逐行 loading） */
  deletingSchemeId?: number | null
}>()

const emit = defineEmits<{
  'update:visible': [v: boolean]
  'update:modelValue': [v: FilterCondition[]]
  apply: []
  /** 新建方案：传入名称 + 当前编辑区条件（只要有字段即可保存） */
  'save-scheme': [name: string, conditions: FilterCondition[]]
  /** 更新已有方案：传入 id + 新名称 + 当前编辑区条件 */
  'update-scheme': [id: number, name: string, conditions: FilterCondition[]]
  'delete-scheme': [id: number]
  'load-scheme': [scheme: FilterScheme]
}>()

// ────────── 本地筛选条件（深拷贝，避免影响父组件直到点Apply） ──────────
const localFilters = ref<FilterCondition[]>([])

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
  // 应用时只发送有字段且有值的条件
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

// ────────── 方案条件预览（tooltip 用） ──────────
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

// ────────── 当前已加载的方案（用于高亮 + 更新） ──────────
const loadedSchemeId = ref<number | null>(null)
/** 更新模式下方案名的编辑副本 */
const updatingName = ref('')

const loadedScheme = computed(() =>
  loadedSchemeId.value !== null
    ? props.schemes.find(s => s.id === loadedSchemeId.value) ?? null
    : null
)

// 打开时重置状态
watch(
  () => props.visible,
  (v) => {
    if (v) {
      localFilters.value = props.modelValue.map(f => ({ ...f }))
      loadedSchemeId.value = null
      updatingName.value = ''
    }
  },
  { immediate: true }
)

function handleLoadScheme(scheme: FilterScheme) {
  try {
    localFilters.value = JSON.parse(scheme.config) as FilterCondition[]
  } catch {
    localFilters.value = []
  }
  loadedSchemeId.value = scheme.id
  updatingName.value = scheme.name
  emit('load-scheme', scheme)
}

// ────────── 新建方案 ──────────
const schemeName = ref('')

function handleSaveScheme() {
  if (!schemeName.value.trim()) {
    ElMessage.warning('请输入方案名称')
    return
  }
  // 只要选了字段即可保存（允许筛选值为空，方便后续使用时填值）
  const conditions = localFilters.value.filter(f => f.field)
  emit('save-scheme', schemeName.value.trim(), conditions)
  schemeName.value = ''
}

// ────────── 更新已加载方案 ──────────
function handleUpdateScheme() {
  if (!updatingName.value.trim()) {
    ElMessage.warning('方案名称不能为空')
    return
  }
  if (loadedSchemeId.value === null) return
  const conditions = localFilters.value.filter(f => f.field)
  emit('update-scheme', loadedSchemeId.value, updatingName.value.trim(), conditions)
}

// ────────── 删除方案 ──────────
async function handleDeleteScheme(id: number) {
  await ElMessageBox.confirm('确定删除该筛选方案？', '提示', {
    confirmButtonText: '删除',
    cancelButtonText: '取消',
    type: 'warning'
  })
  emit('delete-scheme', id)
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
          <div
            v-for="s in schemes"
            :key="s.id"
            class="scheme-item"
            :class="{ 'scheme-active': loadedSchemeId === s.id }"
          >
            <el-tooltip placement="right" :show-after="300">
              <template #content>
                <div
                  v-for="(c, i) in parseSchemeConditions(s)"
                  :key="i"
                  style="font-size:12px;"
                >
                  {{ c }}
                </div>
                <div v-if="parseSchemeConditions(s).length === 0" style="font-size:12px;color:#ccc;">
                  暂无条件
                </div>
              </template>
              <span class="scheme-name" @click="handleLoadScheme(s)">{{ s.name }}</span>
            </el-tooltip>
            <div style="display:flex;align-items:center;gap:4px;">
              <el-tag v-if="loadedSchemeId === s.id" size="small" type="primary">已加载</el-tag>
              <el-button
                link
                type="danger"
                size="small"
                :loading="deletingSchemeId === s.id"
                @click="handleDeleteScheme(s.id)"
              >
                删除
              </el-button>
            </div>
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
            placeholder="筛选值（可选）"
            size="small"
            style="flex: 1;"
          />

          <el-button link type="danger" size="small" @click="removeCondition(idx)">
            <el-icon><Delete /></el-icon>
          </el-button>
        </div>
      </div>

      <el-divider />

      <!-- ── 更新已加载方案（有加载方案时显示） ── -->
      <div v-if="loadedScheme" class="section">
        <div class="section-title">更新方案</div>
        <div class="save-row">
          <el-input
            v-model="updatingName"
            placeholder="方案名称"
            size="small"
            style="flex: 1;"
          />
          <el-button
            size="small"
            type="primary"
            :loading="schemeUpdating"
            @click="handleUpdateScheme"
          >
            更新
          </el-button>
        </div>
        <div class="update-hint">将用当前筛选条件覆盖「{{ loadedScheme.name }}」</div>
      </div>

      <el-divider v-if="loadedScheme" />

      <!-- ── 新建方案 ── -->
      <div class="section">
        <div class="section-title">{{ loadedScheme ? '另存为新方案' : '保存为方案' }}</div>
        <div class="save-row">
          <el-input
            v-model="schemeName"
            placeholder="方案名称"
            size="small"
            style="flex: 1;"
          />
          <el-button size="small" :loading="schemeSaving" @click="handleSaveScheme">保存</el-button>
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
  border: 1px solid transparent;
}

.scheme-item.scheme-active {
  background: #ecf5ff;
  border-color: #b3d8ff;
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

.update-hint {
  font-size: 12px;
  color: #909399;
  margin-top: 6px;
}
</style>
