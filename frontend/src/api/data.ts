import http from './http'
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
