import http from './http'

export interface MenuPayload {
  name: string
  icon?: string
  parentId?: number | null
}

/** 获取完整菜单树 */
export const getMenuTree = () =>
  http.get('/api/menus')

/** 新增菜单：parentId 为 null/undefined 时创建一级菜单，否则创建二级菜单 */
export const addMenu = (payload: MenuPayload) =>
  http.post('/api/menus', payload)

/** 修改菜单名称/图标 */
export const updateMenu = (id: number, payload: MenuPayload) =>
  http.put(`/api/menus/${id}`, payload)

/** 删除菜单 */
export const deleteMenu = (id: number) =>
  http.delete(`/api/menus/${id}`)
