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
/// 导出列偏好，记录每个功能模块用户最后一次勾选的导出列
/// </summary>
public class ExportPreference
{
    /// <summary>主键</summary>
    public int Id { get; set; }

    /// <summary>所属菜单Id</summary>
    public int MenuId { get; set; }

    /// <summary>已选中导出的列名（JSON数组）</summary>
    public string SelectedColumns { get; set; } = "[]";

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
