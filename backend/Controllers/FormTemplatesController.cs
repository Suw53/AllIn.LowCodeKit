using AllIn.LowCodeKit.Backend.Data;
using AllIn.LowCodeKit.Backend.Models;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

namespace AllIn.LowCodeKit.Backend.Controllers;

/// <summary>
/// 表单模板管理接口
/// 资源路由：
///   GET    /api/menus/{menuId}/form-template   → 获取菜单的表单模板
///   PUT    /api/menus/{menuId}/form-template   → 创建或全量更新（upsert）
///   GET    /api/form-templates/{id}            → 按模板Id获取（供前端导出下载）
///   DELETE /api/form-templates/{id}            → 删除模板
/// </summary>
[ApiController]
public class FormTemplatesController : ControllerBase
{
    private readonly AppDbContext _db;

    public FormTemplatesController(AppDbContext db) => _db = db;

    /// <summary>
    /// 获取指定菜单的表单模板（含字段，按 Sort 排序），不存在返回 null
    /// GET /api/menus/{menuId}/form-template
    /// </summary>
    [HttpGet("api/menus/{menuId:int}/form-template")]
    public async Task<IActionResult> GetByMenu(int menuId)
    {
        var template = await _db.FormTemplates
            .Include(t => t.Fields.OrderBy(f => f.Sort))
            .FirstOrDefaultAsync(t => t.MenuId == menuId);
        return Ok(template);
    }

    /// <summary>
    /// 创建或全量更新表单模板（upsert）：不存在则创建，存在则全量替换字段
    /// 同时作为导入接口：前端解析 JSON 后直接 PUT 即可
    /// PUT /api/menus/{menuId}/form-template
    /// </summary>
    [HttpPut("api/menus/{menuId:int}/form-template")]
    public async Task<IActionResult> Upsert(int menuId, [FromBody] SaveTemplateRequest req)
    {
        var template = await _db.FormTemplates
            .Include(t => t.Fields)
            .FirstOrDefaultAsync(t => t.MenuId == menuId);

        if (template == null)
        {
            template = new FormTemplate { MenuId = menuId };
            _db.FormTemplates.Add(template);
        }
        else
        {
            _db.FormFields.RemoveRange(template.Fields);
        }

        template.Name = req.Name;
        template.CodeLogic = req.CodeLogic;
        template.UpdatedAt = DateTime.Now;
        template.Fields = req.Fields.Select((f, i) => new FormField
        {
            FieldName = f.FieldName,
            Label = f.Label,
            FieldType = f.FieldType,
            Options = f.Options,
            IsRequired = f.IsRequired,
            Remark = f.Remark,
            ColumnOrder = f.ColumnOrder,
            Span = f.Span,
            Sort = i
        }).ToList();

        await _db.SaveChangesAsync();

        var result = await _db.FormTemplates
            .Include(t => t.Fields.OrderBy(f => f.Sort))
            .FirstAsync(t => t.MenuId == menuId);
        return Ok(result);
    }

    /// <summary>
    /// 按模板 Id 获取完整模板数据（前端用于导出下载 JSON）
    /// GET /api/form-templates/{id}
    /// </summary>
    [HttpGet("api/form-templates/{id:int}")]
    public async Task<IActionResult> GetById(int id)
    {
        var template = await _db.FormTemplates
            .Include(t => t.Fields.OrderBy(f => f.Sort))
            .FirstOrDefaultAsync(t => t.Id == id);
        if (template == null) return NotFound(new { message = "模板不存在" });
        return Ok(template);
    }

    /// <summary>
    /// 删除表单模板（级联删除字段）
    /// DELETE /api/form-templates/{id}
    /// </summary>
    [HttpDelete("api/form-templates/{id:int}")]
    public async Task<IActionResult> Delete(int id)
    {
        var template = await _db.FormTemplates
            .Include(t => t.Fields)
            .FirstOrDefaultAsync(t => t.Id == id);
        if (template == null) return NotFound(new { message = "模板不存在" });

        _db.FormFields.RemoveRange(template.Fields);
        _db.FormTemplates.Remove(template);
        await _db.SaveChangesAsync();
        return NoContent();
    }
}

// ────────── 请求 DTO ──────────

/// <summary>创建或更新模板请求体</summary>
public record SaveTemplateRequest(string Name, string? CodeLogic, List<FieldDto> Fields);

/// <summary>字段数据传输对象</summary>
public record FieldDto(
    string FieldName,
    string Label,
    string FieldType,
    string? Options,
    bool IsRequired,
    string? Remark,
    int ColumnOrder,
    int Span
);
