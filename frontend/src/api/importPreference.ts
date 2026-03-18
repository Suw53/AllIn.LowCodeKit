import http from './http'
import type { ImportPreference } from '@/types'

/** GET /api/menus/{menuId}/import-preference */
export const getImportPreference = (menuId: number) =>
  http.get<ImportPreference | null>(`/api/menus/${menuId}/import-preference`)

/** PUT /api/menus/{menuId}/import-preference */
export const saveImportPreference = (
  menuId: number,
  data: { useMappingEnabled: boolean; lastMappingConfigId: number | null }
) => http.put<ImportPreference>(`/api/menus/${menuId}/import-preference`, data)
