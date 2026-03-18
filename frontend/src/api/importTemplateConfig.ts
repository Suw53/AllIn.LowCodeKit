import http from './http'
import type { ImportTemplateConfig } from '@/types'

/**
 * 获取指定菜单的所有导入模板配置
 * GET /api/menus/{menuId}/import-template-configs
 */
export const getImportTemplateConfigs = (menuId: number) =>
  http.get<ImportTemplateConfig[]>(`/api/menus/${menuId}/import-template-configs`)

/**
 * 新建导入模板配置
 * POST /api/menus/{menuId}/import-template-configs
 */
export const createImportTemplateConfig = (
  menuId: number,
  data: { name: string; fieldNames: string }
) => http.post<ImportTemplateConfig>(`/api/menus/${menuId}/import-template-configs`, data)

/**
 * 更新导入模板配置
 * PUT /api/import-template-configs/{id}
 */
export const updateImportTemplateConfig = (
  id: number,
  data: { name: string; fieldNames: string }
) => http.put<ImportTemplateConfig>(`/api/import-template-configs/${id}`, data)

/**
 * 删除导入模板配置
 * DELETE /api/import-template-configs/{id}
 */
export const deleteImportTemplateConfig = (id: number) =>
  http.delete(`/api/import-template-configs/${id}`)
