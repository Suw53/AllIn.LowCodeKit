<template>
  <el-dialog
    :model-value="visible"
    title="管理导入模板"
    width="600px"
    @update:model-value="$emit('update:visible', $event)"
    @open="handleOpen"
  >
    <!-- 已有配置列表 -->
    <div v-if="!editing" class="config-list">
      <div v-if="configs.length === 0" class="empty-tip">
        暂无导入模板配置，点击下方按钮新建
      </div>
      <div
        v-for="cfg in configs"
        :key="cfg.id"
        class="config-item"
      >
        <div class="config-info">
          <span class="config-name">{{ cfg.name }}</span>
          <span class="config-count">
            {{ parseFieldNames(cfg.fieldNames).length }} 个字段
          </span>
        </div>
        <div class="config-actions">
          <el-button
            type="primary"
            link
            :loading="downloadingId === cfg.id"
            @click="handleDownload(cfg)"
          >
            下载模板
          </el-button>
          <el-button type="primary" link @click="handleEdit(cfg)">编辑</el-button>
          <el-popconfirm
            title="确定删除该配置？"
            @confirm="handleDelete(cfg.id)"
          >
            <template #reference>
              <el-button
                type="danger"
                link
                :loading="deletingId === cfg.id"
              >
                删除
              </el-button>
            </template>
          </el-popconfirm>
        </div>
      </div>

      <div class="config-footer">
        <el-button type="primary" @click="handleCreate">新建配置</el-button>
      </div>
    </div>

    <!-- 编辑区 -->
    <div v-else class="config-editor">
      <el-form label-width="80px">
        <el-form-item label="配置名称">
          <el-input
            v-model="editForm.name"
            placeholder="请输入配置名称"
            maxlength="50"
          />
        </el-form-item>
        <el-form-item label="选择字段">
          <div class="field-checkboxes">
            <el-checkbox
              :model-value="isAllChecked"
              :indeterminate="isIndeterminate"
              @change="handleCheckAll"
            >
              全选
            </el-checkbox>
            <el-divider />
            <el-checkbox-group v-model="editForm.selectedFields">
              <el-checkbox
                v-for="f in fields"
                :key="f.fieldName"
                :value="f.fieldName"
              >
                {{ f.label }}
                <el-tag v-if="f.isRequired" type="danger" size="small">必填</el-tag>
              </el-checkbox>
            </el-checkbox-group>
          </div>
        </el-form-item>
      </el-form>

      <div class="editor-footer">
        <el-button @click="editing = false">返回列表</el-button>
        <el-button
          type="primary"
          :loading="saving"
          :disabled="!editForm.name.trim() || editForm.selectedFields.length === 0"
          @click="handleSave"
        >
          保存
        </el-button>
      </div>
    </div>
  </el-dialog>
</template>

<script setup lang="ts">
import { ref, computed } from 'vue'
import { ElMessage } from 'element-plus'
import type { FormField, ImportTemplateConfig } from '@/types'
import {
  getImportTemplateConfigs,
  createImportTemplateConfig,
  updateImportTemplateConfig,
  deleteImportTemplateConfig
} from '@/api/importTemplateConfig'
import { downloadTemplate } from '@/api/data'

const props = defineProps<{
  visible: boolean
  menuId: number
  fields: FormField[]
}>()

const emit = defineEmits<{
  'update:visible': [val: boolean]
  /** 配置发生变更时触发（增删改），父组件可刷新列表 */
  change: []
}>()

/** 已有配置列表 */
const configs = ref<ImportTemplateConfig[]>([])
const loading = ref(false)
const saving = ref(false)
const deletingId = ref<number | null>(null)
const downloadingId = ref<number | null>(null)

/** 是否处于编辑状态 */
const editing = ref(false)

/** 编辑表单 */
const editForm = ref({
  id: null as number | null,
  name: '',
  selectedFields: [] as string[]
})

/** 全选状态 */
const isAllChecked = computed(
  () => editForm.value.selectedFields.length === props.fields.length
)
const isIndeterminate = computed(
  () => editForm.value.selectedFields.length > 0 && !isAllChecked.value
)

/** 解析字段名JSON */
function parseFieldNames(json: string): string[] {
  try {
    return JSON.parse(json)
  } catch {
    return []
  }
}

/** 对话框打开时加载配置列表 */
async function handleOpen() {
  editing.value = false
  loading.value = true
  try {
    configs.value = await getImportTemplateConfigs(props.menuId)
  } catch {
    ElMessage.error('加载导入模板配置失败')
  } finally {
    loading.value = false
  }
}

/** 新建配置 */
function handleCreate() {
  editForm.value = {
    id: null,
    name: '',
    selectedFields: props.fields.map(f => f.fieldName)
  }
  editing.value = true
}

/** 编辑已有配置 */
function handleEdit(cfg: ImportTemplateConfig) {
  editForm.value = {
    id: cfg.id,
    name: cfg.name,
    selectedFields: parseFieldNames(cfg.fieldNames)
  }
  editing.value = true
}

/** 全选/取消全选 */
function handleCheckAll(checked: boolean | string | number) {
  if (checked) {
    editForm.value.selectedFields = props.fields.map(f => f.fieldName)
  } else {
    editForm.value.selectedFields = []
  }
}

/** 保存配置 */
async function handleSave() {
  const { id, name, selectedFields } = editForm.value
  if (!name.trim()) return
  if (selectedFields.length === 0) {
    ElMessage.warning('请至少选择一个字段')
    return
  }

  saving.value = true
  try {
    const data = {
      name: name.trim(),
      fieldNames: JSON.stringify(selectedFields)
    }
    if (id) {
      await updateImportTemplateConfig(id, data)
      ElMessage.success('配置已更新')
    } else {
      await createImportTemplateConfig(props.menuId, data)
      ElMessage.success('配置已创建')
    }
    editing.value = false
    configs.value = await getImportTemplateConfigs(props.menuId)
    emit('change')
  } catch {
    ElMessage.error('保存失败')
  } finally {
    saving.value = false
  }
}

/** 删除配置 */
async function handleDelete(id: number) {
  deletingId.value = id
  try {
    await deleteImportTemplateConfig(id)
    configs.value = configs.value.filter(c => c.id !== id)
    ElMessage.success('已删除')
    emit('change')
  } catch {
    ElMessage.error('删除失败')
  } finally {
    deletingId.value = null
  }
}

/** 按配置下载模板 */
async function handleDownload(cfg: ImportTemplateConfig) {
  downloadingId.value = cfg.id
  try {
    await downloadTemplate(props.menuId, cfg.id, cfg.name)
  } catch {
    ElMessage.error('下载失败')
  } finally {
    downloadingId.value = null
  }
}
</script>

<style scoped>
.config-list {
  min-height: 120px;
}
.empty-tip {
  text-align: center;
  color: var(--el-text-color-secondary);
  padding: 32px 0;
}
.config-item {
  display: flex;
  justify-content: space-between;
  align-items: center;
  padding: 10px 12px;
  border-bottom: 1px solid var(--el-border-color-lighter);
}
.config-item:last-child {
  border-bottom: none;
}
.config-info {
  display: flex;
  align-items: center;
  gap: 12px;
}
.config-name {
  font-weight: 500;
}
.config-count {
  color: var(--el-text-color-secondary);
  font-size: 12px;
}
.config-actions {
  display: flex;
  gap: 4px;
}
.config-footer {
  text-align: center;
  padding-top: 16px;
}
.config-editor {
  min-height: 200px;
}
.field-checkboxes {
  max-height: 300px;
  overflow-y: auto;
}
.field-checkboxes .el-checkbox {
  display: flex;
  margin-right: 0;
  margin-bottom: 4px;
}
.editor-footer {
  display: flex;
  justify-content: flex-end;
  gap: 8px;
  padding-top: 16px;
}
</style>
