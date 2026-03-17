import http from './http'
import type { FilterScheme } from '@/types'

/**
 * 获取菜单的所有筛选方案
 * GET /api/menus/{menuId}/filter-schemes
 */
export const getFilterSchemes = (menuId: number) =>
  http.get<FilterScheme[]>(`/api/menus/${menuId}/filter-schemes`)

/**
 * 保存一个筛选方案
 * POST /api/menus/{menuId}/filter-schemes
 */
export const createFilterScheme = (menuId: number, name: string, config: string) =>
  http.post<FilterScheme>(`/api/menus/${menuId}/filter-schemes`, { name, config })

/**
 * 更新筛选方案（名称和条件）
 * PUT /api/filter-schemes/{id}
 */
export const updateFilterScheme = (id: number, name: string, config: string) =>
  http.put<FilterScheme>(`/api/filter-schemes/${id}`, { name, config })

/**
 * 删除筛选方案
 * DELETE /api/filter-schemes/{id}
 */
export const deleteFilterScheme = (id: number) =>
  http.delete(`/api/filter-schemes/${id}`)
