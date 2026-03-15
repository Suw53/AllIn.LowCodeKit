<!-- Monaco Editor 封装组件，支持 C# 语法高亮，离线可用 -->
<script setup lang="ts">
import { ref, watch, onMounted, onBeforeUnmount } from 'vue'
import * as monaco from 'monaco-editor'

// ────────── Props / Emits ──────────
const props = withDefaults(defineProps<{
  modelValue: string
  language?: string
  readOnly?: boolean
  height?: string
}>(), {
  language: 'csharp',
  readOnly: false,
  height: '100%'
})

const emit = defineEmits<{ 'update:modelValue': [v: string] }>()

// ────────── 编辑器实例管理 ──────────
const editorEl = ref<HTMLDivElement>()
let editorInstance: monaco.editor.IStandaloneCodeEditor | null = null
/** 避免外部 value 变化触发死循环的标志 */
let isUpdatingFromOutside = false

onMounted(() => {
  if (!editorEl.value) return
  editorInstance = monaco.editor.create(editorEl.value, {
    value: props.modelValue,
    language: props.language,
    theme: 'vs',
    readOnly: props.readOnly,
    minimap: { enabled: false },
    scrollBeyondLastLine: false,
    fontSize: 13,
    lineNumbers: 'on',
    wordWrap: 'on',
    automaticLayout: true,
    tabSize: 4,
    insertSpaces: true,
  })

  editorInstance.onDidChangeModelContent(() => {
    if (!isUpdatingFromOutside) {
      emit('update:modelValue', editorInstance!.getValue())
    }
  })
})

// 外部 v-model 变化时同步到编辑器（不移动光标）
watch(() => props.modelValue, (newVal) => {
  if (!editorInstance) return
  if (editorInstance.getValue() === newVal) return
  isUpdatingFromOutside = true
  editorInstance.setValue(newVal)
  isUpdatingFromOutside = false
})

watch(() => props.readOnly, (val) => {
  editorInstance?.updateOptions({ readOnly: val })
})

onBeforeUnmount(() => {
  editorInstance?.dispose()
  editorInstance = null
})
</script>

<template>
  <div ref="editorEl" :style="{ height, width: '100%' }" class="code-editor" />
</template>

<style scoped>
.code-editor {
  border: 1px solid #dcdfe6;
  border-radius: 4px;
  overflow: hidden;
}
</style>
