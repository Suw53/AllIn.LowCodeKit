import { defineStore } from 'pinia'
import { ref } from 'vue'
import type { FormTemplate, FormField } from '@/types'
import {
  getTemplateByMenu,
  saveTemplateForMenu,
  exportTemplate,
  importTemplate,
  type SaveTemplateDto
} from '@/api/formTemplate'

// ────────── 代码模式辅助函数 ──────────

const AUTO_START = '// ===== AUTO:REQUIRED:START ====='
const AUTO_END = '// ===== AUTO:REQUIRED:END ====='

/** 生成默认 C# 校验代码模板 */
export function buildDefaultCode(): string {
  return `// 表单校验脚本
// 参数：
//   data   - Dictionary<string, object?>  当前行数据
//   errors - List<string>  追加校验错误信息
${AUTO_START}
${AUTO_END}

// 在此处编写自定义校验逻辑
`
}

/** 从代码的 AUTO 区块中解析出"必填字段名"集合 */
export function parseRequiredFromCode(code: string): Set<string> {
  const required = new Set<string>()
  const startIdx = code.indexOf(AUTO_START)
  const endIdx = code.indexOf(AUTO_END)
  if (startIdx === -1 || endIdx === -1) return required

  const section = code.slice(startIdx + AUTO_START.length, endIdx)
  const regex = /data\["(\w+)"\]/g
  let match
  while ((match = regex.exec(section)) !== null) {
    required.add(match[1])
  }
  return required
}

/** 根据字段列表的 isRequired 重建 AUTO 区块，替换代码中对应区域 */
export function syncRequiredToCode(code: string, fields: FormField[]): string {
  const startIdx = code.indexOf(AUTO_START)
  const endIdx = code.indexOf(AUTO_END)
  if (startIdx === -1 || endIdx === -1) return code

  const lines = fields
    .filter(f => f.isRequired && f.fieldName)
    .map(f => `    if (string.IsNullOrWhiteSpace(data["${f.fieldName}"]?.ToString())) errors.Add("${f.label} 为必填项");`)

  const newSection = lines.length > 0 ? '\n' + lines.join('\n') + '\n' : '\n'
  return (
    code.slice(0, startIdx + AUTO_START.length) +
    newSection +
    code.slice(endIdx)
  )
}

// ────────── Pinia Store ──────────

export const useFormTemplateStore = defineStore('formTemplate', () => {
  const template = ref<FormTemplate | null>(null)
  const loading = ref(false)
  const saving = ref(false)

  /** 根据菜单Id加载模板（不存在则置 null） */
  async function loadByMenu(menuId: number) {
    loading.value = true
    try {
      template.value = await getTemplateByMenu(menuId) ?? null
    } finally {
      loading.value = false
    }
  }

  /** 全量保存（upsert：不存在则创建，存在则更新） */
  async function save(menuId: number, name: string, fields: FormField[], codeLogic: string) {
    saving.value = true
    try {
      const dto: SaveTemplateDto = {
        name,
        codeLogic,
        fields: fields.map(f => ({
          fieldName: f.fieldName,
          label: f.label,
          fieldType: f.fieldType,
          options: f.options,
          isRequired: f.isRequired,
          remark: f.remark,
          columnOrder: f.columnOrder
        }))
      }
      template.value = await saveTemplateForMenu(menuId, dto)
    } finally {
      saving.value = false
    }
  }

  /** 导出模板 JSON */
  async function doExport(): Promise<FormTemplate | null> {
    if (!template.value) return null
    return exportTemplate(template.value.id)
  }

  /** 导入模板（覆盖） */
  async function doImport(menuId: number, data: FormTemplate) {
    saving.value = true
    try {
      const dto: SaveTemplateDto & { name: string } = {
        name: data.name,
        codeLogic: data.codeLogic,
        fields: (data.fields ?? []).map(f => ({
          fieldName: f.fieldName,
          label: f.label,
          fieldType: f.fieldType,
          options: f.options,
          isRequired: f.isRequired,
          remark: f.remark,
          columnOrder: f.columnOrder
        }))
      }
      template.value = await importTemplate(menuId, dto)
    } finally {
      saving.value = false
    }
  }

  /** 清空当前模板（切换菜单时调用） */
  function reset() {
    template.value = null
  }

  return { template, loading, saving, loadByMenu, save, doExport, doImport, reset }
})
