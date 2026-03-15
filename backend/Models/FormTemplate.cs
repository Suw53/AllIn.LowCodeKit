namespace AllIn.LowCodeKit.Backend.Models;

/// <summary>
/// 表单模板，对应一个功能模块的完整表单设计
/// </summary>
public class FormTemplate
{
    /// <summary>主键</summary>
    public int Id { get; set; }

    /// <summary>关联菜单Id（二级菜单）</summary>
    public int MenuId { get; set; }

    /// <summary>模板名称</summary>
    public string Name { get; set; } = string.Empty;

    /// <summary>代码模式中的C#校验逻辑</summary>
    public string? CodeLogic { get; set; }

    /// <summary>创建时间</summary>
    public DateTime CreatedAt { get; set; } = DateTime.Now;

    /// <summary>更新时间</summary>
    public DateTime UpdatedAt { get; set; } = DateTime.Now;

    /// <summary>字段列表导航属性</summary>
    public List<FormField> Fields { get; set; } = new();
}

/// <summary>
/// 表单字段定义
/// </summary>
public class FormField
{
    /// <summary>主键</summary>
    public int Id { get; set; }

    /// <summary>所属模板Id</summary>
    public int TemplateId { get; set; }

    /// <summary>数据库字段名（英文，PascalCase）</summary>
    public string FieldName { get; set; } = string.Empty;

    /// <summary>界面显示标签名</summary>
    public string Label { get; set; } = string.Empty;

    /// <summary>字段类型：Text / Select</summary>
    public string FieldType { get; set; } = "Text";

    /// <summary>下拉选项值（JSON数组，仅FieldType=Select时有效）</summary>
    public string? Options { get; set; }

    /// <summary>是否必填</summary>
    public bool IsRequired { get; set; }

    /// <summary>批注描述（用于Excel导入模板列头批注）</summary>
    public string? Remark { get; set; }

    /// <summary>列头在列表中的显示顺序</summary>
    public int ColumnOrder { get; set; }

    /// <summary>在表单设计器中的排列顺序</summary>
    public int Sort { get; set; }

    /// <summary>模板导航属性</summary>
    public FormTemplate? Template { get; set; }
}
