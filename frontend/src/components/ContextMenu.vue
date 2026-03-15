<script setup lang="ts">
import { onMounted, onUnmounted } from 'vue'

interface ContextMenuItem {
  label: string
  icon?: string
  danger?: boolean
  handler: () => void
}

const props = defineProps<{
  visible: boolean
  x: number
  y: number
  items: ContextMenuItem[]
}>()

const emit = defineEmits<{ close: [] }>()

function handleItemClick(item: ContextMenuItem) {
  item.handler()
  emit('close')
}

function handleOutsideEvent() {
  if (props.visible) emit('close')
}

onMounted(() => {
  // 延迟注册，避免触发本次右键事件立即关闭菜单
  setTimeout(() => {
    document.addEventListener('click', handleOutsideEvent)
    document.addEventListener('contextmenu', handleOutsideEvent)
  }, 0)
})

onUnmounted(() => {
  document.removeEventListener('click', handleOutsideEvent)
  document.removeEventListener('contextmenu', handleOutsideEvent)
})
</script>

<template>
  <teleport to="body">
    <div
      v-if="visible"
      class="ctx-menu"
      :style="{ left: x + 'px', top: y + 'px' }"
      @click.stop
      @contextmenu.stop.prevent
    >
      <div
        v-for="(item, idx) in items"
        :key="idx"
        class="ctx-menu-item"
        :class="{ danger: item.danger }"
        @click="handleItemClick(item)"
      >
        <el-icon v-if="item.icon" class="ctx-icon"><component :is="item.icon" /></el-icon>
        {{ item.label }}
      </div>
    </div>
  </teleport>
</template>

<style scoped>
.ctx-menu {
  position: fixed;
  z-index: 9999;
  background: #fff;
  border: 1px solid #e4e7ed;
  border-radius: 4px;
  box-shadow: 0 4px 16px rgba(0, 0, 0, 0.12);
  min-width: 148px;
  padding: 4px 0;
  user-select: none;
}

.ctx-menu-item {
  padding: 7px 16px;
  font-size: 13px;
  color: #333;
  cursor: pointer;
  display: flex;
  align-items: center;
  gap: 7px;
  transition: background 0.15s, color 0.15s;
}

.ctx-menu-item:hover {
  background: #f5f7fa;
  color: #409eff;
}

.ctx-menu-item.danger:hover {
  background: #fef0f0;
  color: #f56c6c;
}

.ctx-icon {
  font-size: 13px;
}
</style>
