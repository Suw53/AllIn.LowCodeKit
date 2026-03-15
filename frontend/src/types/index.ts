/**
 * 前端公共 TypeScript 类型定义
 */

/** API 通用分页查询参数 */
export interface PageQuery {
  page: number
  pageSize: number
  keyword?: string
}

/** API 通用分页响应 */
export interface PageResult<T> {
  total: number
  items: T[]
}

/** 后端健康检查响应 */
export interface HealthResult {
  status: string
  version: string
  timestamp: string
}

/** 全局配置项 */
export interface GlobalConfig {
  id: number
  category: string
  key: string
  value: string
  description?: string
}

/** 表单字段定义 */
export interface FormField {
  id: number
  templateId: number
  fieldName: string
  label: string
  fieldType: 'Text' | 'Select'
  /** 下拉选项，JSON 字符串数组，仅 Select 类型有效 */
  options?: string
  isRequired: boolean
  remark?: string
  columnOrder: number
  sort: number
  /** 列宽占比：1=半宽（占一列），2=全宽（跨两列），默认 1 */
  span: number
}

/** 表单模板 */
export interface FormTemplate {
  id: number
  menuId: number
  name: string
  codeLogic?: string
  createdAt: string
  updatedAt: string
  fields: FormField[]
}
