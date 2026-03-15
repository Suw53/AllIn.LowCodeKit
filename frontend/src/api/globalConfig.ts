// 全局配置 API 封装
import http from './http'

export interface GlobalConfig {
  id: number
  category: string
  key: string
  value: string | null
  description: string | null
}

/** 获取指定分类的所有配置 */
export const getConfigs = (category?: string) =>
  http.get<GlobalConfig[]>('/api/global-configs', { params: { category } })

/** 设置单条配置（upsert） */
export const setConfig = (category: string, key: string, value: string | null, description?: string) =>
  http.put<GlobalConfig>(`/api/global-configs/${category}/${key}`, { value, description })

/** 删除单条配置 */
export const deleteConfig = (category: string, key: string) =>
  http.delete(`/api/global-configs/${category}/${key}`)
