<!-- 模块数据列表页：VXE-Table 动态列、搜索、筛选、新增/编辑/删除 -->
<script setup lang="ts">
import { ref, computed, watch, onMounted, onUnmounted } from 'vue'
import { useRoute, useRouter } from 'vue-router'
import { ElMessage, ElMessageBox } from 'element-plus'
import { useMenuStore } from '@/stores/menuStore'
import { useModuleStore } from '@/stores/moduleStore'
import { useFormTemplateStore } from '@/stores/formTemplateStore'
import DataForm from '@/components/DataForm.vue'
import DataFilter from '@/components/DataFilter.vue'
import type { DataRow, FilterCondition } from '@/types'

// ────────── 路由 & Store ──────────
const route = useRoute()
const router = useRouter()
const menuStore = useMenuStore()
const store = useModuleStore()
const templateStore = useFormTemplateStore()

const menuId = computed(() => Number(route.params.menuId))
const menuName = computed(() => {
  for (const m of menuStore.menuList) {
    const child = m.children.find(c => c.id === menuId.value)
    if (child) return child.name
  }
  return `模块 #${menuId.value}`
})

// ────────── 模板（字段列定义） ──────────
const fields = computed(() => templateStore.template?.fields ?? [])
const sortedFields = computed(() =>
  [...fields.value].sort((a, b) => a.columnOrder - b.columnOrder)
)

// ────────── 初始化 ──────────
onMounted(async () => {
  store.reset()
  await templateStore.loadByMenu(menuId.value)
  if (templateStore.template) {
    await Promise.all([
      store.fetchData(menuId.value),
      store.fetchFilterSchemes(menuId.value),
      store.fetchExportPreference(menuId.value)
    ])
  }
})

// 路由切换时重新加载
watch(menuId, async (id) => {
  store.reset()
  templateStore.reset()
  await templateStore.loadByMenu(id)
  if (templateStore.template) {
    await Promise.all([
      store.fetchData(id),
      store.fetchFilterSchemes(id),
      store.fetchExportPreference(id)
    ])
  }
})

onUnmounted(() => {
  store.reset()
})

// ────────── 搜索 ──────────
const keywordInput = ref('')
let searchTimer: ReturnType<typeof setTimeout> | null = null

function onKeywordChange() {
  if (searchTimer) clearTimeout(searchTimer)
  searchTimer = setTimeout(() => {
    store.keyword = keywordInput.value
    store.page = 1
    store.fetchData(menuId.value)
  }, 300)
}

// ────────── 高级筛选 ──────────
const filterVisible = ref(false)

function onFiltersApply() {
  store.page = 1
  store.fetchData(menuId.value)
}

async function onSaveScheme(name: string) {
  try {
    await store.saveFilterScheme(menuId.value, name)
    ElMessage.success('筛选方案已保存')
  } catch {
    ElMessage.error('保存失败')
  }
}

async function onDeleteScheme(id: number) {
  try {
    await store.removeFilterScheme(id)
  } catch {
    ElMessage.error('删除失败')
  }
}

// ────────── 新增/编辑 ──────────
const formVisible = ref(false)
const editingRow = ref<DataRow | undefined>()

function openAdd() {
  editingRow.value = undefined
  formVisible.value = true
}

function openEdit(row: DataRow) {
  editingRow.value = row
  formVisible.value = true
}

async function handleFormSubmit(data: Record<string, string | null>) {
  try {
    if (editingRow.value) {
      await store.editRow(menuId.value, Number(editingRow.value['Id']), data)
      ElMessage.success('更新成功')
    } else {
      await store.addRow(menuId.value, data)
      ElMessage.success('新增成功')
    }
    formVisible.value = false
  } catch {
    ElMessage.error('操作失败')
  }
}

// ────────── 删除 ──────────
async function handleDelete(row: DataRow) {
  await ElMessageBox.confirm('确定删除该条记录？', '提示', {
    confirmButtonText: '删除',
    cancelButtonText: '取消',
    type: 'warning'
  })
  try {
    await store.removeRow(menuId.value, Number(row['Id']))
    ElMessage.success('删除成功')
  } catch {
    ElMessage.error('删除失败')
  }
}

// ────────── 分页 ──────────
function onPageChange(p: number) {
  store.page = p
  store.fetchData(menuId.value)
}

function onPageSizeChange(ps: number) {
  store.pageSize = ps
  store.page = 1
  store.fetchData(menuId.value)
}

// ────────── 导出 ──────────
const exportVisible = ref(false)
const exportSelected = ref<string[]>([])

function openExport() {
  // 默认使用上次记忆的列，若无则全选
  exportSelected.value = store.exportColumns.length
    ? store.exportColumns
    : sortedFields.value.map(f => f.fieldName)
  exportVisible.value = true
}

async function handleExport() {
  await store.saveExportColumns(menuId.value, exportSelected.value)

  const colFields = sortedFields.value.filter(f => exportSelected.value.includes(f.fieldName))
  const headers = ['序号', ...colFields.map(f => f.label)]
  const csvRows = [headers.join(',')]
  store.rows.forEach((row, idx) => {
    const cells = [String(idx + 1), ...colFields.map(f => {
      const v = String(row[f.fieldName] ?? '')
      return v.includes(',') || v.includes('\n') ? `"${v.replace(/"/g, '""')}"` : v
    })]
    csvRows.push(cells.join(','))
  })

  const bom = '\uFEFF'
  const blob = new Blob([bom + csvRows.join('\n')], { type: 'text/csv;charset=utf-8' })
  const url = URL.createObjectURL(blob)
  const a = document.createElement('a')
  a.href = url
  a.download = `${menuName.value}-${new Date().toLocaleDateString()}.csv`
  a.click()
  URL.revokeObjectURL(url)
  exportVisible.value = false
}

// ────────── 筛选激活标记 ──────────
const hasActiveFilter = computed(() => store.activeFilters.length > 0)
</script>

<template>
  <div class="module-page">

    <!-- ── 顶部工具栏 ── -->
    <div class="module-header">
      <div class="header-left">
        <el-button link icon="ArrowLeft" @click="router.back()">返回</el-button>
        <span class="page-title">{{ menuName }}</span>
        <el-tag v-if="store.total > 0" type="info" size="small">共 {{ store.total }} 条</el-tag>
      </div>
      <div class="header-right">
        <el-input
          v-model="keywordInput"
          placeholder="关键词搜索"
          size="small"
          clearable
          style="width: 200px;"
          @input="onKeywordChange"
          @clear="onKeywordChange"
        >
          <template #prefix><el-icon><Search /></el-icon></template>
        </el-input>

        <el-badge :is-dot="hasActiveFilter">
          <el-button size="small" icon="Filter" @click="filterVisible = true">高级筛选</el-button>
        </el-badge>

        <el-button size="small" type="primary" icon="Plus" @click="openAdd">新增</el-button>
        <el-button size="small" icon="Download" @click="openExport">导出</el-button>
        <el-button size="small" icon="Setting" disabled title="Phase 6 实现">自动化</el-button>
      </div>
    </div>

    <!-- ── 无模板提示 ── -->
    <div v-if="!templateStore.loading && !templateStore.template" class="no-template">
      <el-empty description="该模块尚未配置表单，请先进行表单设计">
        <el-button type="primary" @click="router.push(`/form-designer/${menuId}`)">
          前往表单设计器
        </el-button>
      </el-empty>
    </div>

    <!-- ── 数据表格 ── -->
    <template v-else-if="templateStore.template">
      <div class="table-wrap">
        <vxe-table
          :data="store.rows"
          :loading="store.loading"
          border="inner"
          stripe
          height="100%"
          size="small"
          :scroll-y="{ enabled: true }"
        >
          <vxe-column type="seq" width="60" title="序号" fixed="left" />

          <vxe-column
            v-for="field in sortedFields"
            :key="field.fieldName"
            :field="field.fieldName"
            :title="field.label"
            min-width="120"
            show-overflow
          />

          <vxe-column title="操作" width="120" fixed="right">
            <template #default="{ row }">
              <el-button link type="primary" size="small" @click="openEdit(row)">编辑</el-button>
              <el-button link type="danger" size="small" @click="handleDelete(row)">删除</el-button>
            </template>
          </vxe-column>
        </vxe-table>
      </div>

      <!-- ── 分页 ── -->
      <div class="pagination">
        <el-pagination
          v-model:current-page="store.page"
          v-model:page-size="store.pageSize"
          :total="store.total"
          :page-sizes="[50, 100, 200, 500]"
          layout="total, sizes, prev, pager, next"
          background
          small
          @current-change="onPageChange"
          @size-change="onPageSizeChange"
        />
      </div>
    </template>

    <!-- ── 新增/编辑对话框 ── -->
    <DataForm
      v-model:visible="formVisible"
      :fields="sortedFields"
      :row="editingRow"
      @submit="handleFormSubmit"
    />

    <!-- ── 高级筛选抽屉 ── -->
    <DataFilter
      v-model:visible="filterVisible"
      v-model="store.activeFilters"
      :fields="sortedFields"
      :schemes="store.filterSchemes"
      @apply="onFiltersApply"
      @save-scheme="onSaveScheme"
      @delete-scheme="onDeleteScheme"
      @load-scheme="store.applyScheme($event)"
    />

    <!-- ── 导出列选择对话框 ── -->
    <el-dialog v-model="exportVisible" title="选择导出列" width="400px">
      <el-checkbox-group v-model="exportSelected" class="export-checkbox-group">
        <el-checkbox
          v-for="field in sortedFields"
          :key="field.fieldName"
          :value="field.fieldName"
          :label="field.label"
        />
      </el-checkbox-group>
      <template #footer>
        <el-button @click="exportSelected = sortedFields.map(f => f.fieldName)">全选</el-button>
        <el-button @click="exportVisible = false">取消</el-button>
        <el-button type="primary" @click="handleExport">导出 CSV</el-button>
      </template>
    </el-dialog>

  </div>
</template>

<style scoped>
.module-page {
  height: 100vh;
  display: flex;
  flex-direction: column;
  background: #f0f2f5;
  overflow: hidden;
}

.module-header {
  height: 52px;
  flex-shrink: 0;
  background: #fff;
  border-bottom: 1px solid #e4e7ed;
  display: flex;
  align-items: center;
  justify-content: space-between;
  padding: 0 16px;
  gap: 12px;
}

.header-left {
  display: flex;
  align-items: center;
  gap: 8px;
}

.header-right {
  display: flex;
  align-items: center;
  gap: 8px;
}

.page-title {
  font-size: 15px;
  font-weight: 600;
  color: #303133;
}

.no-template {
  flex: 1;
  display: flex;
  align-items: center;
  justify-content: center;
}

.table-wrap {
  flex: 1;
  overflow: hidden;
  padding: 12px 16px 0;
}

.pagination {
  flex-shrink: 0;
  padding: 10px 16px;
  background: #fff;
  border-top: 1px solid #e4e7ed;
  display: flex;
  justify-content: flex-end;
}

.export-checkbox-group {
  display: flex;
  flex-direction: column;
  gap: 8px;
}
</style>
