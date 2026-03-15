import http from './http'

export interface MenuPayload {
  name: string
  icon?: string
  parentId?: number
}

/** 获取完整菜单树 */
export const getMenuTree = () =>
  http.get('/api/menus/tree')

/** 新增一级菜单 */
export const addLevel1Menu = (payload: MenuPayload) =>
  http.post('/api/menus/level1', payload)

/** 新增二级菜单 */
export const addLevel2Menu = (payload: MenuPayload) =>
  http.post('/api/menus/level2', payload)

/** 修改菜单名称 */
export const updateMenu = (id: number, payload: MenuPayload) =>
  http.put(`/api/menus/${id}`, payload)

/** 删除菜单 */
export const deleteMenu = (id: number) =>
  http.delete(`/api/menus/${id}`)
