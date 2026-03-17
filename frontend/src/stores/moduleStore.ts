import { defineStore } from 'pinia'
import { ref } from 'vue'
import { ElMessage } from 'element-plus'
import {
  queryData, createRow, updateRow, deleteRow,
  downloadTemplate, exportExcel, getBatchIds, confirmImport, batchDeleteRows
} from '@/api/data'
import { getFilterSchemes, createFilterScheme, updateFilterScheme as updateFilterSchemeApi, deleteFilterScheme } from '@/api/filterScheme'
import { getExportPreference, saveExportPreference } from '@/api/exportPreference'
import type { DataRow, FilterCondition, FilterScheme } from '@/types'

export const useModuleStore = defineStore('module', () => {
  // ────────── 数据列表状态 ──────────
  const rows = ref<DataRow[]>([])
  const total = ref(0)
  const loading = ref(false)
  const page = ref(1)
  const pageSize = ref(100)
  const keyword = ref('')
  const activeFilters = ref<FilterCondition[]>([])

  // ────────── 批次号 ──────────
  const batchIds = ref<string[]>([])
  const currentBatchId = ref<string>('') // 默认显示全部数据

  // ────────── 筛选方案状态 ──────────
  const filterSchemes = ref<FilterScheme[]>([])

  // ────────── 导出列偏好 ──────────
  const exportColumns = ref<string[]>([])

  // ────────── 当前菜单Id ──────────
  const currentMenuId = ref<number | null>(null)

  /** 加载数据列表 */
  async function fetchData(menuId: number) {
    loading.value = true
    try {
      const result = await queryData(menuId, {
        page: page.value,
        pageSize: pageSize.value,
        keyword: keyword.value || undefined,
        filters: activeFilters.value.length ? activeFilters.value : undefined,
        batchId: currentBatchId.value || undefined
      })
      rows.value = result.items
      total.value = result.total
    } catch (e: unknown) {
      ElMessage.error(e instanceof Error ? e.message : '加载数据失败')
    } finally {
      loading.value = false
    }
  }

  /** 加载批次号列表 */
  async function fetchBatchIds(menuId: number) {
    try {
      const ids = await getBatchIds(menuId)
      batchIds.value = ids
    } catch {
      batchIds.value = []
    }
  }

  /** 新增一行 */
  async function addRow(menuId: number, data: Record<string, string | null>) {
    await createRow(menuId, data)
    await fetchData(menuId)
  }

  /** 更新一行 */
  async function editRow(menuId: number, rowId: number, data: Record<string, string | null>) {
    await updateRow(menuId, rowId, data)
    await fetchData(menuId)
  }

  /** 删除一行 */
  async function removeRow(menuId: number, rowId: number) {
    await deleteRow(menuId, rowId)
    await fetchData(menuId)
  }

  /** 批量删除选中行 */
  async function batchRemoveRows(menuId: number, ids: number[]) {
    await batchDeleteRows(menuId, ids)
    await fetchData(menuId)
  }

  /** 加载筛选方案列表 */
  async function fetchFilterSchemes(menuId: number) {
    filterSchemes.value = await getFilterSchemes(menuId)
  }

  /** 保存当前筛选条件为方案 */
  async function saveFilterScheme(menuId: number, name: string, conditions: FilterCondition[]) {
    const config = JSON.stringify(conditions)
    const scheme = await createFilterScheme(menuId, name, config)
    filterSchemes.value.unshift(scheme)
  }

  /** 更新已有筛选方案（名称和条件） */
  async function updateFilterScheme(id: number, name: string, conditions: FilterCondition[]) {
    const config = JSON.stringify(conditions)
    const updated = await updateFilterSchemeApi(id, name, config)
    const idx = filterSchemes.value.findIndex(s => s.id === id)
    if (idx !== -1) filterSchemes.value[idx] = updated
  }

  /** 删除筛选方案 */
  async function removeFilterScheme(id: number) {
    await deleteFilterScheme(id)
    filterSchemes.value = filterSchemes.value.filter(s => s.id !== id)
  }

  /** 应用某个筛选方案（只更新 activeFilters，不触发查询） */
  function applyScheme(scheme: FilterScheme) {
    try {
      activeFilters.value = JSON.parse(scheme.config) as FilterCondition[]
    } catch {
      activeFilters.value = []
    }
  }

  /** 加载导出列偏好 */
  async function fetchExportPreference(menuId: number) {
    const pref = await getExportPreference(menuId)
    if (pref) {
      try {
        exportColumns.value = JSON.parse(pref.selectedColumns) as string[]
      } catch {
        exportColumns.value = []
      }
    } else {
      exportColumns.value = []
    }
  }

  /** 保存导出列偏好 */
  async function saveExportColumns(menuId: number, columns: string[]) {
    exportColumns.value = columns
    await saveExportPreference(menuId, columns)
  }

  /** 下载导入模板 */
  async function fetchTemplate(menuId: number) {
    await downloadTemplate(menuId)
  }

  /** 导出当前筛选结果为 Excel */
  async function exportToExcel(menuId: number, columns: string[]) {
    await exportExcel(menuId, {
      keyword: keyword.value || undefined,
      filters: activeFilters.value.length ? activeFilters.value : undefined,
      columns,
      batchId: currentBatchId.value || undefined
    })
  }

  /** 确认导入（保存预览中有效的行） */
  async function importConfirm(menuId: number, batchId: string, validRows: Record<string, string | null>[]) {
    const result = await confirmImport(menuId, batchId, validRows)
    // 刷新批次列表，保持全部数据视图
    await fetchBatchIds(menuId)
    await fetchData(menuId)
    return result
  }

  /** 切换菜单时重置状态 */
  function reset() {
    rows.value = []
    total.value = 0
    page.value = 1
    keyword.value = ''
    activeFilters.value = []
    filterSchemes.value = []
    exportColumns.value = []
    batchIds.value = []
    currentBatchId.value = ''
    currentMenuId.value = null
  }

  return {
    rows, total, loading, page, pageSize, keyword, activeFilters,
    filterSchemes, exportColumns, currentMenuId,
    batchIds, currentBatchId,
    fetchData, addRow, editRow, removeRow, batchRemoveRows,
    fetchBatchIds,
    fetchFilterSchemes, saveFilterScheme, updateFilterScheme, removeFilterScheme, applyScheme,
    fetchExportPreference, saveExportColumns,
    fetchTemplate, exportToExcel, importConfirm,
    reset
  }
})
