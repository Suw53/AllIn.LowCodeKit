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

// ────────── Phase 4：列表与数据管理 ──────────

/** 动态数据行：Id + CreatedAt + UpdatedAt + 各字段值 */
export type DataRow = Record<string, unknown>

/** 字段级筛选条件 */
export interface FilterCondition {
  field: string
  /** eq：精确匹配；contains：模糊匹配 */
  op: 'eq' | 'contains'
  value: string
}

/** 高级筛选方案 */
export interface FilterScheme {
  id: number
  menuId: number
  name: string
  /** FilterCondition[] 序列化的 JSON */
  config: string
  createdAt: string
}

/** 导出列偏好 */
export interface ExportPreference {
  id: number
  menuId: number
  /** 已选中导出列名 JSON 数组 */
  selectedColumns: string
  /** 列表可见列名 JSON 数组，null 表示显示全部列 */
  visibleColumns: string | null
  updatedAt: string
}

