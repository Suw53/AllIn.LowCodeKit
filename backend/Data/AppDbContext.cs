using AllIn.LowCodeKit.Backend.Models;
using Microsoft.EntityFrameworkCore;

namespace AllIn.LowCodeKit.Backend.Data;

/// <summary>
/// 应用数据库上下文，使用SQLite本地存储
/// </summary>
public class AppDbContext : DbContext
{
    public AppDbContext(DbContextOptions<AppDbContext> options) : base(options) { }

    /// <summary>菜单表</summary>
    public DbSet<Menu> Menus { get; set; }

    /// <summary>表单模板表</summary>
    public DbSet<FormTemplate> FormTemplates { get; set; }

    /// <summary>表单字段表</summary>
    public DbSet<FormField> FormFields { get; set; }

    /// <summary>筛选方案表</summary>
    public DbSet<FilterScheme> FilterSchemes { get; set; }

    /// <summary>导出列偏好表</summary>
    public DbSet<ExportPreference> ExportPreferences { get; set; }

    /// <summary>自动化配置表</summary>
    public DbSet<AutomationConfig> AutomationConfigs { get; set; }

    /// <summary>全局配置表</summary>
    public DbSet<GlobalConfig> GlobalConfigs { get; set; }

    /// <summary>导入模板配置表</summary>
    public DbSet<ImportTemplateConfig> ImportTemplateConfigs { get; set; }

    /// <summary>导入映射配置表</summary>
    public DbSet<ImportMappingConfig> ImportMappingConfigs { get; set; }

    /// <summary>导入偏好表</summary>
    public DbSet<ImportPreference> ImportPreferences { get; set; }

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        base.OnModelCreating(modelBuilder);

        // 菜单自引用关系
        modelBuilder.Entity<Menu>()
            .HasMany(m => m.Children)
            .WithOne(m => m.Parent)
            .HasForeignKey(m => m.ParentId)
            .OnDelete(DeleteBehavior.Restrict);

        // 表单模板与字段的一对多关系
        modelBuilder.Entity<FormTemplate>()
            .HasMany(t => t.Fields)
            .WithOne(f => f.Template)
            .HasForeignKey(f => f.TemplateId)
            .OnDelete(DeleteBehavior.Cascade);

        // 全局配置唯一索引
        modelBuilder.Entity<GlobalConfig>()
            .HasIndex(g => new { g.Category, g.Key })
            .IsUnique();

        // 种子数据：默认全局配置一级菜单
        modelBuilder.Entity<Menu>().HasData(
            new Menu
            {
                Id = 1,
                ParentId = null,
                Name = "全局配置",
                Icon = "Setting",
                Sort = 9999,
                IsSystem = true,
                CreatedAt = new DateTime(2026, 3, 15)
            }
        );
    }
}
