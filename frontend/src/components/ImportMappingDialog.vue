<template>
  <el-dialog
    :model-value="visible"
    title="导入映射配置"
    width="1000px"
    @update:model-value="$emit('update:visible', $event)"
    @open="handleOpen"
    destroy-on-close
    class="mapping-dialog"
  >
    <!-- 顶部工具栏 -->
    <div class="mapping-toolbar">
      <div class="toolbar-left">
        <span class="toolbar-label">映射方案：</span>
        <el-select
          v-model="selectedConfigId"
          placeholder="选择已有方案"
          clearable
          style="width: 180px;"
          @change="handleLoadConfig"
        >
          <el-option
            v-for="cfg in mappingConfigs"
            :key="cfg.id"
            :label="cfg.name"
            :value="cfg.id"
          />
        </el-select>
        <el-button @click="handleSmartMatch" :icon="MagicStick">
          智能匹配
        </el-button>
        <el-button @click="handleClearAll" :icon="Delete">
          清空
        </el-button>
      </div>
    </div>

    <!-- 三栏连线区域 -->
    <div ref="containerRef" class="mapping-container">
      <!-- 左栏：Excel列 -->
      <div class="mapping-column left-column">
        <div class="column-header">Excel 列</div>
        <div class="column-items" ref="leftListRef">
          <div
            v-for="(header, idx) in headers"
            :key="'h-' + idx"
            class="mapping-item"
            :class="{ connected: isSourceConnected(header) }"
          >
            <span class="item-label" :title="header">{{ header }}</span>
            <div
              class="connector-dot right-dot"
              :ref="el => setLeftDotRef(header, el)"
              @mousedown.prevent="startDrag(header, 'source')"
            />
          </div>
        </div>
      </div>

      <!-- 中间：SVG连线区 -->
      <div class="mapping-svg-area" ref="svgAreaRef">
        <svg
          class="mapping-svg"
          @mousemove="onMouseMove"
          @mouseup="onMouseUp"
        >
          <!-- 已建立的连线 -->
          <g v-for="(conn, idx) in connections" :key="'conn-' + idx">
            <path
              :d="getPath(conn)"
              class="connection-line"
              @contextmenu.prevent="removeConnection(idx)"
            />
            <!-- 连线中点的 fx 图标 -->
            <g
              :transform="`translate(${getMidPoint(conn).x - 10}, ${getMidPoint(conn).y - 10})`"
              class="fx-icon"
              @click="openScriptEditor(idx)"
            >
              <rect
                width="20"
                height="20"
                rx="4"
                :fill="conn.transformScript ? '#e6a23c' : '#dcdfe6'"
              />
              <text
                x="10" y="14"
                text-anchor="middle"
                font-size="10"
                :fill="conn.transformScript ? '#fff' : '#606266'"
                font-weight="bold"
              >
                fx
              </text>
            </g>
          </g>

          <!-- 拖拽中的临时连线 -->
          <path
            v-if="dragging"
            :d="tempPath"
            class="temp-line"
          />
        </svg>
      </div>

      <!-- 右栏：表单字段 -->
      <div class="mapping-column right-column">
        <div class="column-header">表单字段</div>
        <div class="column-items" ref="rightListRef">
          <div
            v-for="field in fields"
            :key="'f-' + field.fieldName"
            class="mapping-item"
            :class="{ connected: isTargetConnected(field.fieldName) }"
          >
            <div
              class="connector-dot left-dot"
              :ref="el => setRightDotRef(field.fieldName, el)"
              @mousedown.prevent="startDrag(field.fieldName, 'target')"
              @mouseup="onDotMouseUp(field.fieldName, 'target')"
            />
            <span class="item-label" :title="field.label">
              {{ field.label }}
              <el-tag v-if="field.isRequired" type="danger" size="small">必填</el-tag>
            </span>
          </div>
        </div>
      </div>
    </div>

    <!-- 底部按钮 -->
    <template #footer>
      <div class="dialog-footer">
        <div class="footer-left">
          <el-button @click="handleSaveConfig" :loading="savingConfig">
            保存方案
          </el-button>
          <el-button @click="handleSaveAsConfig">另存为</el-button>
        </div>
        <div class="footer-right">
          <el-button @click="$emit('update:visible', false)">取消</el-button>
          <el-button
            type="primary"
            :disabled="connections.length === 0"
            @click="handleApply"
          >
            应用映射并预览
          </el-button>
        </div>
      </div>
    </template>

    <!-- 转换脚本编辑弹窗 -->
    <TransformScriptEditor
      v-model:visible="scriptEditorVisible"
      :script="editingScript"
      :source-column="editingSourceColumn"
      :target-field="editingTargetField"
      @confirm="handleScriptConfirm"
    />
  </el-dialog>
</template>

<script setup lang="ts">
import { ref, nextTick, onBeforeUnmount } from 'vue'
import { ElMessage, ElMessageBox } from 'element-plus'
import { MagicStick, Delete } from '@element-plus/icons-vue'
import type { FormField, ImportMappingConfig, MappingItem } from '@/types'
import {
  getImportMappingConfigs,
  createImportMappingConfig,
  updateImportMappingConfig
} from '@/api/importMappingConfig'
import TransformScriptEditor from './TransformScriptEditor.vue'

/** 内部连接数据结构，扩展了坐标信息 */
interface Connection {
  sourceColumn: string
  targetField: string
  transformScript: string | null
  /** 缓存的左侧dot坐标（相对于SVG） */
  x1: number
  y1: number
  /** 缓存的右侧dot坐标（相对于SVG） */
  x2: number
  y2: number
}

const props = defineProps<{
  visible: boolean
  menuId: number
  /** Excel表头列名列表 */
  headers: string[]
  /** 表单字段定义列表 */
  fields: FormField[]
}>()

const emit = defineEmits<{
  'update:visible': [val: boolean]
  /** 点击"应用映射并预览"后，返回映射规则 */
  apply: [mappings: MappingItem[]]
}>()

// ────────── DOM引用 ──────────
const containerRef = ref<HTMLElement>()
const svgAreaRef = ref<HTMLElement>()
const leftListRef = ref<HTMLElement>()
const rightListRef = ref<HTMLElement>()

/** 左侧dot元素映射 */
const leftDotRefs = new Map<string, HTMLElement>()
/** 右侧dot元素映射 */
const rightDotRefs = new Map<string, HTMLElement>()

function setLeftDotRef(key: string, el: unknown) {
  if (el instanceof HTMLElement) leftDotRefs.set(key, el)
}
function setRightDotRef(key: string, el: unknown) {
  if (el instanceof HTMLElement) rightDotRefs.set(key, el)
}

// ────────── 连接数据 ──────────
const connections = ref<Connection[]>([])
const mappingConfigs = ref<ImportMappingConfig[]>([])
const selectedConfigId = ref<number | null>(null)
const savingConfig = ref(false)

// ────────── 拖拽状态 ──────────
const dragging = ref(false)
const dragFrom = ref<{ key: string; side: 'source' | 'target' }>()
const dragStart = ref({ x: 0, y: 0 })
const dragCurrent = ref({ x: 0, y: 0 })
const tempPath = ref('')

// ────────── 脚本编辑器 ──────────
const scriptEditorVisible = ref(false)
const editingConnectionIdx = ref(-1)
const editingScript = ref<string | null>(null)
const editingSourceColumn = ref('')
const editingTargetField = ref('')

// ────────── ResizeObserver ──────────
let resizeObserver: ResizeObserver | null = null

function isSourceConnected(header: string) {
  return connections.value.some(c => c.sourceColumn === header)
}
function isTargetConnected(fieldName: string) {
  return connections.value.some(c => c.targetField === fieldName)
}

/** 获取dot相对于SVG区域的中心坐标 */
function getDotPosition(dot: HTMLElement): { x: number; y: number } {
  const svgRect = svgAreaRef.value?.getBoundingClientRect()
  if (!svgRect) return { x: 0, y: 0 }
  const dotRect = dot.getBoundingClientRect()
  return {
    x: dotRect.left + dotRect.width / 2 - svgRect.left,
    y: dotRect.top + dotRect.height / 2 - svgRect.top
  }
}

/** 刷新所有连线坐标 */
function refreshPositions() {
  for (const conn of connections.value) {
    const leftDot = leftDotRefs.get(conn.sourceColumn)
    const rightDot = rightDotRefs.get(conn.targetField)
    if (leftDot && rightDot) {
      const leftPos = getDotPosition(leftDot)
      const rightPos = getDotPosition(rightDot)
      conn.x1 = leftPos.x
      conn.y1 = leftPos.y
      conn.x2 = rightPos.x
      conn.y2 = rightPos.y
    }
  }
}

/** 生成贝塞尔曲线路径 */
function getPath(conn: Connection): string {
  const cx = (conn.x1 + conn.x2) / 2
  return `M ${conn.x1},${conn.y1} C ${cx},${conn.y1} ${cx},${conn.y2} ${conn.x2},${conn.y2}`
}

/** 获取连线中点坐标（用于放置fx图标） */
function getMidPoint(conn: Connection): { x: number; y: number } {
  return {
    x: (conn.x1 + conn.x2) / 2,
    y: (conn.y1 + conn.y2) / 2
  }
}

/** 开始拖拽创建连线 */
function startDrag(key: string, side: 'source' | 'target') {
  // 如果已连接，不允许再连
  if (side === 'source' && isSourceConnected(key)) return
  if (side === 'target' && isTargetConnected(key)) return

  dragging.value = true
  dragFrom.value = { key, side }

  const svgRect = svgAreaRef.value?.getBoundingClientRect()
  if (!svgRect) return

  let dotEl: HTMLElement | undefined
  if (side === 'source') {
    dotEl = leftDotRefs.get(key)
  } else {
    dotEl = rightDotRefs.get(key)
  }
  if (dotEl) {
    const pos = getDotPosition(dotEl)
    dragStart.value = pos
    dragCurrent.value = { ...pos }
  }

  // 全局鼠标事件
  document.addEventListener('mousemove', onMouseMoveGlobal)
  document.addEventListener('mouseup', onMouseUpGlobal)
}

function onMouseMoveGlobal(e: MouseEvent) {
  if (!dragging.value) return
  const svgRect = svgAreaRef.value?.getBoundingClientRect()
  if (!svgRect) return
  dragCurrent.value = {
    x: e.clientX - svgRect.left,
    y: e.clientY - svgRect.top
  }
  const { x: x1, y: y1 } = dragStart.value
  const { x: x2, y: y2 } = dragCurrent.value
  const cx = (x1 + x2) / 2
  tempPath.value = `M ${x1},${y1} C ${cx},${y1} ${cx},${y2} ${x2},${y2}`
}

function onMouseUpGlobal() {
  dragging.value = false
  tempPath.value = ''
  document.removeEventListener('mousemove', onMouseMoveGlobal)
  document.removeEventListener('mouseup', onMouseUpGlobal)
}

function onMouseMove() {
  // SVG内鼠标移动也处理（备用）
}

function onMouseUp() {
  // SVG区域mouseup，取消拖拽
  if (dragging.value) {
    dragging.value = false
    tempPath.value = ''
  }
}

/** dot上mouseup - 完成连线 */
function onDotMouseUp(key: string, side: 'source' | 'target') {
  if (!dragging.value || !dragFrom.value) return

  // 必须是不同侧
  if (dragFrom.value.side === side) return

  let sourceColumn: string
  let targetField: string

  if (dragFrom.value.side === 'source') {
    sourceColumn = dragFrom.value.key
    targetField = key
  } else {
    sourceColumn = key
    targetField = dragFrom.value.key
  }

  // 检查是否已连接
  if (isSourceConnected(sourceColumn) || isTargetConnected(targetField)) {
    dragging.value = false
    tempPath.value = ''
    return
  }

  // 建立连接
  addConnection(sourceColumn, targetField, null)

  dragging.value = false
  tempPath.value = ''
  document.removeEventListener('mousemove', onMouseMoveGlobal)
  document.removeEventListener('mouseup', onMouseUpGlobal)
}

/** 添加一条连接 */
function addConnection(sourceColumn: string, targetField: string, transformScript: string | null) {
  const leftDot = leftDotRefs.get(sourceColumn)
  const rightDot = rightDotRefs.get(targetField)
  if (!leftDot || !rightDot) return

  const leftPos = getDotPosition(leftDot)
  const rightPos = getDotPosition(rightDot)

  connections.value.push({
    sourceColumn,
    targetField,
    transformScript,
    x1: leftPos.x,
    y1: leftPos.y,
    x2: rightPos.x,
    y2: rightPos.y
  })
}

/** 移除一条连接（右键） */
function removeConnection(idx: number) {
  connections.value.splice(idx, 1)
}

/** 智能匹配：按label名匹配 */
function handleSmartMatch() {
  for (const header of props.headers) {
    if (isSourceConnected(header)) continue
    // 查找label完全匹配的字段
    const field = props.fields.find(
      f => f.label === header && !isTargetConnected(f.fieldName)
    )
    if (field) {
      addConnection(header, field.fieldName, null)
    }
  }
  nextTick(refreshPositions)
}

/** 清空所有连线 */
function handleClearAll() {
  connections.value = []
}

/** 打开转换脚本编辑器 */
function openScriptEditor(idx: number) {
  const conn = connections.value[idx]
  editingConnectionIdx.value = idx
  editingScript.value = conn.transformScript
  editingSourceColumn.value = conn.sourceColumn
  editingTargetField.value = conn.targetField
  scriptEditorVisible.value = true
}

/** 脚本编辑确认 */
function handleScriptConfirm(script: string | null) {
  const idx = editingConnectionIdx.value
  if (idx >= 0 && idx < connections.value.length) {
    connections.value[idx].transformScript = script
  }
}

/** 对话框打开时初始化 */
async function handleOpen() {
  connections.value = []
  selectedConfigId.value = null
  leftDotRefs.clear()
  rightDotRefs.clear()

  // 加载映射方案列表
  try {
    mappingConfigs.value = await getImportMappingConfigs(props.menuId)
  } catch {
    mappingConfigs.value = []
  }

  // 等待DOM渲染后设置ResizeObserver
  await nextTick()
  await nextTick()
  setupResizeObserver()
}

/** 加载已有映射方案 */
function handleLoadConfig(configId: number | null) {
  if (!configId) return
  const cfg = mappingConfigs.value.find(c => c.id === configId)
  if (!cfg) return

  connections.value = []
  let items: MappingItem[] = []
  try {
    items = JSON.parse(cfg.mappings)
  } catch {
    return
  }

  nextTick(() => {
    for (const item of items) {
      // 确保两侧都存在
      if (
        props.headers.includes(item.sourceColumn) &&
        props.fields.some(f => f.fieldName === item.targetField)
      ) {
        addConnection(item.sourceColumn, item.targetField, item.transformScript)
      }
    }
    nextTick(refreshPositions)
  })
}

/** 保存映射方案 */
async function handleSaveConfig() {
  if (connections.value.length === 0) {
    ElMessage.warning('请先建立映射关系')
    return
  }

  const mappingItems = connectionsToMappingItems()

  if (selectedConfigId.value) {
    // 更新已有方案
    const cfg = mappingConfigs.value.find(c => c.id === selectedConfigId.value)
    savingConfig.value = true
    try {
      await updateImportMappingConfig(selectedConfigId.value, {
        name: cfg?.name || '映射方案',
        mappings: JSON.stringify(mappingItems)
      })
      mappingConfigs.value = await getImportMappingConfigs(props.menuId)
      ElMessage.success('方案已更新')
    } catch {
      ElMessage.error('保存失败')
    } finally {
      savingConfig.value = false
    }
  } else {
    await doSaveAs(mappingItems)
  }
}

/** 另存为新方案 */
async function handleSaveAsConfig() {
  if (connections.value.length === 0) {
    ElMessage.warning('请先建立映射关系')
    return
  }
  await doSaveAs(connectionsToMappingItems())
}

async function doSaveAs(mappingItems: MappingItem[]) {
  try {
    const { value: name } = await ElMessageBox.prompt('请输入方案名称', '保存映射方案', {
      confirmButtonText: '保存',
      cancelButtonText: '取消',
      inputPlaceholder: '输入方案名称'
    })
    if (!name?.trim()) return

    savingConfig.value = true
    try {
      const created = await createImportMappingConfig(props.menuId, {
        name: name.trim(),
        mappings: JSON.stringify(mappingItems)
      })
      mappingConfigs.value = await getImportMappingConfigs(props.menuId)
      selectedConfigId.value = created.id
      ElMessage.success('方案已保存')
    } catch {
      ElMessage.error('保存失败')
    } finally {
      savingConfig.value = false
    }
  } catch {
    // 用户取消
  }
}

/** 连接数据转为 MappingItem 数组 */
function connectionsToMappingItems(): MappingItem[] {
  return connections.value.map(c => ({
    sourceColumn: c.sourceColumn,
    targetField: c.targetField,
    transformScript: c.transformScript
  }))
}

/** 应用映射并预览 */
function handleApply() {
  emit('apply', connectionsToMappingItems())
  emit('update:visible', false)
}

/** 设置 ResizeObserver 监听容器变化 */
function setupResizeObserver() {
  if (resizeObserver) resizeObserver.disconnect()

  const target = containerRef.value
  if (!target) return

  resizeObserver = new ResizeObserver(() => {
    refreshPositions()
  })
  resizeObserver.observe(target)

  // 监听左右列表滚动事件
  leftListRef.value?.addEventListener('scroll', refreshPositions)
  rightListRef.value?.addEventListener('scroll', refreshPositions)
}

onBeforeUnmount(() => {
  resizeObserver?.disconnect()
  leftListRef.value?.removeEventListener('scroll', refreshPositions)
  rightListRef.value?.removeEventListener('scroll', refreshPositions)
  document.removeEventListener('mousemove', onMouseMoveGlobal)
  document.removeEventListener('mouseup', onMouseUpGlobal)
})
</script>

<style scoped>
.mapping-dialog :deep(.el-dialog__body) {
  padding: 12px 20px;
}

.mapping-toolbar {
  display: flex;
  justify-content: space-between;
  align-items: center;
  margin-bottom: 12px;
}
.toolbar-left {
  display: flex;
  align-items: center;
  gap: 8px;
}
.toolbar-label {
  font-size: 13px;
  color: var(--el-text-color-regular);
}

.mapping-container {
  display: flex;
  height: 420px;
  border: 1px solid var(--el-border-color);
  border-radius: 6px;
  overflow: hidden;
  user-select: none;
}

.mapping-column {
  width: 240px;
  min-width: 200px;
  display: flex;
  flex-direction: column;
  background: var(--el-bg-color);
}
.column-header {
  padding: 10px 12px;
  font-weight: 600;
  font-size: 13px;
  border-bottom: 1px solid var(--el-border-color-lighter);
  background: var(--el-fill-color-light);
  text-align: center;
}
.column-items {
  flex: 1;
  overflow-y: auto;
  padding: 4px 0;
}

.mapping-item {
  display: flex;
  align-items: center;
  padding: 8px 12px;
  font-size: 13px;
  transition: background 0.15s;
  position: relative;
}
.mapping-item:hover {
  background: var(--el-fill-color-lighter);
}
.mapping-item.connected {
  color: var(--el-color-primary);
}
.left-column .mapping-item {
  justify-content: space-between;
}
.right-column .mapping-item {
  gap: 8px;
}
.item-label {
  overflow: hidden;
  text-overflow: ellipsis;
  white-space: nowrap;
  flex: 1;
}

.connector-dot {
  width: 12px;
  height: 12px;
  border-radius: 50%;
  border: 2px solid var(--el-color-primary);
  background: #fff;
  cursor: crosshair;
  flex-shrink: 0;
  transition: background 0.15s, transform 0.15s;
  z-index: 2;
}
.connector-dot:hover {
  background: var(--el-color-primary);
  transform: scale(1.3);
}
.mapping-item.connected .connector-dot {
  background: var(--el-color-primary);
}

.mapping-svg-area {
  flex: 1;
  position: relative;
  min-width: 160px;
  background:
    repeating-linear-gradient(
      90deg,
      transparent, transparent 19px,
      var(--el-border-color-extra-light) 19px,
      var(--el-border-color-extra-light) 20px
    ),
    repeating-linear-gradient(
      0deg,
      transparent, transparent 19px,
      var(--el-border-color-extra-light) 19px,
      var(--el-border-color-extra-light) 20px
    );
}
.mapping-svg {
  width: 100%;
  height: 100%;
  position: absolute;
  top: 0;
  left: 0;
}

.connection-line {
  fill: none;
  stroke: var(--el-color-primary);
  stroke-width: 2;
  cursor: pointer;
  transition: stroke-width 0.15s;
}
.connection-line:hover {
  stroke-width: 3;
  stroke: var(--el-color-danger);
}
.temp-line {
  fill: none;
  stroke: var(--el-color-primary-light-3);
  stroke-width: 2;
  stroke-dasharray: 6 4;
  pointer-events: none;
}

.fx-icon {
  cursor: pointer;
}
.fx-icon:hover rect {
  fill: var(--el-color-primary) !important;
}
.fx-icon:hover text {
  fill: #fff !important;
}

.dialog-footer {
  display: flex;
  justify-content: space-between;
  width: 100%;
}
.footer-left,
.footer-right {
  display: flex;
  gap: 8px;
}
</style>
