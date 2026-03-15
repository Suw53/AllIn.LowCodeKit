<script setup lang="ts">
import { computed } from 'vue'
import { useRoute } from 'vue-router'
import { useMenuStore } from '@/stores/menuStore'

const route = useRoute()
const menuStore = useMenuStore()
const menuId = computed(() => Number(route.params.menuId))

const menuName = computed(() => {
  for (const m of menuStore.menuList) {
    const child = m.children.find(c => c.id === menuId.value)
    if (child) return child.name
  }
  return `菜单 #${menuId.value}`
})
</script>

<template>
  <div class="page-container" style="display:flex; align-items:center; justify-content:center; height:100%;">
    <el-empty :description="`「${menuName}」表单设计器（Phase 3 实现）`">
      <template #image>
        <el-icon :size="64" style="color:#c0c4cc;"><Edit /></el-icon>
      </template>
    </el-empty>
  </div>
</template>
