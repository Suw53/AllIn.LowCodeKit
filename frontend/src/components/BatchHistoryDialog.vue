<!-- 历史批次数据查询对话框：展示各批次统计，支持按批次筛选主列表 -->
<script setup lang="ts">
import { ref, watch } from 'vue'
import { getBatchStats } from '@/api/data'
import type { BatchStat } from '@/api/data'

// ────────── Props / Emits ──────────
const props = defineProps<{
  visible: boolean
  menuId: number
}>()

const emit = defineEmits<{
  'update:visible': [v: boolean]
  /** 查看指定批次（空字符串表示查看全部） */
  'view-batch': [batchId: string]
}>()

// ────────── 批次统计数据 ──────────
const stats = ref<BatchStat[]>([])
const loading = ref(false)

watch(
  () => props.visible,
  async (v) => {
    if (v) {
      loading.value = true
      try {
        stats.value = await getBatchStats(props.menuId)
      } catch {
        stats.value = []
      } finally {
        loading.value = false
      }
    }
  }
)

/** 从批次号 BATCH-YYYYMMDD-HHmmss 解析为可读时间 */
function parseBatchTime(batchId: string): string {
  const m = batchId.match(/^BATCH-(\d{4})(\d{2})(\d{2})-(\d{2})(\d{2})(\d{2})$/)
  if (!m) return batchId
  return `${m[1]}-${m[2]}-${m[3]} ${m[4]}:${m[5]}:${m[6]}`
}

function handleViewBatch(batchId: string) {
  emit('view-batch', batchId)
  emit('update:visible', false)
}

function handleViewAll() {
  emit('view-batch', '')
  emit('update:visible', false)
}
</script>

<template>
  <el-dialog
    :model-value="visible"
    title="历史批次数据"
    width="600px"
    :close-on-click-modal="false"
    @update:model-value="emit('update:visible', $event)"
  >
    <div v-loading="loading" class="batch-dialog-body">
      <div v-if="!loading && stats.length === 0" class="empty-hint">
        暂无批次数据（仅手动添加的数据无批次记录）
      </div>

      <el-table
        v-else
        :data="stats"
        border
        size="small"
        style="width: 100%"
      >
        <el-table-column label="批次号" prop="batchId" min-width="190" show-overflow-tooltip />
        <el-table-column label="导入时间" min-width="165">
          <template #default="{ row }">{{ parseBatchTime(row.batchId) }}</template>
        </el-table-column>
        <el-table-column label="数据行数" prop="count" width="90" align="center" />
        <el-table-column label="操作" width="110" align="center">
          <template #default="{ row }">
            <el-button link type="primary" size="small" @click="handleViewBatch(row.batchId)">
              查看该批次
            </el-button>
          </template>
        </el-table-column>
      </el-table>
    </div>

    <template #footer>
      <el-button @click="handleViewAll">查看全部数据</el-button>
      <el-button @click="emit('update:visible', false)">关闭</el-button>
    </template>
  </el-dialog>
</template>

<style scoped>
.batch-dialog-body {
  min-height: 120px;
}

.empty-hint {
  text-align: center;
  color: #c0c4cc;
  font-size: 13px;
  padding: 40px 0;
}
</style>
