import { defineStore } from 'pinia'
import { ref } from 'vue'
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
    } finally {
      loading.value = false
    }
  }

  return { menuList, loading, fetchMenus }
})
