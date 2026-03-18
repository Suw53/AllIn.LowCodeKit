<template>
  <el-dialog
    :model-value="visible"
    title="编辑转换脚本"
    width="650px"
    @update:model-value="$emit('update:visible', $event)"
    destroy-on-close
  >
    <div class="script-editor-wrapper">
      <!-- 变量说明 -->
      <el-alert type="info" :closable="false" class="var-hint">
        <template #title>
          <span>可用变量：</span>
          <el-tag size="small">Value</el-tag> 原始值（string?）&nbsp;&nbsp;
          <el-tag size="small">SourceColumn</el-tag> 源列名&nbsp;&nbsp;
          <el-tag size="small">TargetField</el-tag> 目标字段名
        </template>
      </el-alert>

      <!-- 映射信息 -->
      <div class="mapping-info">
        <span>源列：<strong>{{ sourceColumn }}</strong></span>
        <span style="margin: 0 12px;">→</span>
        <span>目标字段：<strong>{{ targetField }}</strong></span>
      </div>

      <!-- 代码编辑器 -->
      <CodeEditor
        v-model="localScript"
        language="csharp"
        height="240px"
      />

      <!-- 示例提示 -->
      <div class="examples">
        <span class="examples-label">示例：</span>
        <el-tag
          v-for="ex in examples"
          :key="ex.label"
          size="small"
          class="example-tag"
          @click="localScript = ex.code"
          style="cursor: pointer;"
        >
          {{ ex.label }}
        </el-tag>
      </div>
    </div>

    <template #footer>
      <el-button @click="handleClear">清空脚本</el-button>
      <el-button @click="$emit('update:visible', false)">取消</el-button>
      <el-button type="primary" @click="handleConfirm">确定</el-button>
    </template>
  </el-dialog>
</template>

<script setup lang="ts">
import { ref, watch } from 'vue'
import CodeEditor from './CodeEditor.vue'

const props = defineProps<{
  visible: boolean
  /** 当前转换脚本内容 */
  script: string | null
  /** Excel源列名 */
  sourceColumn: string
  /** 目标字段名 */
  targetField: string
}>()

const emit = defineEmits<{
  'update:visible': [val: boolean]
  /** 确认后返回新脚本（null 表示清空） */
  confirm: [script: string | null]
}>()

const localScript = ref('')

/** 打开时同步外部值 */
watch(() => props.visible, (v) => {
  if (v) {
    localScript.value = props.script || 'return Value;'
  }
})

/** 示例脚本列表 */
const examples = [
  { label: '去除空格', code: 'return Value?.Trim();' },
  { label: '去除"元"', code: 'return Value?.Replace("元","").Trim();' },
  { label: '提取数字', code: 'return Value == null ? null : Regex.Match(Value, @"\\d+\\.?\\d*").Value;' },
  { label: '日期格式化', code: 'return DateTime.TryParse(Value, out var d) ? d.ToString("yyyy-MM-dd") : Value;' }
]

function handleConfirm() {
  const code = localScript.value.trim()
  emit('confirm', code || null)
  emit('update:visible', false)
}

function handleClear() {
  localScript.value = ''
  emit('confirm', null)
  emit('update:visible', false)
}
</script>

<style scoped>
.script-editor-wrapper {
  display: flex;
  flex-direction: column;
  gap: 12px;
}
.var-hint {
  padding: 8px 12px;
}
.var-hint .el-tag {
  font-family: monospace;
  margin: 0 2px;
}
.mapping-info {
  font-size: 13px;
  color: var(--el-text-color-regular);
}
.examples {
  display: flex;
  align-items: center;
  gap: 6px;
  flex-wrap: wrap;
}
.examples-label {
  font-size: 12px;
  color: var(--el-text-color-secondary);
}
.example-tag:hover {
  opacity: 0.8;
}
</style>
