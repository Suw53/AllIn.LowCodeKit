<script setup lang="ts">
import { ref, computed } from 'vue'
import * as Icons from '@element-plus/icons-vue'

const props = defineProps<{ modelValue?: string }>()
const emit = defineEmits<{ 'update:modelValue': [value: string] }>()

const search = ref('')

/** 所有图标名称（PascalCase，与 Element Plus 全局注册一致） */
const allIconNames = Object.keys(Icons)

const filteredIcons = computed(() =>
  search.value.trim()
    ? allIconNames.filter(n => n.toLowerCase().includes(search.value.trim().toLowerCase()))
    : allIconNames
)

function select(name: string) {
  emit('update:modelValue', props.modelValue === name ? '' : name)
}
</script>

<template>
  <div class="icon-picker">
    <!-- 搜索 -->
    <el-input
      v-model="search"
      placeholder="搜索图标名称…"
      size="small"
      clearable
      prefix-icon="Search"
    />

    <!-- 图标网格 -->
    <div class="icon-grid">
      <el-tooltip
        v-for="name in filteredIcons"
        :key="name"
        :content="name"
        placement="top"
        :show-after="600"
      >
        <div
          class="icon-cell"
          :class="{ active: modelValue === name }"
          @click="select(name)"
        >
          <el-icon :size="16"><component :is="name" /></el-icon>
        </div>
      </el-tooltip>
    </div>

    <!-- 空态 -->
    <div v-if="filteredIcons.length === 0" class="icon-empty">
      未找到匹配图标
    </div>

    <!-- 已选预览 -->
    <div v-if="modelValue" class="selected-bar">
      <el-icon :size="14"><component :is="modelValue" /></el-icon>
      <span class="selected-name">{{ modelValue }}</span>
      <el-button link type="danger" size="small" @click="emit('update:modelValue', '')">
        清除
      </el-button>
    </div>
    <div v-else class="selected-bar placeholder">
      未选择图标（可选）
    </div>
  </div>
</template>

<style scoped>
.icon-picker {
  display: flex;
  flex-direction: column;
  gap: 8px;
}

.icon-grid {
  display: flex;
  flex-wrap: wrap;
  gap: 2px;
  max-height: 192px;
  overflow-y: auto;
  padding: 4px;
  border: 1px solid #e4e7ed;
  border-radius: 4px;
  background: #fafafa;
}

.icon-cell {
  width: 34px;
  height: 34px;
  display: flex;
  align-items: center;
  justify-content: center;
  border-radius: 4px;
  cursor: pointer;
  color: #606266;
  border: 1px solid transparent;
  transition: background 0.12s, color 0.12s, border-color 0.12s;
  flex-shrink: 0;
}

.icon-cell:hover {
  background: #ecf5ff;
  color: #409eff;
}

.icon-cell.active {
  background: #409eff;
  color: #fff;
  border-color: #409eff;
}

.icon-empty {
  text-align: center;
  color: #c0c4cc;
  font-size: 13px;
  padding: 16px 0;
}

.selected-bar {
  display: flex;
  align-items: center;
  gap: 6px;
  font-size: 13px;
  color: #333;
  min-height: 24px;
}

.selected-bar.placeholder {
  color: #c0c4cc;
}

.selected-name {
  flex: 1;
  color: #409eff;
}
</style>
