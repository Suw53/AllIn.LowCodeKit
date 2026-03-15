<script setup lang="ts">
import SidebarMenu from '@/components/SidebarMenu.vue'
</script>

<template>
  <div style="display: flex; height: 100vh;">
    <SidebarMenu />
    <main style="flex: 1; background: #f0f2f5; overflow: hidden; min-width: 0;">
      <router-view />
    </main>
  </div>
</template>
