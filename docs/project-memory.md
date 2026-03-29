# AllIn.LowCodeKit 项目记忆

> 最后基于当前代码整理时间：2026-03-29

## 一、使用原则

- 本文件必须以当前仓库代码和实际构建结果为准。
- 旧的 Claude 计划只可作为历史背景参考，不能高于当前代码事实。
- 开始任何任务前，优先阅读本文件和 `docs/project-plan.md`。
- 当任务引入新的稳定事实、明确决策或计划变化时，先询问用户是否同步更新文档。

## 二、项目定位

- 本项目是一个本地低代码桌面工具。
- 核心目标是让用户通过可视化方式完成：
  - 菜单配置
  - 表单设计
  - 动态数据录入与管理
  - Excel 导入导出
  - 基于 Playwright 的自动化脚本配置与执行

## 三、当前代码结构

- 前端目录：`frontend/`
- 后端目录：`backend/`
- 桌面壳：Tauri
- 前端主栈：
  - Vue 3
  - TypeScript
  - Pinia
  - Vue Router
  - Element Plus
  - VXE-Table
  - Monaco Editor
  - Vite
- 后端主栈：
  - ASP.NET Core Web API
  - .NET 10
  - EF Core
  - SQLite
  - NPOI
  - Microsoft.Playwright
  - Roslyn C# Scripting

## 四、运行时稳定事实

- 后端固定监听：`http://localhost:5000`
- 前端路由模式：Hash
- SQLite 数据库路径：`LocalApplicationData/AllIn.LowCodeKit/app.db`
- 后端启动时自动执行 EF Core 迁移

## 五、当前菜单与路由真相

### 1. 菜单模型

- 当前菜单是两级结构，不是旧计划中的三级结构。
- `Menus` 表支持一级菜单和二级菜单。
- 系统内置菜单在当前种子数据中为：
  - `Id=1`：全局配置
  - `Id=100`：自动化配置，父级为全局配置
  - `Id=101`：系统配置，父级为全局配置

### 2. 正式路由入口

- `/home`
- `/module/:menuId`
- `/form-designer/:menuId`
- `/global-config/automation`
- `/global-config/system`
- `/automation/:menuId`

### 3. 兼容入口

- `/global-config` 当前只是兼容跳转入口：
  - `tab=login` 或 `tab=playwright` 时跳到 `/global-config/automation`
  - 其他情况跳到 `/global-config/system`

### 4. 遗留页面

- `frontend/src/views/GlobalConfigView.vue` 仍在仓库中。
- 但从当前路由看，它已经不是正式入口页面。
- 后续涉及全局配置改动时，默认优先修改：
  - `frontend/src/views/AutomationConfigView.vue`
  - `frontend/src/views/SystemConfigView.vue`
- 不要未经确认直接把 `GlobalConfigView.vue` 当作主入口改动。

## 六、已实现功能版图

### 1. 菜单管理

- 左侧菜单树展示
- 右键新增一级菜单
- 右键新增二级菜单
- 菜单重命名
- 菜单删除
- 菜单图标选择
- 系统菜单不可删除
- 新增二级菜单后会跳转到表单设计器

### 2. 表单设计器

- 支持文本框、下拉框字段
- 支持字段标签、字段名、必填、备注、列顺序
- 支持 `Span` 布局：
  - `1` 表示半宽
  - `2` 表示全宽
- 支持拖拽增删字段
- 支持代码模式
- 支持可视化必填规则与代码 `AUTO:REQUIRED` 区块双向同步
- 支持模板 JSON 导入
- 支持模板 JSON 导出
- 表单模板保存使用单接口 upsert，保存时会全量替换字段定义

### 3. 动态数据管理

- 每个业务模块对应一张动态表：`DynamicData_{menuId}`
- 系统列固定包含：
  - `Id`
  - `CreatedAt`
  - `UpdatedAt`
  - `_BatchId`
- 动态表在运行时按表单字段自动创建和补列
- 支持：
  - 分页查询
  - 关键词搜索
  - 高级筛选
  - 单行新增
  - 单行编辑
  - 单行删除
  - 批量删除
  - 批次过滤
  - 批次历史统计

### 4. 筛选与导出偏好

- 支持筛选方案保存、更新、删除
- 支持导出列偏好持久化
- `ExportPreference` 当前同时包含：
  - 已选导出列
  - 可见列状态

### 5. Excel 导入导出

- 支持导入模板下载
- 支持基于导入模板配置决定模板字段集
- 支持 Excel 导入预览
- 支持导入确认
- 支持导入批次号写入 `_BatchId`
- 支持导入模板配置
- 支持导入映射配置
- 支持导入偏好持久化

### 6. 导入映射与转换脚本

- 导入映射配置支持定义：
  - Excel 源列
  - 目标字段
  - 转换脚本
- 后端已有 `ImportTransformService`
- 转换脚本使用 Roslyn 执行
- 当前导入转换脚本可直接使用的全局变量包括：
  - `Value`
  - `SourceColumn`
  - `TargetField`
- 转换脚本有缓存与超时保护

### 7. 自动化能力

- 每个模块支持独立的自动化配置
- 自动化页面支持：
  - 配置名称
  - C# 脚本编辑
  - 选择登录方案
  - 手动填写 CDP 地址
  - 执行日志查看
- 后端自动化执行链路已经打通：
  - Playwright `ConnectOverCDP`
  - Roslyn `CSharpScript`
  - 将 `IPage` 注入脚本环境执行

### 8. 全局配置与系统配置

- 当前全局配置已拆成两个正式页面：
  - 自动化配置页
  - 系统配置页
- 自动化配置页当前负责：
  - 登录方案
  - Playwright 路径
- 系统配置页当前负责：
  - 应用标题
  - 侧边栏名称
  - Logo
  - Favicon
  - 主题颜色
- 系统配置页当前已经支持：
  - 选择 Logo 后即时预览
  - 选择 Favicon 后即时预览
  - 选择 Favicon 后立即应用到页面标签图标
  - 保存后持久化 Logo 与 Favicon 配置
- 主题配置通过 CSS 变量驱动
- 应用启动时会加载：
  - 主题配置
  - 应用配置

## 七、重要数据模型

- `Menus`
- `FormTemplates`
- `FormFields`
- `FilterSchemes`
- `ExportPreferences`
- `AutomationConfigs`
- `ImportTemplateConfigs`
- `ImportMappingConfigs`
- `ImportPreferences`
- `GlobalConfigs`
- 运行时动态表：`DynamicData_{menuId}`

## 八、关键文件定位

### 1. 后端关键文件

- `backend/Program.cs`
- `backend/Data/AppDbContext.cs`
- `backend/Controllers/MenusController.cs`
- `backend/Controllers/FormTemplatesController.cs`
- `backend/Controllers/DynamicDataController.cs`
- `backend/Controllers/AutomationConfigsController.cs`
- `backend/Controllers/GlobalConfigsController.cs`
- `backend/Services/DynamicDataService.cs`
- `backend/Services/RoslynScriptEngine.cs`
- `backend/Services/ImportTransformService.cs`

### 2. 前端关键文件

- `frontend/src/router/index.ts`
- `frontend/src/components/SidebarMenu.vue`
- `frontend/src/views/FormDesignerView.vue`
- `frontend/src/views/ModuleView.vue`
- `frontend/src/views/AutomationView.vue`
- `frontend/src/views/AutomationConfigView.vue`
- `frontend/src/views/SystemConfigView.vue`
- `frontend/src/stores/menuStore.ts`
- `frontend/src/stores/formTemplateStore.ts`
- `frontend/src/stores/moduleStore.ts`
- `frontend/src/stores/themeStore.ts`
- `frontend/src/stores/appConfigStore.ts`
- `frontend/src/components/ImportExcelDialog.vue`
- `frontend/src/components/ImportMappingDialog.vue`
- `frontend/src/components/ImportPreviewDialog.vue`

## 九、当前已验证状态

- 2026-03-29：`backend/` 下 `dotnet build` 已通过
- 2026-03-29：`frontend/` 下 `npx.cmd vue-tsc -b` 已通过
- 2026-03-29：`frontend/` 下 `npm.cmd run build` 已通过
- 2026-03-29：`frontend/src/views/AutomationView.vue` 中 `DEFAULT_SCRIPT` 先使用后声明的问题已修复
- 2026-03-29：`frontend/src/views/SystemConfigView.vue` 中 Logo/Favicon 的选择、预览和保存生效链路已修复
- 2026-03-29：`frontend/src/components/ImportExcelDialog.vue` 与 `frontend/src/components/ImportMappingDialog.vue` 的启动期语法/类型问题已修复
- 2026-03-29：工作区已补充 `.vscode/settings.json`、`.vscode/tasks.json`、`.vscode/extensions.json`，用于降低 Vue 扩展误报和 VS Code 启动任务失败的概率

## 十、当前已知实现特征

- `ModuleView.vue` 仍然承担了较多主流程职责，是当前前端耦合度最高的页面之一
- `moduleStore.ts` 已经成为导入、导出、筛选、批次、偏好的统一状态入口
- 全局配置拆页后的主入口已经稳定，但遗留页还未彻底清理
- 当前代码明显处于“功能基本齐备，等待清理和验收”的状态，而不是“早期功能搭建期”

## 十一、前端开发环境记忆点

- 本仓库的 Vue SFC 开发与诊断，统一以 `Vue - Official` 和 `vue-tsc` 为准
- 不应再依赖 `Vetur` 对 `.vue` 文件做语法和类型诊断
- 工作区已经明确：
  - 推荐扩展：`Vue - Official`
  - 不推荐扩展：`Vetur`
  - 已关闭 `Vetur` 的模板、脚本、样式校验
- VS Code 的前置启动任务当前要求：
  - 前端使用 `process + npm.cmd run dev`
  - 后端使用 `process + dotnet run`
- 这套配置是为了避免再次出现以下问题：
  - `.vue` 文件代码实际可编译，但被旧扩展误报为别名找不到、宏找不到、默认导出错误
  - `preLaunchTask` 因 shell 环境差异导致前端启动失败
- 后续新建或修改 Vue 组件时，优先保证：
  - 先过 `npx.cmd vue-tsc -b`
  - 再看编辑器提示
  - 如果编辑器报错与 `vue-tsc` 结果冲突，优先排查是否是扩展误报
- 为兼容当前团队环境，新写 `defineEmits` 时优先使用重载签名写法，不优先使用对旧扩展更敏感的命名元组写法

## 十二、后续默认执行准则

- 后续任务优先以当前代码为主，不以旧计划为主
- 如果涉及全局配置，先确认修改的是正式入口页还是遗留页
- 如果涉及导入导出或自动化，优先检查 `ModuleView.vue`、`moduleStore.ts` 和对应后端 Controller/Service
- 如果任务结束后形成新的稳定认知，先询问用户是否同步更新本文件和 `docs/project-plan.md`
