import http, { downloadFile } from './http'
import type { DataRow, FilterCondition, PageResult } from '@/types'

export interface DataQueryParams {
  page?: number
  pageSize?: number
  keyword?: string
  filters?: FilterCondition[]
}

/**
 * 分页查询模块数据
 * GET /api/menus/{menuId}/data
 */
export const queryData = (menuId: number, params: DataQueryParams = {}) => {
  const { page = 1, pageSize = 100, keyword, filters } = params
  return http.get<PageResult<DataRow>>(`/api/menus/${menuId}/data`, {
    params: {
      page,
      pageSize,
      keyword: keyword || undefined,
      filters: filters?.length ? JSON.stringify(filters) : undefined
    }
  })
}

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
  params: { keyword?: string; filters?: FilterCondition[]; columns?: string[] }
) =>
  downloadFile(`/api/menus/${menuId}/data/export`, {
    keyword: params.keyword || undefined,
    filters: params.filters?.length ? JSON.stringify(params.filters) : undefined,
    columns: params.columns?.length ? JSON.stringify(params.columns) : undefined
  }, '导出数据.xlsx')

/**
 * 从 Excel 文件批量导入数据
 * POST /api/menus/{menuId}/data/import
 */
export const importData = (menuId: number, file: File) => {
  const form = new FormData()
  form.append('file', file)
  return http.post<{ imported: number; errors: string[] }>(
    `/api/menus/${menuId}/data/import`,
    form,
    { headers: { 'Content-Type': 'multipart/form-data' } }
  )
}
