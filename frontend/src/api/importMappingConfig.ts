import http from './http'
import type { ImportMappingConfig } from '@/types'

/**
 * 获取指定菜单的所有映射配置
 * GET /api/menus/{menuId}/import-mapping-configs
 */
export const getImportMappingConfigs = (menuId: number) =>
  http.get<ImportMappingConfig[]>(`/api/menus/${menuId}/import-mapping-configs`)

/**
 * 新建映射配置
 * POST /api/menus/{menuId}/import-mapping-configs
 */
export const createImportMappingConfig = (
  menuId: number,
  data: { name: string; mappings: string }
) => http.post<ImportMappingConfig>(`/api/menus/${menuId}/import-mapping-configs`, data)

/**
 * 更新映射配置
 * PUT /api/import-mapping-configs/{id}
 */
export const updateImportMappingConfig = (
  id: number,
  data: { name: string; mappings: string }
) => http.put<ImportMappingConfig>(`/api/import-mapping-configs/${id}`, data)

/**
 * 删除映射配置
 * DELETE /api/import-mapping-configs/{id}
 */
export const deleteImportMappingConfig = (id: number) =>
  http.delete(`/api/import-mapping-configs/${id}`)
