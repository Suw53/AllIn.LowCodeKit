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
  span: number
}

/** 创建或全量更新模板请求体（upsert / 导入通用） */
export interface SaveTemplateDto {
  name: string
  codeLogic?: string
  fields: FieldDto[]
}

/**
 * 获取指定菜单的表单模板（含字段），不存在返回 null
 * GET /api/menus/{menuId}/form-template
 */
export const getTemplateByMenu = (menuId: number) =>
  http.get<FormTemplate | null>(`/api/menus/${menuId}/form-template`)

/**
 * 创建或全量更新表单模板（upsert）
 * PUT /api/menus/{menuId}/form-template
 */
export const saveTemplateForMenu = (menuId: number, dto: SaveTemplateDto) =>
  http.put<FormTemplate>(`/api/menus/${menuId}/form-template`, dto)

/**
 * 获取模板完整数据（用于导出下载 JSON）
 * GET /api/form-templates/{id}
 */
export const getTemplateById = (id: number) =>
  http.get<FormTemplate>(`/api/form-templates/${id}`)

/**
 * 删除表单模板
 * DELETE /api/form-templates/{id}
 */
export const deleteTemplate = (id: number) =>
  http.delete(`/api/form-templates/${id}`)
