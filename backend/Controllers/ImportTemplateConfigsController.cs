using AllIn.LowCodeKit.Backend.Data;
using AllIn.LowCodeKit.Backend.Models;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

namespace AllIn.LowCodeKit.Backend.Controllers;

/// <summary>
/// 导入模板配置接口：管理每个功能模块的导入模板（选择包含哪些字段）
/// </summary>
[ApiController]
public class ImportTemplateConfigsController : ControllerBase
{
    private readonly AppDbContext _db;

    public ImportTemplateConfigsController(AppDbContext db) => _db = db;

    /// <summary>
    /// 获取指定菜单的所有导入模板配置
    /// GET /api/menus/{menuId}/import-template-configs
    /// </summary>
    [HttpGet("api/menus/{menuId:int}/import-template-configs")]
    public async Task<IActionResult> GetAll(int menuId)
    {
        var configs = await _db.ImportTemplateConfigs
            .Where(c => c.MenuId == menuId)
            .OrderByDescending(c => c.CreatedAt)
            .ToListAsync();
        return Ok(configs);
    }

    /// <summary>
    /// 新建导入模板配置
    /// POST /api/menus/{menuId}/import-template-configs
    /// </summary>
    [HttpPost("api/menus/{menuId:int}/import-template-configs")]
    public async Task<IActionResult> Create(int menuId, [FromBody] ImportTemplateConfigRequest req)
    {
        var config = new ImportTemplateConfig
        {
            MenuId = menuId,
            Name = req.Name,
            FieldNames = req.FieldNames
        };
        _db.ImportTemplateConfigs.Add(config);
        await _db.SaveChangesAsync();
        return Ok(config);
    }

    /// <summary>
    /// 更新导入模板配置
    /// PUT /api/import-template-configs/{id}
    /// </summary>
    [HttpPut("api/import-template-configs/{id:int}")]
    public async Task<IActionResult> Update(int id, [FromBody] ImportTemplateConfigRequest req)
    {
        var config = await _db.ImportTemplateConfigs.FindAsync(id);
        if (config == null) return NotFound();
        config.Name = req.Name;
        config.FieldNames = req.FieldNames;
        config.UpdatedAt = DateTime.Now;
        await _db.SaveChangesAsync();
        return Ok(config);
    }

    /// <summary>
    /// 删除导入模板配置
    /// DELETE /api/import-template-configs/{id}
    /// </summary>
    [HttpDelete("api/import-template-configs/{id:int}")]
    public async Task<IActionResult> Delete(int id)
    {
        var config = await _db.ImportTemplateConfigs.FindAsync(id);
        if (config == null) return NotFound();
        _db.ImportTemplateConfigs.Remove(config);
        await _db.SaveChangesAsync();
        return NoContent();
    }
}

/// <summary>导入模板配置请求</summary>
public record ImportTemplateConfigRequest(string Name, string FieldNames);
