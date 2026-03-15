<!-- 数据录入/编辑对话框：按表单设计的 Span 栅格布局渲染字段 -->
<script setup lang="ts">
import { ref, watch, computed } from 'vue'
import { ElMessage } from 'element-plus'
import type { FormField, DataRow } from '@/types'

// ────────── Props / Emits ──────────
const props = defineProps<{
  visible: boolean
  fields: FormField[]
  /** 编辑模式传入已有行，新增模式不传 */
  row?: DataRow
}>()

const emit = defineEmits<{
  'update:visible': [v: boolean]
  submit: [data: Record<string, string | null>]
}>()

// ────────── 本地表单数据 ──────────
const formData = ref<Record<string, string>>({})

watch(
  () => props.visible,
  (v) => {
    if (v) {
      // 打开时初始化：编辑时回填已有值，新增时清空
      const init: Record<string, string> = {}
      for (const f of props.fields) {
        init[f.fieldName] = props.row ? String(props.row[f.fieldName] ?? '') : ''
      }
      formData.value = init
    }
  },
  { immediate: true }
)

const title = computed(() => props.row ? '编辑记录' : '新增记录')

// ────────── 下拉选项解析 ──────────
function parseOptions(optionsJson?: string): string[] {
  if (!optionsJson) return []
  try { return JSON.parse(optionsJson) as string[] }
  catch { return [] }
}

// ────────── 提交 ──────────
function handleSubmit() {
  // 必填校验
  for (const f of props.fields) {
    if (f.isRequired && !formData.value[f.fieldName]?.trim()) {
      ElMessage.warning(`「${f.label}」为必填项`)
      return
    }
  }
  // 转为 string | null（空字符串转 null 方便后端存储）
  const data: Record<string, string | null> = {}
  for (const f of props.fields) {
    const v = formData.value[f.fieldName]
    data[f.fieldName] = v === '' ? null : v
  }
  emit('submit', data)
}

function handleClose() {
  emit('update:visible', false)
}
</script>

<template>
  <el-dialog
    :model-value="visible"
    :title="title"
    width="640px"
    draggable
    :close-on-click-modal="false"
    @update:model-value="emit('update:visible', $event)"
  >
    <!-- 与表单设计器相同的双列栅格布局 -->
    <div class="form-grid">
      <div
        v-for="field in fields"
        :key="field.fieldName"
        :class="['form-cell', field.span === 2 ? 'span-full' : '']"
      >
        <label class="form-label">
          {{ field.label }}
          <span v-if="field.isRequired" class="required-star">*</span>
        </label>

        <!-- 下拉框 -->
        <el-select
          v-if="field.fieldType === 'Select'"
          v-model="formData[field.fieldName]"
          placeholder="请选择"
          clearable
          style="width: 100%;"
        >
          <el-option
            v-for="opt in parseOptions(field.options)"
            :key="opt"
            :label="opt"
            :value="opt"
          />
        </el-select>

        <!-- 文本框 -->
        <el-input
          v-else
          v-model="formData[field.fieldName]"
          :placeholder="`请输入${field.label}`"
          clearable
        />
      </div>
    </div>

    <template #footer>
      <el-button @click="handleClose">取消</el-button>
      <el-button type="primary" @click="handleSubmit">确定</el-button>
    </template>
  </el-dialog>
</template>

<style scoped>
.form-grid {
  display: grid;
  grid-template-columns: 1fr 1fr;
  gap: 14px 20px;
  padding: 4px 0 8px;
}

.form-cell {
  display: flex;
  flex-direction: column;
  gap: 5px;
}

.form-cell.span-full {
  grid-column: 1 / -1;
}

.form-label {
  font-size: 13px;
  color: #606266;
  font-weight: 500;
}

.required-star {
  color: #f56c6c;
  margin-left: 2px;
}
</style>
