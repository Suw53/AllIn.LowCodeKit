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
