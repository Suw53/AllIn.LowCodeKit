<script setup lang="ts">
import { watch } from 'vue'
import { useRoute } from 'vue-router'
import SidebarMenu from '@/components/SidebarMenu.vue'
import TabBar from '@/components/TabBar.vue'
import { useTabStore } from '@/stores/tabStore'

const route = useRoute()
const tabStore = useTabStore()

// 路由变化时同步激活状态（处理非侧边栏触发的跳转，如浏览器返回）
watch(() => route.path, (path) => {
  tabStore.setActive(path)
}, { immediate: true })
</script>

<template>
  <div style="display: flex; height: 100vh;">
    <SidebarMenu />
    <main style="flex: 1; display: flex; flex-direction: column; overflow: hidden; min-width: 0;">
      <TabBar />
      <div style="flex: 1; overflow: hidden;">
        <router-view />
      </div>
    </main>
  </div>
</template>
