<script setup lang="ts">
import { ref, onMounted, onUnmounted } from 'vue'
import { useRouter } from 'vue-router'
import { ElMessageBox, ElMessage } from 'element-plus'
import { useMenuStore, type MenuItem } from '@/stores/menuStore'
import { useTabStore } from '@/stores/tabStore'
import ContextMenu from '@/components/ContextMenu.vue'
import IconPicker from '@/components/IconPicker.vue'

const router = useRouter()
const menuStore = useMenuStore()
const tabStore = useTabStore()
const collapsed = ref(false)
const sidebarRef = ref<HTMLElement>()

// ────────── 右键菜单 ──────────
interface CtxItem {
  label: string
  icon?: string
  danger?: boolean
  handler: () => void
}
const ctxMenu = ref({ visible: false, x: 0, y: 0, items: [] as CtxItem[] })

function showCtxMenu(e: MouseEvent, items: CtxItem[]) {
  if (!items.length) return
  ctxMenu.value = {
    visible: true,
    x: Math.min(e.clientX, window.innerWidth - 160),
    y: Math.min(e.clientY, window.innerHeight - items.length * 36 - 16),
    items,
  }
}

/** 从右键事件的目标元素逆推对应的菜单数据 */
function resolveTarget(target: HTMLElement): { menu: MenuItem; level: 1 | 2 } | null {
  // 1. 命中带 data-ctx-id 的 span（level-1 name span 或 level-2 name span）
  const dataEl = target.closest('[data-ctx-id]') as HTMLElement | null
  if (dataEl) {
    const id = Number(dataEl.dataset.ctxId)
    const level = Number(dataEl.dataset.ctxLevel) as 1 | 2
    if (level === 1) {
      const m = menuStore.menuList.find(m => m.id === id)
      return m ? { menu: m, level: 1 } : null
    }
    for (const parent of menuStore.menuList) {
      const child = parent.children.find(c => c.id === id)
      if (child) return { menu: child, level: 2 }
    }
    return null
  }

  // 2. 命中 el-sub-menu__title（展开箭头区域，不在 span 内）
  //    用 querySelector 从 title 容器里找到 span 取 id
  const titleEl = target.closest('.el-sub-menu__title')
  if (titleEl) {
    const span = titleEl.querySelector('[data-ctx-level="1"]') as HTMLElement | null
    if (span) {
      const m = menuStore.menuList.find(m => m.id === Number(span.dataset.ctxId))
      return m ? { menu: m, level: 1 } : null
    }
    return null
  }

  // 3. 命中 el-menu-item 内边距区域（不在 span 内）
  const itemEl = target.closest('.el-menu-item')
  if (itemEl) {
    const span = itemEl.querySelector('[data-ctx-level="2"]') as HTMLElement | null
    if (span) {
      const id = Number(span.dataset.ctxId)
      for (const parent of menuStore.menuList) {
        const child = parent.children.find(c => c.id === id)
        if (child) return { menu: child, level: 2 }
      }
    }
    return null
  }

  return null
}

/** document 级 contextmenu 监听，完全绕过 Element Plus 内部事件处理 */
function handleDocContextMenu(e: MouseEvent) {
  if (!sidebarRef.value?.contains(e.target as Node)) {
    // 右键命中侧边栏外：关闭已打开的菜单，不阻止浏览器默认行为
    ctxMenu.value.visible = false
    return
  }
  e.preventDefault()

  const result = resolveTarget(e.target as HTMLElement)

  if (!result) {
    // 空白区域 → 新增一级菜单
    showCtxMenu(e, [
      { label: '新增一级菜单', icon: 'Plus', handler: () => openDialog('add-level1', null) },
    ])
    return
  }

  if (result.level === 1) {
    const items: CtxItem[] = [
      { label: '新增子菜单', icon: 'Plus', handler: () => openDialog('add-level2', result.menu) },
    ]
    if (!result.menu.isSystem) {
      items.push({ label: '重命名', icon: 'Edit', handler: () => openDialog('rename', result.menu) })
      items.push({ label: '删除', icon: 'Delete', danger: true, handler: () => doDelete(result.menu) })
    }
    showCtxMenu(e, items)
  } else {
    if (result.menu.isSystem) return
    showCtxMenu(e, [
      { label: '表单设计', icon: 'Setting', handler: () => router.push(`/form-designer/${result.menu.id}`) },
      { label: '重命名', icon: 'Edit', handler: () => openDialog('rename', result.menu) },
      { label: '删除', icon: 'Delete', danger: true, handler: () => doDelete(result.menu) },
    ])
  }
}

onMounted(() => {
  document.addEventListener('contextmenu', handleDocContextMenu)
  menuStore.fetchMenus()
})
onUnmounted(() => {
  document.removeEventListener('contextmenu', handleDocContextMenu)
})

// ────────── 对话框 ──────────
type DialogMode = 'add-level1' | 'add-level2' | 'rename'
const dialog = ref({
  visible: false,
  title: '',
  inputValue: '',
  iconValue: '',
  submitting: false,
  mode: 'add-level1' as DialogMode,
  target: null as MenuItem | null,
})

function openDialog(mode: DialogMode, menu: MenuItem | null) {
  dialog.value = {
    visible: true,
    mode,
    target: menu,
    title:
      mode === 'add-level1' ? '新增一级菜单'
      : mode === 'add-level2' ? `在「${menu!.name}」下新增子菜单`
      : '重命名',
    inputValue: mode === 'rename' ? menu!.name : '',
    iconValue: mode === 'rename' ? (menu!.icon ?? '') : '',
    submitting: false,
  }
}

async function submitDialog() {
  const name = dialog.value.inputValue.trim()
  if (!name) { ElMessage.warning('请输入名称'); return }

  const icon = dialog.value.iconValue || undefined
  dialog.value.submitting = true
  try {
    const { mode, target } = dialog.value
    if (mode === 'add-level1') {
      await menuStore.addLevel1(name, icon)
      ElMessage.success('创建成功')
    } else if (mode === 'add-level2') {
      const newMenu = await menuStore.addLevel2(target!.id, name, icon)
      dialog.value.visible = false
      ElMessage.success('创建成功，请先完成表单设计')
      router.push(`/form-designer/${newMenu.id}`)
      return
    } else {
      await menuStore.updateMenu(target!.id, name, icon)
      ElMessage.success('保存成功')
    }
    dialog.value.visible = false
  } catch (e: unknown) {
    ElMessage.error(e instanceof Error ? e.message : '操作失败')
  } finally {
    dialog.value.submitting = false
  }
}

// ────────── 删除 ──────────
async function doDelete(menu: MenuItem) {
  try {
    await ElMessageBox.confirm(
      `确定要删除「${menu.name}」${menu.children?.length ? ' 及其所有子菜单' : ''}吗？`,
      '删除确认',
      { confirmButtonText: '删除', cancelButtonText: '取消', type: 'warning', confirmButtonClass: 'el-button--danger' },
    )
    await menuStore.deleteMenu(menu.id)
    ElMessage.success('删除成功')
  } catch { /* 用户取消 */ }
}

// ────────── 导航 ──────────
function navigate(menu: MenuItem) {
  // 找到父菜单名用于Tab标题
  let tabTitle = menu.name
  for (const m of menuStore.menuList) {
    if (m.children.some(c => c.id === menu.id)) {
      tabTitle = `${m.name} / ${menu.name}`
      break
    }
  }

  if (menu.isSystem) {
    tabStore.openTab('/global-config', '全局配置')
    router.push('/global-config')
  } else {
    const path = `/module/${menu.id}`
    tabStore.openTab(path, tabTitle)
    router.push(path)
  }
}
</script>

<template>
  <div ref="sidebarRef" class="sidebar" :class="{ collapsed }">

    <!-- Logo -->
    <div class="logo">
      <el-icon v-if="collapsed" :size="20"><Grid /></el-icon>
      <span v-else>AllIn LowCode Kit</span>
    </div>

    <!-- 加载骨架屏 -->
    <div v-if="menuStore.loading" style="padding: 12px;">
      <el-skeleton :rows="4" animated />
    </div>

    <!-- 菜单树 -->
    <el-menu
      v-else
      :collapse="collapsed"
      :collapse-transition="false"
      background-color="#001529"
      text-color="#c0c4cc"
      active-text-color="#ffffff"
      :default-openeds="collapsed ? [] : menuStore.menuList.map(m => String(m.id))"
      style="border: none; flex: 1; overflow-y: auto; overflow-x: hidden; min-height: 0;"
    >
      <template v-for="menu in menuStore.menuList" :key="menu.id">
        <el-sub-menu :index="String(menu.id)">
          <template #title>
            <el-icon><component :is="menu.icon || 'Folder'" /></el-icon>
            <!--
              span 携带 data 属性：右键检测时通过 closest/querySelector 定位
              不能用 div（会破坏 el-sub-menu__title 的 flex 布局）
            -->
            <span :data-ctx-id="menu.id" :data-ctx-level="1">{{ menu.name }}</span>
          </template>

          <el-menu-item
            v-for="child in menu.children"
            :key="child.id"
            :index="String(child.id)"
            @click="navigate(child)"
          >
            <el-icon><component :is="child.icon || 'Document'" /></el-icon>
            <template #title>
              <span :data-ctx-id="child.id" :data-ctx-level="2">{{ child.name }}</span>
            </template>
          </el-menu-item>
        </el-sub-menu>
      </template>
    </el-menu>

    <!-- 折叠按钮 -->
    <div class="collapse-btn" @click="collapsed = !collapsed">
      <el-icon :size="16"><component :is="collapsed ? 'Expand' : 'Fold'" /></el-icon>
    </div>
  </div>

  <!-- 右键上下文菜单（teleport 到 body） -->
  <ContextMenu
    :visible="ctxMenu.visible"
    :x="ctxMenu.x"
    :y="ctxMenu.y"
    :items="ctxMenu.items"
    @close="ctxMenu.visible = false"
  />

  <!-- 新增 / 编辑对话框 -->
  <el-dialog
    v-model="dialog.visible"
    :title="dialog.title"
    width="480px"
    draggable
    :close-on-click-modal="false"
  >
    <div class="dialog-form">
      <div class="form-row">
        <span class="form-label">名称</span>
        <el-input
          v-model="dialog.inputValue"
          placeholder="请输入菜单名称"
          maxlength="50"
          show-word-limit
          autofocus
          @keyup.enter="submitDialog"
        />
      </div>
      <div class="form-row">
        <span class="form-label">图标</span>
        <IconPicker v-model="dialog.iconValue" />
      </div>
    </div>
    <template #footer>
      <el-button @click="dialog.visible = false">取消</el-button>
      <el-button type="primary" :loading="dialog.submitting" @click="submitDialog">确定</el-button>
    </template>
  </el-dialog>
</template>

<style scoped>
.sidebar {
  width: 220px;
  height: 100vh;
  background: #001529;
  display: flex;
  flex-direction: column;
  flex-shrink: 0;
  overflow: hidden;
  transition: width 0.2s ease;
}

.sidebar.collapsed {
  width: 64px;
}

.logo {
  height: 50px;
  flex-shrink: 0;
  display: flex;
  align-items: center;
  justify-content: center;
  background: #002140;
  color: #fff;
  font-size: 14px;
  font-weight: bold;
  white-space: nowrap;
  overflow: hidden;
}

.collapse-btn {
  height: 40px;
  flex-shrink: 0;
  display: flex;
  align-items: center;
  justify-content: center;
  color: #c0c4cc;
  cursor: pointer;
  background: #001529;
  border-top: 1px solid #002a40;
  transition: background 0.15s, color 0.15s;
}

.collapse-btn:hover {
  background: #002140;
  color: #fff;
}

.dialog-form {
  display: flex;
  flex-direction: column;
  gap: 16px;
}

.form-row {
  display: flex;
  flex-direction: column;
  gap: 6px;
}

.form-label {
  font-size: 13px;
  color: #606266;
  font-weight: 500;
}
</style>
