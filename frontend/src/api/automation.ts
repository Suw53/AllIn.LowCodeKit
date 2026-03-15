// 自动化配置 API 封装
import http from './http'

export interface AutomationConfig {
  id: number
  menuId: number
  name: string
  scriptCode: string
  loginConfigId: number | null
}

export interface RunResult {
  success: boolean
  output: string
  error: string | null
}

/** 获取菜单自动化配置 */
export const getAutomationConfig = (menuId: number) =>
  http.get<AutomationConfig>(`/api/menus/${menuId}/automation`)

/** 保存自动化配置（upsert） */
export const saveAutomationConfig = (
  menuId: number,
  payload: { name: string; scriptCode: string; loginConfigId: number | null }
) => http.put<AutomationConfig>(`/api/menus/${menuId}/automation`, payload)

/** 执行自动化脚本 */
export const runAutomation = (
  menuId: number,
  payload: { scriptCode: string; cdpAddress: string; loginConfigId: number | null }
) => http.post<RunResult>(`/api/menus/${menuId}/automation/run`, payload, { timeout: 360000 })
