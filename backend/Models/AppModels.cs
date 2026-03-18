namespace AllIn.LowCodeKit.Backend.Models;

/// <summary>
/// 高级筛选方案，支持按功能模块保存多套筛选条件
/// </summary>
public class FilterScheme
{
    /// <summary>主键</summary>
    public int Id { get; set; }

    /// <summary>所属菜单Id</summary>
    public int MenuId { get; set; }

    /// <summary>方案名称</summary>
    public string Name { get; set; } = string.Empty;

    /// <summary>筛选条件配置（JSON）</summary>
    public string Config { get; set; } = "[]";

    /// <summary>创建时间</summary>
    public DateTime CreatedAt { get; set; } = DateTime.Now;
}

/// <summary>
/// 导出列偏好，记录每个功能模块用户最后一次勾选的导出列及列可见性
/// </summary>
public class ExportPreference
{
    /// <summary>主键</summary>
    public int Id { get; set; }

    /// <summary>所属菜单Id</summary>
    public int MenuId { get; set; }

    /// <summary>已选中导出的列名（JSON数组）</summary>
    public string SelectedColumns { get; set; } = "[]";

    /// <summary>列表可见列名（JSON数组），null 表示显示全部列</summary>
    public string? VisibleColumns { get; set; }

    /// <summary>更新时间</summary>
    public DateTime UpdatedAt { get; set; } = DateTime.Now;
}

/// <summary>
/// 自动化流程配置
/// </summary>
public class AutomationConfig
{
    /// <summary>主键</summary>
    public int Id { get; set; }

    /// <summary>所属菜单Id</summary>
    public int MenuId { get; set; }

    /// <summary>配置名称</summary>
    public string Name { get; set; } = string.Empty;

    /// <summary>C#脚本代码</summary>
    public string ScriptCode { get; set; } = string.Empty;

    /// <summary>关联的全局登录配置Id</summary>
    public int? LoginConfigId { get; set; }

    /// <summary>创建时间</summary>
    public DateTime CreatedAt { get; set; } = DateTime.Now;

    /// <summary>更新时间</summary>
    public DateTime UpdatedAt { get; set; } = DateTime.Now;
}

/// <summary>
/// 导入模板配置，定义导入时包含哪些字段
/// </summary>
public class ImportTemplateConfig
{
    /// <summary>主键</summary>
    public int Id { get; set; }

    /// <summary>所属菜单Id</summary>
    public int MenuId { get; set; }

    /// <summary>配置名称</summary>
    public string Name { get; set; } = string.Empty;

    /// <summary>包含的字段名（JSON数组）</summary>
    public string FieldNames { get; set; } = "[]";

    /// <summary>创建时间</summary>
    public DateTime CreatedAt { get; set; } = DateTime.Now;

    /// <summary>更新时间</summary>
    public DateTime UpdatedAt { get; set; } = DateTime.Now;
}

/// <summary>
/// 导入映射配置，定义Excel列与表单字段的映射关系及转换脚本
/// </summary>
public class ImportMappingConfig
{
    /// <summary>主键</summary>
    public int Id { get; set; }

    /// <summary>所属菜单Id</summary>
    public int MenuId { get; set; }

    /// <summary>映射配置名称</summary>
    public string Name { get; set; } = string.Empty;

    /// <summary>映射规则（JSON数组），每项包含 sourceColumn/targetField/transformScript</summary>
    public string Mappings { get; set; } = "[]";

    /// <summary>创建时间</summary>
    public DateTime CreatedAt { get; set; } = DateTime.Now;

    /// <summary>更新时间</summary>
    public DateTime UpdatedAt { get; set; } = DateTime.Now;
}

/// <summary>
/// 全局配置项
/// </summary>
public class GlobalConfig
{
    /// <summary>主键</summary>
    public int Id { get; set; }

    /// <summary>配置分类（Login / Playwright / General）</summary>
    public string Category { get; set; } = string.Empty;

    /// <summary>配置键</summary>
    public string Key { get; set; } = string.Empty;

    /// <summary>配置值</summary>
    public string? Value { get; set; }

    /// <summary>描述</summary>
    public string? Description { get; set; }
}

/// <summary>
/// 导入偏好，记忆每个模块的导入模式和上次使用的映射方案
/// </summary>
public class ImportPreference
{
    /// <summary>主键</summary>
    public int Id { get; set; }

    /// <summary>所属菜单Id（唯一）</summary>
    public int MenuId { get; set; }

    /// <summary>是否启用字段映射模式</summary>
    public bool UseMappingEnabled { get; set; }

    /// <summary>上次使用的映射配置Id（可空）</summary>
    public int? LastMappingConfigId { get; set; }

    /// <summary>更新时间</summary>
    public DateTime UpdatedAt { get; set; } = DateTime.Now;
}
