// 主题配置 Store：管理 Element Plus CSS 变量，支持持久化
import { defineStore } from 'pinia'
import { ref } from 'vue'
import { getConfigs, setConfig } from '@/api/globalConfig'

/** 主题颜色变量定义 */
export interface ThemeVar {
  key: string        // CSS 变量名，如 --el-color-primary
  label: string      // 显示名称
  defaultValue: string
}

export const THEME_VARS: ThemeVar[] = [
  { key: '--el-color-primary',   label: '主色调',    defaultValue: '#409eff' },
  { key: '--el-color-success',   label: '成功色',    defaultValue: '#67c23a' },
  { key: '--el-color-warning',   label: '警告色',    defaultValue: '#e6a23c' },
  { key: '--el-color-danger',    label: '危险色',    defaultValue: '#f56c6c' },
  { key: '--el-bg-color',        label: '背景色',    defaultValue: '#ffffff' },
  { key: '--el-text-color-primary', label: '主文字色', defaultValue: '#303133' },
]

export const useThemeStore = defineStore('theme', () => {
  // 当前主题值 key→value
  const colors = ref<Record<string, string>>({})

  /** 从后端加载主题配置并应用到页面 */
  async function loadTheme() {
    try {
      const configs = await getConfigs('theme')
      const map: Record<string, string> = {}
      for (const v of THEME_VARS) {
        const found = configs.find(c => c.key === v.key)
        map[v.key] = found?.value ?? v.defaultValue
      }
      colors.value = map
      applyToDocument(map)
    } catch {
      // 后端不可用时使用默认值
      const map: Record<string, string> = {}
      for (const v of THEME_VARS) map[v.key] = v.defaultValue
      colors.value = map
    }
  }

  /** 实时预览：更新单个 CSS 变量（不保存） */
  function previewColor(key: string, value: string) {
    colors.value[key] = value
    document.documentElement.style.setProperty(key, value)
  }

  /** 保存全部主题配置到后端 */
  async function saveTheme() {
    await Promise.all(
      Object.entries(colors.value).map(([key, value]) =>
        setConfig('theme', key, value)
      )
    )
  }

  /** 重置为默认主题 */
  function resetToDefault() {
    const map: Record<string, string> = {}
    for (const v of THEME_VARS) map[v.key] = v.defaultValue
    colors.value = map
    applyToDocument(map)
  }

  /** 将主题值写入 document CSS 变量 */
  function applyToDocument(map: Record<string, string>) {
    for (const [key, value] of Object.entries(map)) {
      document.documentElement.style.setProperty(key, value)
    }
  }

  return { colors, loadTheme, previewColor, saveTheme, resetToDefault }
})
