import { defineStore } from 'pinia'
import { ref } from 'vue'
import { ElMessage } from 'element-plus'
import http from '@/api/http'

export interface MenuItem {
  id: number
  parentId: number | null
  name: string
  icon?: string
  sort: number
  isSystem: boolean
  children: MenuItem[]
}

export const useMenuStore = defineStore('menu', () => {
  const menuList = ref<MenuItem[]>([])
  const loading = ref(false)

  /** 加载完整菜单树 */
  async function fetchMenus() {
    loading.value = true
    try {
      menuList.value = await http.get('/api/menus/tree')
    } catch {
      ElMessage.error('无法连接到后端服务，请确认后端已启动（端口 5000）')
    } finally {
      loading.value = false
    }
  }

  return { menuList, loading, fetchMenus }
})
