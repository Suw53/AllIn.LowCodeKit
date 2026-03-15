import { defineStore } from 'pinia'
import { ref } from 'vue'
import { ElMessage } from 'element-plus'
import {
  queryData, createRow, updateRow, deleteRow,
  downloadTemplate, exportExcel, getBatchIds, confirmImport
} from '@/api/data'
import { getFilterSchemes, createFilterScheme, deleteFilterScheme } from '@/api/filterScheme'
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
  const currentBatchId = ref<string>('latest') // 默认显示最新批次

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

  /** 加载批次号列表并设置默认批次 */
  async function fetchBatchIds(menuId: number) {
    try {
      const ids = await getBatchIds(menuId)
      batchIds.value = ids
      // 若有批次，默认选择最新批次（"latest"语义）；若无批次，显示全部
      currentBatchId.value = ids.length > 0 ? 'latest' : ''
    } catch {
      batchIds.value = []
      currentBatchId.value = ''
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

  /** 加载筛选方案列表 */
  async function fetchFilterSchemes(menuId: number) {
    filterSchemes.value = await getFilterSchemes(menuId)
  }

  /** 保存当前筛选条件为方案 */
  async function saveFilterScheme(menuId: number, name: string) {
    const config = JSON.stringify(activeFilters.value)
    const scheme = await createFilterScheme(menuId, name, config)
    filterSchemes.value.unshift(scheme)
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
    // 刷新批次列表并切换到新批次
    await fetchBatchIds(menuId)
    currentBatchId.value = batchId
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
    currentBatchId.value = 'latest'
    currentMenuId.value = null
  }

  return {
    rows, total, loading, page, pageSize, keyword, activeFilters,
    filterSchemes, exportColumns, currentMenuId,
    batchIds, currentBatchId,
    fetchData, addRow, editRow, removeRow,
    fetchBatchIds,
    fetchFilterSchemes, saveFilterScheme, removeFilterScheme, applyScheme,
    fetchExportPreference, saveExportColumns,
    fetchTemplate, exportToExcel, importConfirm,
    reset
  }
})
