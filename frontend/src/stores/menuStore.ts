import { defineStore } from 'pinia'
import { ref } from 'vue'
import { ElMessage } from 'element-plus'
import * as menuApi from '@/api/menu'

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
      menuList.value = await menuApi.getMenuTree() as MenuItem[]
    } catch {
      ElMessage.error('无法连接到后端服务，请确认后端已启动（端口 5000）')
    } finally {
      loading.value = false
    }
  }

  /** 新增一级菜单 */
  async function addLevel1(name: string, icon?: string) {
    await menuApi.addMenu({ name, icon, parentId: null })
    await fetchMenus()
  }

  /** 新增二级菜单，返回后端新建的菜单对象 */
  async function addLevel2(parentId: number, name: string, icon?: string): Promise<MenuItem> {
    const result = await menuApi.addMenu({ name, icon, parentId }) as MenuItem
    await fetchMenus()
    return result
  }

  /** 修改菜单名称/图标 */
  async function updateMenu(id: number, name: string, icon?: string) {
    await menuApi.updateMenu(id, { name, icon })
    await fetchMenus()
  }

  /** 删除菜单 */
  async function deleteMenu(id: number) {
    await menuApi.deleteMenu(id)
    await fetchMenus()
  }

  return { menuList, loading, fetchMenus, addLevel1, addLevel2, updateMenu, deleteMenu }
})
