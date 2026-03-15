import http from './http'
import type { FormTemplate } from '@/types'

/** 字段传输对象 */
export interface FieldDto {
  fieldName: string
  label: string
  fieldType: 'Text' | 'Select'
  options?: string
  isRequired: boolean
  remark?: string
  columnOrder: number
}

/** 全量保存模板请求体 */
export interface SaveTemplateDto {
  name: string
  codeLogic?: string
  fields: FieldDto[]
}

/** 根据菜单Id查询模板（含字段，未创建时返回 null） */
export const getTemplateByMenu = (menuId: number) =>
  http.get<FormTemplate | null>(`/api/form-templates/by-menu/${menuId}`)

/** 创建新模板 */
export const createTemplate = (menuId: number, name: string) =>
  http.post<FormTemplate>('/api/form-templates', { menuId, name })

/** 全量保存模板（替换所有字段） */
export const saveTemplate = (id: number, dto: SaveTemplateDto) =>
  http.put<FormTemplate>(`/api/form-templates/${id}`, dto)

/** 导出模板数据 */
export const exportTemplate = (id: number) =>
  http.get<FormTemplate>(`/api/form-templates/${id}/export`)

/** 导入模板（覆盖该菜单现有模板） */
export const importTemplate = (menuId: number, dto: SaveTemplateDto & { name: string }) =>
  http.post<FormTemplate>(`/api/form-templates/import/${menuId}`, dto)

/** 删除模板 */
export const deleteTemplate = (id: number) =>
  http.delete(`/api/form-templates/${id}`)
