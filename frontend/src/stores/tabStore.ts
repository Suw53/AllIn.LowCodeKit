// 页签管理 Store：维护顶部已打开页签列表
import { defineStore } from 'pinia'
import { ref } from 'vue'
import { useRouter } from 'vue-router'

export interface Tab {
  key: string        // 路由 path（唯一键）
  title: string      // 显示标题
  closeable: boolean // 首页不可关闭
}

export const useTabStore = defineStore('tab', () => {
  const router = useRouter()

  // 首页 Tab 始终存在
  const tabs = ref<Tab[]>([
    { key: '/home', title: '首页', closeable: false }
  ])
  const activeKey = ref('/home')

  /** 打开或激活一个 Tab */
  function openTab(path: string, title: string) {
    const existing = tabs.value.find(t => t.key === path)
    if (existing) {
      existing.title = title  // 更新标题（菜单重命名场景）
      activeKey.value = path
    } else {
      tabs.value.push({ key: path, title, closeable: true })
      activeKey.value = path
    }
  }

  /** 关闭一个 Tab，自动跳转到前一个或首页 */
  function closeTab(key: string) {
    const idx = tabs.value.findIndex(t => t.key === key)
    if (idx === -1) return

    tabs.value.splice(idx, 1)

    // 如果关闭的是当前激活的 Tab，跳转到前一个
    if (activeKey.value === key) {
      const next = tabs.value[Math.max(0, idx - 1)]
      activeKey.value = next.key
      router.push(next.key)
    }
  }

  /** 更新已有 Tab 的标题（模块名加载完成后调用） */
  function updateTitle(key: string, title: string) {
    const tab = tabs.value.find(t => t.key === key)
    if (tab) tab.title = title
  }

  /** 设置当前激活 Tab（不触发路由跳转） */
  function setActive(key: string) {
    activeKey.value = key
  }

  return { tabs, activeKey, openTab, closeTab, updateTitle, setActive }
})
