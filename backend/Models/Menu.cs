namespace AllIn.LowCodeKit.Backend.Models;

/// <summary>
/// 菜单实体，支持一级目录和二级功能模块
/// </summary>
public class Menu
{
    /// <summary>主键</summary>
    public int Id { get; set; }

    /// <summary>父级Id，null表示一级菜单</summary>
    public int? ParentId { get; set; }

    /// <summary>菜单名称</summary>
    public string Name { get; set; } = string.Empty;

    /// <summary>图标</summary>
    public string? Icon { get; set; }

    /// <summary>排序</summary>
    public int Sort { get; set; }

    /// <summary>是否系统内置（不可删除）</summary>
    public bool IsSystem { get; set; }

    /// <summary>创建时间</summary>
    public DateTime CreatedAt { get; set; } = DateTime.Now;

    /// <summary>子菜单导航属性</summary>
    public List<Menu> Children { get; set; } = new();

    /// <summary>父菜单导航属性</summary>
    public Menu? Parent { get; set; }
}
