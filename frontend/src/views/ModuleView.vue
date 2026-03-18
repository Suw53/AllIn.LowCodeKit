<!-- 模块数据列表页：VXE-Table 动态列、搜索、筛选、新增/编辑/删除、Excel 导入导出、批次管理 -->
<script setup lang="ts">
import { ref, computed, watch, onMounted, onUnmounted, nextTick } from 'vue'
import { useRoute, useRouter } from 'vue-router'
import { ElMessage, ElMessageBox } from 'element-plus'
import { useMenuStore } from '@/stores/menuStore'
import { useModuleStore } from '@/stores/moduleStore'
import { useFormTemplateStore } from '@/stores/formTemplateStore'
import { useTabStore } from '@/stores/tabStore'
import { withLoading } from '@/utils/loading'
import DataForm from '@/components/DataForm.vue'
import DataFilter from '@/components/DataFilter.vue'
import ImportPreviewDialog from '@/components/ImportPreviewDialog.vue'
import BatchHistoryDialog from '@/components/BatchHistoryDialog.vue'
import ImportTemplateConfigDialog from '@/components/ImportTemplateConfigDialog.vue'
import ImportExcelDialog from '@/components/ImportExcelDialog.vue'
import type { DataRow, FilterCondition } from '@/types'
import type { PreviewRow } from '@/api/data'

// ────────── 路由 & Store ──────────
const route = useRoute()
const router = useRouter()
const menuStore = useMenuStore()
const store = useModuleStore()
const templateStore = useFormTemplateStore()
const tabStore = useTabStore()

const menuId = computed(() => Number(route.params.menuId))

// 一级菜单名
const parentMenuName = computed(() => {
  for (const m of menuStore.menuList) {
    if (m.children.some(c => c.id === menuId.value)) return m.name
  }
  return ''
})

// 二级菜单名
const menuName = computed(() => {
  for (const m of menuStore.menuList) {
    const child = m.children.find(c => c.id === menuId.value)
    if (child) return child.name
  }
  return `模块 #${menuId.value}`
})

// 顶部标题：一级 / 二级
const fullTitle = computed(() =>
  parentMenuName.value ? `${parentMenuName.value} / ${menuName.value}` : menuName.value
)

// ────────── 字段列定义（按 columnOrder 排序） ──────────
const sortedFields = computed(() =>
  [...(templateStore.template?.fields ?? [])].sort((a, b) => a.columnOrder - b.columnOrder)
)

// ────────── 初始化 ──────────
async function loadModule(id: number) {
  store.reset()
  templateStore.reset()
  await templateStore.loadByMenu(id)
  if (templateStore.template) {
    await Promise.all([
      store.fetchBatchIds(id),
      store.fetchFilterSchemes(id),
      store.fetchExportPreference(id),
      store.fetchImportTemplateConfigs(id),
      store.fetchImportMappingConfigs(id),
      store.fetchImportPreference(id)
    ])
    await store.fetchData(id)
  }
  tabStore.updateTitle(route.path, fullTitle.value)
  // 等 DOM 渲染后连接 toolbar 与 table
  await nextTick()
  if (vxeTableRef.value && toolbarRef.value) {
    vxeTableRef.value.connect(toolbarRef.value)
  }
}

onMounted(() => loadModule(menuId.value))
watch(menuId, (id) => loadModule(id))
onUnmounted(() => store.reset())

// ────────── 搜索（防抖 300ms） ──────────
const keywordInput = ref('')
let searchTimer: ReturnType<typeof setTimeout> | null = null

function onKeywordChange() {
  if (searchTimer) clearTimeout(searchTimer)
  searchTimer = setTimeout(async () => {
    store.keyword = keywordInput.value
    store.page = 1
    await withLoading(() => store.fetchData(menuId.value), '查询中…')
  }, 300)
}

// ────────── 高级筛选 ──────────
const filterVisible = ref(false)
const hasActiveFilter = computed(() => store.activeFilters.length > 0)
const filterSchemeSaving = ref(false)
const filterDeletingSchemeId = ref<number | null>(null)
const filterSchemeUpdating = ref(false)

async function onFiltersApply() {
  store.page = 1
  await withLoading(() => store.fetchData(menuId.value), '查询中…')
}

async function onSaveScheme(name: string, conditions: FilterCondition[]) {
  filterSchemeSaving.value = true
  try {
    await store.saveFilterScheme(menuId.value, name, conditions)
    ElMessage.success('筛选方案已保存')
  } catch {
    ElMessage.error('保存失败')
  } finally {
    filterSchemeSaving.value = false
  }
}

async function onDeleteScheme(id: number) {
  filterDeletingSchemeId.value = id
  try { await store.removeFilterScheme(id) }
  catch { ElMessage.error('删除失败') }
  finally { filterDeletingSchemeId.value = null }
}

async function onUpdateScheme(id: number, name: string, conditions: FilterCondition[]) {
  filterSchemeUpdating.value = true
  try {
    await store.updateFilterScheme(id, name, conditions)
    ElMessage.success('方案已更新')
  } catch {
    ElMessage.error('更新失败')
  } finally {
    filterSchemeUpdating.value = false
  }
}

// ────────── 新增/编辑 ──────────
const formVisible = ref(false)
const editingRow = ref<DataRow | undefined>()
const formSubmitting = ref(false)

function openAdd() {
  editingRow.value = undefined
  formVisible.value = true
}

function openEdit(row: DataRow) {
  editingRow.value = row
  formVisible.value = true
}

async function handleFormSubmit(data: Record<string, string | null>) {
  formSubmitting.value = true
  try {
    if (editingRow.value) {
      await withLoading(
        () => store.editRow(menuId.value, Number(editingRow.value!['Id']), data),
        '保存中…'
      )
      ElMessage.success('更新成功')
    } else {
      await withLoading(() => store.addRow(menuId.value, data), '新增中…')
      ElMessage.success('新增成功')
    }
    formVisible.value = false
  } catch {
    ElMessage.error('操作失败')
  } finally {
    formSubmitting.value = false
  }
}

// ────────── 单行删除 ──────────
const deletingId = ref<number | null>(null)

async function handleDelete(row: DataRow) {
  await ElMessageBox.confirm('确定删除该条记录？', '提示', {
    confirmButtonText: '删除',
    cancelButtonText: '取消',
    type: 'warning'
  })
  const id = Number(row['Id'])
  deletingId.value = id
  try {
    await withLoading(() => store.removeRow(menuId.value, id), '删除中…')
    ElMessage.success('删除成功')
  } catch {
    ElMessage.error('删除失败')
  } finally {
    deletingId.value = null
  }
}

// ────────── 批量删除 ──────────
const vxeTableRef = ref()
const toolbarRef = ref()
const selectedIds = ref<number[]>([])
const batchDeleting = ref(false)

function onCheckboxChange() {
  const records = vxeTableRef.value?.getCheckboxRecords() ?? []
  selectedIds.value = records.map((r: DataRow) => Number(r['Id']))
}

async function handleBatchDelete() {
  if (selectedIds.value.length === 0) return
  await ElMessageBox.confirm(
    `确定删除选中的 ${selectedIds.value.length} 条记录？`,
    '批量删除',
    { confirmButtonText: '删除', cancelButtonText: '取消', type: 'warning' }
  )
  batchDeleting.value = true
  try {
    await withLoading(() => store.batchRemoveRows(menuId.value, selectedIds.value), '删除中…')
    selectedIds.value = []
    ElMessage.success('批量删除成功')
  } catch {
    ElMessage.error('批量删除失败')
  } finally {
    batchDeleting.value = false
  }
}

// ────────── 历史批次对话框 ──────────
const batchHistoryVisible = ref(false)

async function handleViewBatch(batchId: string) {
  store.currentBatchId = batchId
  store.page = 1
  await withLoading(() => store.fetchData(menuId.value), '查询中…')
}

// ────────── 分页 ──────────
async function onPageChange(p: number) {
  store.page = p
  await withLoading(() => store.fetchData(menuId.value), '加载中…')
}

async function onPageSizeChange(ps: number) {
  store.pageSize = ps
  store.page = 1
  await withLoading(() => store.fetchData(menuId.value), '加载中…')
}

// ────────── 导入模板配置管理 ──────────
const importTemplateDialogVisible = ref(false)

// ────────── 模板下载（支持配置选择） ──────────
const templateDownloading = ref(false)
const templateSelectVisible = ref(false)
const selectedTemplateConfigId = ref<number | undefined>(undefined)

async function handleDownloadTemplate() {
  const configs = store.importTemplateConfigs
  if (configs.length > 0) {
    // 有配置时弹出选择对话框
    templateSelectVisible.value = true
  } else {
    // 无配置，直接下载默认模板
    await doDownloadTemplate(undefined, '默认模板')
  }
}

async function confirmTemplateDownload() {
  const configs = store.importTemplateConfigs
  let configName = '默认模板'

  if (selectedTemplateConfigId.value) {
    const cfg = configs.find(c => c.id === selectedTemplateConfigId.value)
    configName = cfg?.name || '自定义模板'
  }

  templateSelectVisible.value = false
  await doDownloadTemplate(selectedTemplateConfigId.value, configName)
}

async function doDownloadTemplate(configId: number | undefined, configName: string) {
  templateDownloading.value = true
  try {
    await withLoading(() => store.fetchTemplate(menuId.value, configId, configName), '下载中…')
  } catch {
    ElMessage.error('模板下载失败')
  } finally {
    templateDownloading.value = false
  }
}

// ────────── Excel 导入（统一对话框） ──────────
const importExcelDialogVisible = ref(false)
const previewVisible = ref(false)
const previewRows = ref<PreviewRow[]>([])
const previewBatchId = ref('')
const confirming = ref(false)

function handleImportPreview(rows: PreviewRow[], batchId: string) {
  previewRows.value = rows
  previewBatchId.value = batchId
  previewVisible.value = true
}

async function handleConfirmImport() {
  const validRows = previewRows.value
    .filter(r => r.status === 'ok')
    .map(r => r.data)

  confirming.value = true
  try {
    const result = await withLoading(
      () => store.importConfirm(menuId.value, previewBatchId.value, validRows),
      '导入中…'
    )
    previewVisible.value = false
    ElMessage.success(`成功导入 ${result.imported} 条，批次号：${result.batchId}`)
  } catch {
    ElMessage.error('导入失败')
  } finally {
    confirming.value = false
  }
}

// ────────── Excel 导出 ──────────
const exportVisible = ref(false)
const exportSelected = ref<string[]>([])
const exporting = ref(false)

function openExport() {
  exportSelected.value = store.exportColumns.length
    ? store.exportColumns
    : sortedFields.value.map(f => f.fieldName)
  exportVisible.value = true
}

async function handleExport() {
  exporting.value = true
  try {
    await withLoading(async () => {
      await store.saveExportColumns(menuId.value, exportSelected.value)
      await store.exportToExcel(menuId.value, exportSelected.value)
    }, '导出中…')
    exportVisible.value = false
  } catch {
    ElMessage.error('导出失败')
  } finally {
    exporting.value = false
  }
}
</script>

<template>
  <div class="module-page">

    <!-- ── 顶部工具栏 ── -->
    <div class="module-header">
      <div class="header-left">
        <span class="page-title">{{ fullTitle }}</span>
        <el-tag v-if="store.total > 0" type="info" size="small">共 {{ store.total }} 条</el-tag>
        <!-- 批次筛选激活提示 -->
        <el-tag v-if="store.currentBatchId" type="warning" size="small" closable @close="handleViewBatch('')">
          批次：{{ store.currentBatchId }}
        </el-tag>
      </div>
      <div class="header-right">
        <!-- 搜索 -->
        <el-input
          v-model="keywordInput"
          placeholder="关键词搜索"
          size="small"
          clearable
          style="width: 180px;"
          @input="onKeywordChange"
          @clear="onKeywordChange"
        >
          <template #prefix><el-icon><Search /></el-icon></template>
        </el-input>

        <!-- 高级筛选 -->
        <el-badge :is-dot="hasActiveFilter">
          <el-button size="small" icon="Filter" @click="filterVisible = true">高级筛选</el-button>
        </el-badge>

        <!-- 历史批次 -->
        <el-button size="small" icon="Clock" @click="batchHistoryVisible = true">历史批次</el-button>

        <!-- 批量删除（有选中行时出现） -->
        <el-button
          v-if="selectedIds.length > 0"
          size="small"
          type="danger"
          :loading="batchDeleting"
          @click="handleBatchDelete"
        >
          删除选中 ({{ selectedIds.length }})
        </el-button>

        <el-button size="small" type="primary" icon="Plus" @click="openAdd">新增</el-button>

        <!-- 导入/导出下拉 -->
        <el-dropdown trigger="click">
          <el-button size="small" icon="Document" :loading="templateDownloading">
            模板/导入/导出 <el-icon class="el-icon--right"><ArrowDown /></el-icon>
          </el-button>
          <template #dropdown>
            <el-dropdown-menu>
              <el-dropdown-item @click="importTemplateDialogVisible = true">
                <el-icon><Setting /></el-icon> 管理导入模板
              </el-dropdown-item>
              <el-dropdown-item @click="handleDownloadTemplate">
                <el-icon><Download /></el-icon> 下载导入模板
              </el-dropdown-item>
              <el-dropdown-item divided @click="importExcelDialogVisible = true">
                <el-icon><Upload /></el-icon> 导入 Excel
              </el-dropdown-item>
              <el-dropdown-item @click="openExport">
                <el-icon><Download /></el-icon> 导出 Excel
              </el-dropdown-item>
            </el-dropdown-menu>
          </template>
        </el-dropdown>

        <el-button size="small" icon="Setting" @click="router.push(`/automation/${menuId}`)">自动化</el-button>
      </div>
    </div>

    <!-- ── 初始化骨架屏 ── -->
    <div v-if="templateStore.loading" class="page-loading">
      <el-skeleton :rows="6" animated style="padding: 20px 24px;" />
    </div>

    <!-- ── 无模板提示 ── -->
    <div v-else-if="!templateStore.template" class="no-template">
      <el-empty description="该模块尚未配置表单，请先进行表单设计">
        <el-button type="primary" @click="router.push(`/form-designer/${menuId}`)">
          前往表单设计器
        </el-button>
      </el-empty>
    </div>

    <!-- ── 数据表格 ── -->
    <template v-else>
      <div class="table-wrap">
        <!-- VXE 原生 toolbar：仅保留列设置图标，连接到下方 table -->
        <vxe-toolbar ref="toolbarRef" :refresh="false" :zoom="false" :custom="true" class="vxe-toolbar-bar" />
        <div class="vxe-table-wrap">
        <vxe-table
          ref="vxeTableRef"
          :id="`module-table-${menuId}`"
          :data="store.rows"
          :loading="store.loading"
          border="inner"
          stripe
          height="100%"
          size="small"
          :checkbox-config="{ highlight: true }"
          :sort-config="{ trigger: 'cell', defaultSort: { field: 'Id', order: 'desc' } }"
          :scroll-y="{ enabled: true }"
          :custom-config="{ storage: true }"
          @checkbox-change="onCheckboxChange"
          @checkbox-all="onCheckboxChange"
        >
          <!-- 全选复选框列 -->
          <vxe-column type="checkbox" width="46" fixed="left" />

          <!-- 序号列 -->
          <vxe-column type="seq" width="60" title="序号" fixed="left" />

          <!-- 动态数据列 -->
          <vxe-column
            v-for="field in sortedFields"
            :key="field.fieldName"
            :field="field.fieldName"
            :title="field.label"
            min-width="120"
            show-overflow
            sortable
          />

          <!-- 批次号列 -->
          <vxe-column title="批次" field="_BatchId" width="160" show-overflow>
            <template #default="{ row }">
              <el-tag v-if="row['_BatchId']" size="small" type="info" style="font-size:11px;">
                {{ row['_BatchId'] }}
              </el-tag>
              <el-tag v-else size="small" type="success" style="font-size:11px;">手动添加</el-tag>
            </template>
          </vxe-column>

          <!-- 操作列 -->
          <vxe-column title="操作" width="120" fixed="right">
            <template #default="{ row }">
              <el-button link type="primary" size="small" @click="openEdit(row)">编辑</el-button>
              <el-button
                link
                type="danger"
                size="small"
                :loading="deletingId === Number(row['Id'])"
                @click="handleDelete(row)"
              >
                删除
              </el-button>
            </template>
          </vxe-column>
        </vxe-table>
        </div>
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
      :submitting="formSubmitting"
      @submit="handleFormSubmit"
    />

    <!-- ── 高级筛选抽屉 ── -->
    <DataFilter
      v-model:visible="filterVisible"
      v-model="store.activeFilters"
      :fields="sortedFields"
      :schemes="store.filterSchemes"
      :scheme-saving="filterSchemeSaving"
      :scheme-updating="filterSchemeUpdating"
      :deleting-scheme-id="filterDeletingSchemeId"
      @apply="onFiltersApply"
      @save-scheme="onSaveScheme"
      @update-scheme="onUpdateScheme"
      @delete-scheme="onDeleteScheme"
      @load-scheme="store.applyScheme($event)"
    />

    <!-- ── 导入预览对话框 ── -->
    <ImportPreviewDialog
      v-model:visible="previewVisible"
      :rows="previewRows"
      :batch-id="previewBatchId"
      :fields="sortedFields"
      :confirming="confirming"
      @confirm="handleConfirmImport"
    />

    <!-- ── 历史批次对话框 ── -->
    <BatchHistoryDialog
      v-model:visible="batchHistoryVisible"
      :menu-id="menuId"
      @view-batch="handleViewBatch"
    />

    <!-- ── 模板选择对话框 ── -->
    <el-dialog v-model="templateSelectVisible" title="选择导入模板" width="400px">
      <el-select
        v-model="selectedTemplateConfigId"
        placeholder="请选择模板配置"
        filterable
        clearable
        style="width: 100%;"
      >
        <el-option label="全部字段（默认模板）" :value="undefined" />
        <el-option
          v-for="cfg in store.importTemplateConfigs"
          :key="cfg.id"
          :label="cfg.name"
          :value="cfg.id"
        />
      </el-select>
      <template #footer>
        <el-button @click="templateSelectVisible = false">取消</el-button>
        <el-button type="primary" @click="confirmTemplateDownload">下载</el-button>
      </template>
    </el-dialog>

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
        <el-button type="primary" :loading="exporting" @click="handleExport">导出 Excel</el-button>
      </template>
    </el-dialog>

    <!-- ── 导入模板配置管理对话框 ── -->
    <ImportTemplateConfigDialog
      v-model:visible="importTemplateDialogVisible"
      :menu-id="menuId"
      :fields="sortedFields"
      @change="store.fetchImportTemplateConfigs(menuId)"
    />

    <!-- ── 统一导入对话框 ── -->
    <ImportExcelDialog
      v-model:visible="importExcelDialogVisible"
      :menu-id="menuId"
      :fields="sortedFields"
      @preview="handleImportPreview"
    />

  </div>
</template>

<style scoped>
.module-page {
  height: 100%;
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
  flex-shrink: 0;
}

.header-right {
  display: flex;
  align-items: center;
  gap: 8px;
  flex-wrap: nowrap;
}

.page-title {
  font-size: 15px;
  font-weight: 600;
  color: #303133;
  white-space: nowrap;
}

.no-template {
  flex: 1;
  display: flex;
  align-items: center;
  justify-content: center;
}

.page-loading {
  flex: 1;
  background: #fff;
  overflow: hidden;
}

.table-wrap {
  flex: 1;
  overflow: hidden;
  padding: 12px 16px 0;
  display: flex;
  flex-direction: column;
}

.vxe-toolbar-bar {
  flex-shrink: 0;
  background: #fff;
  border: 1px solid #e4e7ed;
  border-bottom: none;
  padding: 0 8px;
}

.vxe-table-wrap {
  flex: 1;
  min-height: 0;
  overflow: hidden;
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
