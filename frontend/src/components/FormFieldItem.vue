<!-- 表单字段行组件：支持展开/收起属性表单、拖拽排序（外部由 vuedraggable 提供 handle） -->
<script setup lang="ts">
import { ref, computed, watch } from 'vue'
import type { FormField } from '@/types'

// ────────── Props / Emits ──────────
const props = defineProps<{
  modelValue: FormField
  index: number
}>()

const emit = defineEmits<{
  'update:modelValue': [v: FormField]
  delete: []
}>()

// ────────── 本地编辑状态 ──────────
const expanded = ref(false)
/** 本地副本，展开后编辑；保存时 emit */
const local = ref<FormField>({ ...props.modelValue })

watch(() => props.modelValue, (v) => {
  if (!expanded.value) local.value = { ...v }
}, { deep: true })

// ────────── 下拉选项：用独立 ref 存文本，blur 时才转 JSON ──────────
/** 文本形式的选项（供输入框直接绑定，避免实时转 JSON 导致逗号被吞） */
const optionsInput = ref('')

/** 当字段类型变化或属性面板展开时，同步 JSON → 文本 */
watch(() => local.value.options, (jsonStr) => {
  try {
    const arr = JSON.parse(jsonStr ?? '[]') as string[]
    optionsInput.value = arr.join(', ')
  } catch {
    optionsInput.value = jsonStr ?? ''
  }
}, { immediate: true })

/** 失焦时将文本转回 JSON 存入 local */
function onOptionsBlur() {
  const arr = optionsInput.value.split(',').map(s => s.trim()).filter(Boolean)
  local.value.options = JSON.stringify(arr)
}

// ────────── 展开 / 折叠 ──────────
function toggleExpand() {
  if (!expanded.value) {
    local.value = { ...props.modelValue }
  }
  expanded.value = !expanded.value
}

// ────────── 保存属性 ──────────
function save() {
  if (!local.value.fieldName.trim()) {
    return // 字段名不能为空，由模板校验提示
  }
  emit('update:modelValue', { ...local.value })
  expanded.value = false
}

// ────────── 外部触发展开（新拖入字段） ──────────
function openForEdit() {
  local.value = { ...props.modelValue }
  expanded.value = true
}

defineExpose({ openForEdit })
</script>

<template>
  <div class="field-item" :class="{ expanded }">
    <!-- ── 折叠行 ── -->
    <div class="field-row" @click="toggleExpand">
      <!-- 拖拽 handle -->
      <el-icon class="drag-handle" title="拖拽排序"><Rank /></el-icon>

      <!-- 字段类型徽章 -->
      <el-tag
        :type="modelValue.fieldType === 'Select' ? 'warning' : 'info'"
        size="small"
        class="type-badge"
      >
        {{ modelValue.fieldType === 'Select' ? '下拉框' : '文本框' }}
      </el-tag>

      <!-- 列宽标签 -->
      <el-tag
        v-if="modelValue.span === 2"
        type="success"
        size="small"
        class="type-badge"
      >
        全宽
      </el-tag>

      <!-- 标签名 + 字段名 tooltip -->
      <el-tooltip :content="`DB字段名：${modelValue.fieldName || '未设置'}`" placement="top" :show-after="500">
        <span class="field-label">
          {{ modelValue.label || '(未命名)' }}
          <span v-if="modelValue.isRequired" class="required-star">*</span>
        </span>
      </el-tooltip>

      <!-- 占位 flex -->
      <span class="spacer" />

      <!-- 操作按钮 -->
      <el-button
        link
        type="primary"
        size="small"
        :icon="expanded ? 'ArrowUp' : 'ArrowDown'"
        @click.stop="toggleExpand"
      />
      <el-button
        link
        type="danger"
        size="small"
        icon="Delete"
        @click.stop="emit('delete')"
      />
    </div>

    <!-- ── 展开属性面板 ── -->
    <div v-if="expanded" class="field-form" @click.stop>
      <div class="form-grid">
        <div class="form-item">
          <label class="form-label">字段名 <span class="required-star">*</span></label>
          <el-tooltip content="存入数据库的英文字段名（PascalCase）" placement="top">
            <el-input
              v-model="local.fieldName"
              placeholder="如 CustomerName"
              size="small"
            />
          </el-tooltip>
        </div>
        <div class="form-item">
          <label class="form-label">标签名 <span class="required-star">*</span></label>
          <el-input
            v-model="local.label"
            placeholder="如 客户名称"
            size="small"
          />
        </div>
        <div class="form-item">
          <label class="form-label">字段类型</label>
          <el-select v-model="local.fieldType" size="small">
            <el-option label="文本框" value="Text" />
            <el-option label="下拉框" value="Select" />
          </el-select>
        </div>
        <div class="form-item">
          <label class="form-label">列头序号</label>
          <el-input-number
            v-model="local.columnOrder"
            :min="1"
            :max="999"
            size="small"
            controls-position="right"
          />
        </div>
        <div v-if="local.fieldType === 'Select'" class="form-item full-width">
          <label class="form-label">
            下拉选项
            <span class="hint">（用英文逗号 <code>,</code> 分隔，如：选项A, 选项B）</span>
          </label>
          <el-input
            v-model="optionsInput"
            placeholder="选项A, 选项B, 选项C"
            size="small"
            @blur="onOptionsBlur"
          />
        </div>
        <div class="form-item full-width">
          <label class="form-label">批注描述 <span class="hint">（显示在 Excel 导入模板列头）</span></label>
          <el-input
            v-model="local.remark"
            placeholder="输入此字段的说明信息"
            size="small"
          />
        </div>
        <div class="form-item">
          <label class="form-label">必填</label>
          <el-switch v-model="local.isRequired" />
        </div>
        <div class="form-item">
          <label class="form-label">列宽</label>
          <el-radio-group v-model="local.span" size="small">
            <el-radio-button :value="1">半宽</el-radio-button>
            <el-radio-button :value="2">全宽</el-radio-button>
          </el-radio-group>
        </div>
      </div>
      <div class="form-actions">
        <el-button size="small" @click="expanded = false">取消</el-button>
        <el-button size="small" type="primary" @click="save">确定</el-button>
      </div>
    </div>
  </div>
</template>

<style scoped>
.field-item {
  border: 1px solid #e4e7ed;
  border-radius: 6px;
  background: #fff;
  transition: box-shadow 0.15s;
  cursor: default;
}

.field-item.expanded {
  box-shadow: 0 2px 8px rgba(64, 158, 255, 0.15);
  border-color: #409eff;
}

.field-row {
  display: flex;
  align-items: center;
  gap: 8px;
  padding: 8px 12px;
  cursor: pointer;
  user-select: none;
}

.drag-handle {
  color: #c0c4cc;
  cursor: grab;
  flex-shrink: 0;
  font-size: 16px;
}

.drag-handle:hover {
  color: #409eff;
}

.type-badge {
  flex-shrink: 0;
}

.field-label {
  font-size: 13px;
  color: #303133;
}

.required-star {
  color: #f56c6c;
  font-weight: bold;
  margin-left: 2px;
}

.spacer {
  flex: 1;
}

.field-form {
  border-top: 1px dashed #e4e7ed;
  padding: 12px 16px 8px;
  background: #fafafa;
  border-radius: 0 0 6px 6px;
}

.form-grid {
  display: grid;
  grid-template-columns: 1fr 1fr;
  gap: 10px 16px;
}

.form-item {
  display: flex;
  flex-direction: column;
  gap: 4px;
}

.form-item.full-width {
  grid-column: 1 / -1;
}

.form-label {
  font-size: 12px;
  color: #606266;
  font-weight: 500;
}

.hint {
  font-size: 11px;
  color: #c0c4cc;
  font-weight: normal;
}

.form-actions {
  display: flex;
  justify-content: flex-end;
  gap: 8px;
  margin-top: 10px;
}
</style>
