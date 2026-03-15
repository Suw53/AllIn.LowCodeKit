<!-- 顶部页签栏：显示已打开的模块页签，支持切换和关闭 -->
<script setup lang="ts">
import { useTabStore } from '@/stores/tabStore'
import { useRouter } from 'vue-router'

const tabStore = useTabStore()
const router = useRouter()

function handleTabClick(key: string) {
  tabStore.setActive(key)
  router.push(key)
}

function handleClose(e: MouseEvent, key: string) {
  e.stopPropagation()
  tabStore.closeTab(key)
}
</script>

<template>
  <div class="tab-bar">
    <div
      v-for="tab in tabStore.tabs"
      :key="tab.key"
      class="tab-item"
      :class="{ active: tabStore.activeKey === tab.key }"
      @click="handleTabClick(tab.key)"
    >
      <span class="tab-title">{{ tab.title }}</span>
      <el-icon
        v-if="tab.closeable"
        class="tab-close"
        @click="handleClose($event, tab.key)"
      >
        <Close />
      </el-icon>
    </div>
  </div>
</template>

<style scoped>
.tab-bar {
  height: 36px;
  flex-shrink: 0;
  background: #e8eaed;
  display: flex;
  align-items: flex-end;
  padding: 0 4px;
  overflow-x: auto;
  gap: 2px;
  border-bottom: 1px solid #d4d7de;
}

/* 隐藏横向滚动条 */
.tab-bar::-webkit-scrollbar {
  height: 0;
}

.tab-item {
  height: 30px;
  padding: 0 10px;
  background: #cfd3dc;
  border-radius: 4px 4px 0 0;
  display: flex;
  align-items: center;
  gap: 5px;
  cursor: pointer;
  font-size: 12px;
  color: #606266;
  white-space: nowrap;
  flex-shrink: 0;
  user-select: none;
  transition: background 0.1s;
  max-width: 160px;
}

.tab-item:hover {
  background: #dde0e7;
  color: #303133;
}

.tab-item.active {
  background: #f0f2f5;
  color: #409eff;
  font-weight: 500;
}

.tab-title {
  overflow: hidden;
  text-overflow: ellipsis;
  white-space: nowrap;
  max-width: 120px;
}

.tab-close {
  font-size: 10px;
  color: #909399;
  flex-shrink: 0;
  border-radius: 50%;
  padding: 2px;
}

.tab-close:hover {
  background: #c0c4cc;
  color: #303133;
}
</style>
