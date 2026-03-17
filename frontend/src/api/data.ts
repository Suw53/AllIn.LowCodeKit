import http, { downloadFile } from './http'
import type { DataRow, FilterCondition, PageResult } from '@/types'

export interface DataQueryParams {
  page?: number
  pageSize?: number
  keyword?: string
  filters?: FilterCondition[]
  batchId?: string
}

export interface PreviewRow {
  rowIndex: number
  data: Record<string, string | null>
  status: 'ok' | 'error'
  errors: string[]
}

export interface ImportPreviewResult {
  rows: PreviewRow[]
  successCount: number
  errorCount: number
}

export interface BatchStat {
  batchId: string
  count: number
}

/**
 * 分页查询模块数据
 * GET /api/menus/{menuId}/data
 */
export const queryData = (menuId: number, params: DataQueryParams = {}) => {
  const { page = 1, pageSize = 100, keyword, filters, batchId } = params
  return http.get<PageResult<DataRow>>(`/api/menus/${menuId}/data`, {
    params: {
      page,
      pageSize,
      keyword: keyword || undefined,
      filters: filters?.length ? JSON.stringify(filters) : undefined,
      batchId: batchId || undefined
    }
  })
}

/**
 * 获取批次统计信息（批次号 + 行数）
 * GET /api/menus/{menuId}/data/batch-stats
 */
export const getBatchStats = (menuId: number) =>
  http.get<BatchStat[]>(`/api/menus/${menuId}/data/batch-stats`)

/**
 * 批量删除
 * DELETE /api/menus/{menuId}/data/batch
 */
export const batchDeleteRows = (menuId: number, ids: number[]) =>
  http.delete<{ deleted: number }>(`/api/menus/${menuId}/data/batch`, { data: ids })

/**
 * 获取批次号列表（倒序）
 * GET /api/menus/{menuId}/data/batches
 */
export const getBatchIds = (menuId: number) =>
  http.get<string[]>(`/api/menus/${menuId}/data/batches`)

/**
 * 预览导入数据（解析+校验，不保存）
 * POST /api/menus/{menuId}/data/import/preview
 */
export const previewImport = (menuId: number, file: File) => {
  const form = new FormData()
  form.append('file', file)
  return http.post<ImportPreviewResult>(
    `/api/menus/${menuId}/data/import/preview`,
    form,
    { headers: { 'Content-Type': 'multipart/form-data' } }
  )
}

/**
 * 确认导入（保存有效行）
 * POST /api/menus/{menuId}/data/import/confirm
 */
export const confirmImport = (
  menuId: number,
  batchId: string,
  rows: Record<string, string | null>[]
) => http.post<{ imported: number; batchId: string }>(
  `/api/menus/${menuId}/data/import/confirm`,
  { batchId, rows }
)

/**
 * 新增一条数据记录
 * POST /api/menus/{menuId}/data
 */
export const createRow = (menuId: number, data: Record<string, string | null>) =>
  http.post<{ id: number }>(`/api/menus/${menuId}/data`, data)

/**
 * 更新一条数据记录
 * PUT /api/menus/{menuId}/data/{rowId}
 */
export const updateRow = (menuId: number, rowId: number, data: Record<string, string | null>) =>
  http.put(`/api/menus/${menuId}/data/${rowId}`, data)

/**
 * 删除一条数据记录
 * DELETE /api/menus/{menuId}/data/{rowId}
 */
export const deleteRow = (menuId: number, rowId: number) =>
  http.delete(`/api/menus/${menuId}/data/${rowId}`)

/**
 * 下载 Excel 导入模板
 * GET /api/menus/{menuId}/data/template
 */
export const downloadTemplate = (menuId: number) =>
  downloadFile(`/api/menus/${menuId}/data/template`, undefined, '导入模板.xlsx')

/**
 * 导出当前筛选结果为 Excel（全量）
 * GET /api/menus/{menuId}/data/export
 */
export const exportExcel = (
  menuId: number,
  params: { keyword?: string; filters?: FilterCondition[]; columns?: string[]; batchId?: string }
) =>
  downloadFile(`/api/menus/${menuId}/data/export`, {
    keyword: params.keyword || undefined,
    filters: params.filters?.length ? JSON.stringify(params.filters) : undefined,
    columns: params.columns?.length ? JSON.stringify(params.columns) : undefined,
    batchId: params.batchId || undefined
  }, '导出数据.xlsx')
