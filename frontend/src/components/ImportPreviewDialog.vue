<!-- 导入预览对话框：展示每行解析结果（成功/失败），用户确认后才真正导入 -->
<script setup lang="ts">
import { computed } from 'vue'
import type { FormField } from '@/types'
import type { PreviewRow } from '@/api/data'

const props = defineProps<{
  visible: boolean
  rows: PreviewRow[]
  batchId: string
  fields: FormField[]
  confirming: boolean
}>()

const emit = defineEmits<{
  'update:visible': [boolean]
  confirm: []
}>()

const successCount = computed(() => props.rows.filter(r => r.status === 'ok').length)
const errorCount = computed(() => props.rows.filter(r => r.status === 'error').length)

function getRowClassName({ row }: { row: PreviewRow }) {
  return row.status === 'error' ? 'row-error' : 'row-ok'
}
</script>

<template>
  <el-dialog
    :model-value="visible"
    title="导入预览"
    width="900px"
    :close-on-click-modal="false"
    @update:model-value="emit('update:visible', $event)"
  >
    <!-- 摘要信息 -->
    <div class="preview-summary">
      <el-tag type="info" size="large">共 {{ rows.length }} 行</el-tag>
      <el-tag type="success" size="large">
        <el-icon><CircleCheck /></el-icon> 成功 {{ successCount }} 行
      </el-tag>
      <el-tag v-if="errorCount > 0" type="danger" size="large">
        <el-icon><CircleClose /></el-icon> 失败 {{ errorCount }} 行
      </el-tag>
      <div class="batch-label">
        批次号：<el-tag type="warning" size="small">{{ batchId }}</el-tag>
      </div>
    </div>

    <!-- 数据预览表格 -->
    <div class="preview-table-wrap">
      <el-table
        :data="rows"
        border
        size="small"
        :max-height="400"
        :row-class-name="getRowClassName"
      >
        <!-- 行号 -->
        <el-table-column label="行号" prop="rowIndex" width="60" fixed="left" align="center" />

        <!-- 状态 -->
        <el-table-column label="状态" width="80" fixed="left" align="center">
          <template #default="{ row }">
            <el-tag :type="row.status === 'ok' ? 'success' : 'danger'" size="small">
              {{ row.status === 'ok' ? '成功' : '失败' }}
            </el-tag>
          </template>
        </el-table-column>

        <!-- 数据列 -->
        <el-table-column
          v-for="field in fields"
          :key="field.fieldName"
          :label="field.label"
          min-width="120"
          show-overflow-tooltip
        >
          <template #default="{ row }">
            {{ row.data[field.fieldName] ?? '' }}
          </template>
        </el-table-column>

        <!-- 失败原因 -->
        <el-table-column label="失败原因" min-width="200" fixed="right">
          <template #default="{ row }">
            <span v-if="row.errors.length > 0" class="error-text">
              {{ row.errors.join('；') }}
            </span>
          </template>
        </el-table-column>
      </el-table>
    </div>

    <template #footer>
      <div class="dialog-footer">
        <span class="hint" v-if="errorCount > 0">
          失败行将跳过，仅导入 {{ successCount }} 行成功数据
        </span>
        <el-button @click="emit('update:visible', false)">取消</el-button>
        <el-button
          type="primary"
          :loading="confirming"
          :disabled="successCount === 0"
          @click="emit('confirm')"
        >
          确认导入 {{ successCount }} 行
        </el-button>
      </div>
    </template>
  </el-dialog>
</template>

<style scoped>
.preview-summary {
  display: flex;
  align-items: center;
  gap: 10px;
  margin-bottom: 16px;
  flex-wrap: wrap;
}

.batch-label {
  margin-left: auto;
  font-size: 12px;
  color: #909399;
  display: flex;
  align-items: center;
  gap: 6px;
}

.preview-table-wrap {
  border-radius: 4px;
  overflow: hidden;
}

.error-text {
  color: #f56c6c;
  font-size: 12px;
}

.dialog-footer {
  display: flex;
  align-items: center;
  gap: 12px;
  justify-content: flex-end;
}

.hint {
  font-size: 12px;
  color: #e6a23c;
}
</style>

<style>
/* 全局：失败行背景色（不能 scoped，因为 el-table 的 row-class 在外层） */
.el-table .row-error td {
  background-color: #fff0f0 !important;
}
</style>
