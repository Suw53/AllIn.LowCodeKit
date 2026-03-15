using AllIn.LowCodeKit.Backend.Data;
using AllIn.LowCodeKit.Backend.Models;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

namespace AllIn.LowCodeKit.Backend.Controllers;

/// <summary>
/// 表单模板管理接口：创建、查询、保存、导入导出
/// </summary>
[ApiController]
[Route("api/form-templates")]
public class FormTemplatesController : ControllerBase
{
    private readonly AppDbContext _db;

    public FormTemplatesController(AppDbContext db) => _db = db;

    /// <summary>
    /// 根据菜单Id获取表单模板（含字段列表，按Sort排序）
    /// </summary>
    [HttpGet("by-menu/{menuId:int}")]
    public async Task<IActionResult> GetByMenu(int menuId)
    {
        var template = await _db.FormTemplates
            .Include(t => t.Fields.OrderBy(f => f.Sort))
            .FirstOrDefaultAsync(t => t.MenuId == menuId);
        return Ok(template);
    }

    /// <summary>
    /// 创建新表单模板（空字段，后续通过 PUT 保存字段）
    /// </summary>
    [HttpPost]
    public async Task<IActionResult> Create([FromBody] CreateTemplateRequest req)
    {
        // 同一 MenuId 只允许一个模板
        var existing = await _db.FormTemplates.FirstOrDefaultAsync(t => t.MenuId == req.MenuId);
        if (existing != null)
            return BadRequest(new { message = "该菜单已存在表单模板" });

        var template = new FormTemplate
        {
            MenuId = req.MenuId,
            Name = req.Name,
            CodeLogic = req.CodeLogic ?? string.Empty
        };
        _db.FormTemplates.Add(template);
        await _db.SaveChangesAsync();
        return Ok(template);
    }

    /// <summary>
    /// 全量保存表单模板（模板信息 + 替换全部字段）
    /// </summary>
    [HttpPut("{id:int}")]
    public async Task<IActionResult> SaveFull(int id, [FromBody] SaveTemplateRequest req)
    {
        var template = await _db.FormTemplates
            .Include(t => t.Fields)
            .FirstOrDefaultAsync(t => t.Id == id);
        if (template == null) return NotFound(new { message = "模板不存在" });

        template.Name = req.Name;
        template.CodeLogic = req.CodeLogic;
        template.UpdatedAt = DateTime.Now;

        // 全量替换字段（删除旧字段，写入新字段）
        _db.FormFields.RemoveRange(template.Fields);
        template.Fields = req.Fields.Select((f, i) => new FormField
        {
            TemplateId = id,
            FieldName = f.FieldName,
            Label = f.Label,
            FieldType = f.FieldType,
            Options = f.Options,
            IsRequired = f.IsRequired,
            Remark = f.Remark,
            ColumnOrder = f.ColumnOrder,
            Sort = i
        }).ToList();

        await _db.SaveChangesAsync();

        // 重新查询，返回含 Id 的完整数据
        var result = await _db.FormTemplates
            .Include(t => t.Fields.OrderBy(f => f.Sort))
            .FirstAsync(t => t.Id == id);
        return Ok(result);
    }

    /// <summary>
    /// 导出表单模板为JSON（含字段列表）
    /// </summary>
    [HttpGet("{id:int}/export")]
    public async Task<IActionResult> Export(int id)
    {
        var template = await _db.FormTemplates
            .Include(t => t.Fields.OrderBy(f => f.Sort))
            .FirstOrDefaultAsync(t => t.Id == id);
        if (template == null) return NotFound(new { message = "模板不存在" });
        return Ok(template);
    }

    /// <summary>
    /// 导入表单模板（覆盖指定菜单的现有模板）
    /// </summary>
    [HttpPost("import/{menuId:int}")]
    public async Task<IActionResult> Import(int menuId, [FromBody] ImportTemplateRequest req)
    {
        // 删除已有模板
        var existing = await _db.FormTemplates
            .Include(t => t.Fields)
            .FirstOrDefaultAsync(t => t.MenuId == menuId);
        if (existing != null)
        {
            _db.FormFields.RemoveRange(existing.Fields);
            _db.FormTemplates.Remove(existing);
        }

        var template = new FormTemplate
        {
            MenuId = menuId,
            Name = req.Name,
            CodeLogic = req.CodeLogic,
            Fields = req.Fields.Select((f, i) => new FormField
            {
                FieldName = f.FieldName,
                Label = f.Label,
                FieldType = f.FieldType,
                Options = f.Options,
                IsRequired = f.IsRequired,
                Remark = f.Remark,
                ColumnOrder = f.ColumnOrder,
                Sort = i
            }).ToList()
        };
        _db.FormTemplates.Add(template);
        await _db.SaveChangesAsync();

        var result = await _db.FormTemplates
            .Include(t => t.Fields.OrderBy(f => f.Sort))
            .FirstAsync(t => t.Id == template.Id);
        return Ok(result);
    }

    /// <summary>
    /// 删除表单模板（级联删除字段）
    /// </summary>
    [HttpDelete("{id:int}")]
    public async Task<IActionResult> Delete(int id)
    {
        var template = await _db.FormTemplates
            .Include(t => t.Fields)
            .FirstOrDefaultAsync(t => t.Id == id);
        if (template == null) return NotFound(new { message = "模板不存在" });

        _db.FormFields.RemoveRange(template.Fields);
        _db.FormTemplates.Remove(template);
        await _db.SaveChangesAsync();
        return Ok();
    }
}

// ────────── 请求 DTO ──────────

/// <summary>创建模板请求</summary>
public record CreateTemplateRequest(int MenuId, string Name, string? CodeLogic);

/// <summary>全量保存模板请求</summary>
public record SaveTemplateRequest(string Name, string? CodeLogic, List<FieldDto> Fields);

/// <summary>导入模板请求</summary>
public record ImportTemplateRequest(string Name, string? CodeLogic, List<FieldDto> Fields);

/// <summary>字段数据传输对象</summary>
public record FieldDto(
    string FieldName,
    string Label,
    string FieldType,
    string? Options,
    bool IsRequired,
    string? Remark,
    int ColumnOrder
);
