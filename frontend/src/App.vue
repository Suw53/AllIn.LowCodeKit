<script setup lang="ts">
import { ref, onMounted } from 'vue'
import { useRouter } from 'vue-router'
import { ElMessageBox, ElMessage } from 'element-plus'
import { useMenuStore, type MenuItem } from '@/stores/menuStore'
import ContextMenu from '@/components/ContextMenu.vue'

const router = useRouter()
const menuStore = useMenuStore()

// ────────── 侧边栏折叠 ──────────
const collapsed = ref(false)

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
    items
  }
}

/** 侧边栏空白区右键 */
function onSidebarCtx(e: MouseEvent) {
  showCtxMenu(e, [
    { label: '新增一级菜单', icon: 'Plus', handler: () => openDialog('add-level1', null) }
  ])
}

/** 一级菜单标题右键 */
function onLevel1Ctx(e: MouseEvent, menu: MenuItem) {
  const items: CtxItem[] = [
    { label: '新增子菜单', icon: 'Plus', handler: () => openDialog('add-level2', menu) }
  ]
  if (!menu.isSystem) {
    items.push({ label: '重命名', icon: 'Edit', handler: () => openDialog('rename', menu) })
    items.push({ label: '删除', icon: 'Delete', danger: true, handler: () => doDelete(menu) })
  }
  showCtxMenu(e, items)
}

/** 二级菜单右键 */
function onLevel2Ctx(e: MouseEvent, menu: MenuItem) {
  if (menu.isSystem) return
  showCtxMenu(e, [
    { label: '重命名', icon: 'Edit', handler: () => openDialog('rename', menu) },
    { label: '删除', icon: 'Delete', danger: true, handler: () => doDelete(menu) }
  ])
}

// ────────── 对话框 ──────────
type DialogMode = 'add-level1' | 'add-level2' | 'rename'

const dialog = ref({
  visible: false,
  title: '',
  inputValue: '',
  submitting: false,
  mode: 'add-level1' as DialogMode,
  target: null as MenuItem | null  // add-level2: 父级菜单；rename: 目标菜单
})

function openDialog(mode: DialogMode, menu: MenuItem | null) {
  dialog.value = {
    visible: true,
    mode,
    target: menu,
    title: mode === 'add-level1' ? '新增一级菜单'
         : mode === 'add-level2' ? `在「${menu!.name}」下新增子菜单`
         : '重命名',
    inputValue: mode === 'rename' ? menu!.name : '',
    submitting: false
  }
}

async function submitDialog() {
  const name = dialog.value.inputValue.trim()
  if (!name) {
    ElMessage.warning('请输入名称')
    return
  }
  dialog.value.submitting = true
  try {
    const { mode, target } = dialog.value
    if (mode === 'add-level1') {
      await menuStore.addLevel1(name)
      ElMessage.success('创建成功')
    } else if (mode === 'add-level2') {
      const newMenu = await menuStore.addLevel2(target!.id, name)
      ElMessage.success('创建成功，请先完成表单设计')
      dialog.value.visible = false
      router.push(`/form-designer/${newMenu.id}`)
      return
    } else {
      await menuStore.updateMenu(target!.id, name)
      ElMessage.success('重命名成功')
    }
    dialog.value.visible = false
  } catch (e: unknown) {
    const msg = e instanceof Error ? e.message : '操作失败'
    ElMessage.error(msg)
  } finally {
    dialog.value.submitting = false
  }
}

// ────────── 删除 ──────────
async function doDelete(menu: MenuItem) {
  const hasChildren = menu.children?.length > 0
  try {
    await ElMessageBox.confirm(
      `确定要删除「${menu.name}」${hasChildren ? ' 及其所有子菜单' : ''}吗？`,
      '删除确认',
      {
        confirmButtonText: '删除',
        cancelButtonText: '取消',
        type: 'warning',
        confirmButtonClass: 'el-button--danger'
      }
    )
    await menuStore.deleteMenu(menu.id)
    ElMessage.success('删除成功')
  } catch {
    // 用户取消，不处理
  }
}

// ────────── 导航 ──────────
function handleMenuClick(menu: MenuItem) {
  router.push(`/module/${menu.id}`)
}
function handleGlobalConfig() {
  router.push('/global-config')
}

onMounted(() => {
  menuStore.fetchMenus()
})
</script>

<template>
  <el-container style="height: 100vh;">

    <!-- ── 左侧菜单 ── -->
    <el-aside
      :width="collapsed ? '64px' : '220px'"
      class="sidebar"
      @contextmenu.prevent="onSidebarCtx"
    >
      <!-- Logo -->
      <div class="logo" :class="{ collapsed }">
        <el-icon v-if="collapsed" :size="20"><Grid /></el-icon>
        <span v-else>AllIn LowCode Kit</span>
      </div>

      <!-- 骨架屏 -->
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
        :default-openeds="collapsed ? [] : menuStore.menuList.map((m: MenuItem) => String(m.id))"
        style="border: none; height: calc(100vh - 90px); overflow-y: auto; overflow-x: hidden;"
      >
        <template v-for="menu in menuStore.menuList" :key="menu.id">
          <el-sub-menu :index="String(menu.id)">
            <template #title>
              <!-- 使用 wrapper div 捕获右键，避免 el-sub-menu 内部拦截 -->
              <div class="level1-title" @contextmenu.prevent.stop="onLevel1Ctx($event, menu)">
                <el-icon><component :is="menu.icon || 'Folder'" /></el-icon>
                <span>{{ menu.name }}</span>
              </div>
            </template>

            <el-menu-item
              v-for="child in menu.children"
              :key="child.id"
              :index="String(child.id)"
              @click="child.isSystem ? handleGlobalConfig() : handleMenuClick(child)"
              @contextmenu.prevent.stop="onLevel2Ctx($event, child)"
            >
              <el-icon><component :is="child.icon || 'Document'" /></el-icon>
              <template #title>{{ child.name }}</template>
            </el-menu-item>
          </el-sub-menu>
        </template>
      </el-menu>

      <!-- 折叠按钮 -->
      <div class="collapse-btn" @click.stop="collapsed = !collapsed">
        <el-icon :size="16">
          <component :is="collapsed ? 'Expand' : 'Fold'" />
        </el-icon>
      </div>
    </el-aside>

    <!-- ── 右侧内容区 ── -->
    <el-main style="padding: 0; background: #f0f2f5; overflow: hidden;">
      <router-view />
    </el-main>
  </el-container>

  <!-- 右键上下文菜单 -->
  <ContextMenu
    :visible="ctxMenu.visible"
    :x="ctxMenu.x"
    :y="ctxMenu.y"
    :items="ctxMenu.items"
    @close="ctxMenu.visible = false"
  />

  <!-- 新增 / 重命名对话框 -->
  <el-dialog
    v-model="dialog.visible"
    :title="dialog.title"
    width="360px"
    draggable
    :close-on-click-modal="false"
  >
    <el-input
      v-model="dialog.inputValue"
      placeholder="请输入名称"
      maxlength="50"
      show-word-limit
      autofocus
      @keyup.enter="submitDialog"
    />
    <template #footer>
      <el-button @click="dialog.visible = false">取消</el-button>
      <el-button type="primary" :loading="dialog.submitting" @click="submitDialog">
        确定
      </el-button>
    </template>
  </el-dialog>
</template>

<style scoped>
.sidebar {
  background: #001529;
  overflow: hidden;
  flex-shrink: 0;
  transition: width 0.2s ease;
  position: relative;
  display: flex;
  flex-direction: column;
}

.logo {
  height: 50px;
  display: flex;
  align-items: center;
  justify-content: center;
  background: #002140;
  color: #fff;
  font-size: 14px;
  font-weight: bold;
  white-space: nowrap;
  overflow: hidden;
  flex-shrink: 0;
}

.level1-title {
  display: flex;
  align-items: center;
  gap: 6px;
  width: 100%;
  height: 100%;
}

.collapse-btn {
  position: absolute;
  bottom: 0;
  left: 0;
  right: 0;
  height: 40px;
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
</style>
