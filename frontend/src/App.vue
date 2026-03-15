<script setup lang="ts">
import { onMounted } from 'vue'
import { useRouter } from 'vue-router'
import { useMenuStore, type MenuItem } from '@/stores/menuStore'

const router = useRouter()
const menuStore = useMenuStore()

onMounted(() => {
  menuStore.fetchMenus()
})

/** 导航到功能模块列表 */
function handleMenuClick(menu: MenuItem) {
  router.push(`/module/${menu.id}`)
}

/** 导航到全局配置 */
function handleGlobalConfig() {
  router.push('/global-config')
}
</script>

<template>
  <el-container style="height: 100vh;">
    <!-- 左侧菜单 -->
    <el-aside width="220px" style="background:#001529; overflow:hidden;">
      <div class="logo">AllIn LowCode Kit</div>
      <el-menu
        background-color="#001529"
        text-color="#c0c4cc"
        active-text-color="#ffffff"
        :default-openeds="menuStore.menuList.map((m: MenuItem) => String(m.id))"
        style="border:none; height:calc(100vh - 50px); overflow-y:auto;"
      >
        <template v-for="menu in menuStore.menuList" :key="menu.id">
          <!-- 一级菜单（目录） -->
          <el-sub-menu :index="String(menu.id)">
            <template #title>
              <el-icon v-if="menu.icon"><component :is="menu.icon" /></el-icon>
              <span>{{ menu.name }}</span>
            </template>
            <!-- 二级菜单（功能模块） -->
            <el-menu-item
              v-for="child in menu.children"
              :key="child.id"
              :index="String(child.id)"
              @click="child.isSystem ? handleGlobalConfig() : handleMenuClick(child)"
            >
              <el-icon v-if="child.icon"><component :is="child.icon" /></el-icon>
              <span>{{ child.name }}</span>
            </el-menu-item>
          </el-sub-menu>
        </template>
      </el-menu>
    </el-aside>

    <!-- 右侧内容区 -->
    <el-main style="padding:0; background:#f0f2f5;">
      <router-view />
    </el-main>
  </el-container>
</template>

<style scoped>
.logo {
  height: 50px;
  line-height: 50px;
  text-align: center;
  color: #fff;
  font-size: 14px;
  font-weight: bold;
  background: #002140;
  overflow: hidden;
  white-space: nowrap;
  text-overflow: ellipsis;
  padding: 0 8px;
}
</style>
