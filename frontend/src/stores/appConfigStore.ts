// 应用配置 Store：管理网站标题、Logo、Favicon 等
import { defineStore } from 'pinia'
import { ref } from 'vue'
import { getConfigs, setConfig } from '@/api/globalConfig'

export interface AppConfig {
  title: string
  sidebarName: string
  logo: string
  favicon: string
}

export const useAppConfigStore = defineStore('appConfig', () => {
  const config = ref<AppConfig>({
    title: 'AllIn LowCode Kit',
    sidebarName: 'AllIn LowCode Kit',
    logo: '',
    favicon: ''
  })

  /** 从后端加载应用配置 */
  async function loadAppConfig() {
    try {
      const configs = await getConfigs('app')
      config.value.title = configs.find(c => c.key === 'title')?.value || 'AllIn LowCode Kit'
      config.value.sidebarName = configs.find(c => c.key === 'sidebarName')?.value || 'AllIn LowCode Kit'
      config.value.logo = configs.find(c => c.key === 'logo')?.value || ''
      config.value.favicon = configs.find(c => c.key === 'favicon')?.value || ''

      console.log('应用配置已加载:', {
        title: config.value.title,
        sidebarName: config.value.sidebarName,
        logoLength: config.value.logo?.length || 0,
        faviconLength: config.value.favicon?.length || 0
      })

      applyTitle()
      applyFavicon()
    } catch (error) {
      console.error('加载应用配置失败:', error)
      // 加载失败使用默认值
    }
  }

  /** 保存应用配置到后端 */
  async function saveAppConfig() {
    console.log('保存应用配置:', {
      title: config.value.title,
      sidebarName: config.value.sidebarName,
      logoLength: config.value.logo?.length || 0,
      faviconLength: config.value.favicon?.length || 0
    })

    await Promise.all([
      setConfig('app', 'title', config.value.title, '网站标题'),
      setConfig('app', 'sidebarName', config.value.sidebarName, '侧边栏名称'),
      setConfig('app', 'logo', config.value.logo, 'Logo图片'),
      setConfig('app', 'favicon', config.value.favicon, 'Favicon图标')
    ])

    console.log('应用配置已保存到后端')
    applyTitle()
    applyFavicon()
  }

  /** 应用标题到页面 */
  function applyTitle() {
    document.title = config.value.title
  }

  /** 应用 Favicon 到页面 */
  function applyFavicon() {
    let link = document.querySelector("link[rel~='icon']") as HTMLLinkElement
    if (!config.value.favicon) {
      if (link) {
        link.href = '/favicon.svg'
      }
      return
    }

    if (!link) {
      link = document.createElement('link')
      link.rel = 'icon'
      document.head.appendChild(link)
    }
    link.href = config.value.favicon
  }

  return { config, loadAppConfig, saveAppConfig, applyTitle, applyFavicon }
})
