import http from './http'
import type { ExportPreference } from '@/types'

/**
 * 获取菜单的导出列偏好
 * GET /api/menus/{menuId}/export-preference
 */
export const getExportPreference = (menuId: number) =>
  http.get<ExportPreference | null>(`/api/menus/${menuId}/export-preference`)

/**
 * 保存导出列偏好（upsert）
 * PUT /api/menus/{menuId}/export-preference
 */
export const saveExportPreference = (menuId: number, selectedColumns: string[]) =>
  http.put<ExportPreference>(`/api/menus/${menuId}/export-preference`, {
    selectedColumns: JSON.stringify(selectedColumns)
  })
