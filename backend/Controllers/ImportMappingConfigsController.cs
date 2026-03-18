using AllIn.LowCodeKit.Backend.Data;
using AllIn.LowCodeKit.Backend.Models;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

namespace AllIn.LowCodeKit.Backend.Controllers;

/// <summary>
/// 导入映射配置接口：管理Excel列与表单字段的映射关系
/// </summary>
[ApiController]
public class ImportMappingConfigsController : ControllerBase
{
    private readonly AppDbContext _db;

    public ImportMappingConfigsController(AppDbContext db) => _db = db;

    /// <summary>
    /// 获取指定菜单的所有映射配置
    /// GET /api/menus/{menuId}/import-mapping-configs
    /// </summary>
    [HttpGet("api/menus/{menuId:int}/import-mapping-configs")]
    public async Task<IActionResult> GetAll(int menuId)
    {
        var configs = await _db.ImportMappingConfigs
            .Where(c => c.MenuId == menuId)
            .OrderByDescending(c => c.CreatedAt)
            .ToListAsync();
        return Ok(configs);
    }

    /// <summary>
    /// 新建映射配置
    /// POST /api/menus/{menuId}/import-mapping-configs
    /// </summary>
    [HttpPost("api/menus/{menuId:int}/import-mapping-configs")]
    public async Task<IActionResult> Create(int menuId, [FromBody] ImportMappingConfigRequest req)
    {
        var config = new ImportMappingConfig
        {
            MenuId = menuId,
            Name = req.Name,
            Mappings = req.Mappings
        };
        _db.ImportMappingConfigs.Add(config);
        await _db.SaveChangesAsync();
        return Ok(config);
    }

    /// <summary>
    /// 更新映射配置
    /// PUT /api/import-mapping-configs/{id}
    /// </summary>
    [HttpPut("api/import-mapping-configs/{id:int}")]
    public async Task<IActionResult> Update(int id, [FromBody] ImportMappingConfigRequest req)
    {
        var config = await _db.ImportMappingConfigs.FindAsync(id);
        if (config == null) return NotFound();
        config.Name = req.Name;
        config.Mappings = req.Mappings;
        config.UpdatedAt = DateTime.Now;
        await _db.SaveChangesAsync();
        return Ok(config);
    }

    /// <summary>
    /// 删除映射配置
    /// DELETE /api/import-mapping-configs/{id}
    /// </summary>
    [HttpDelete("api/import-mapping-configs/{id:int}")]
    public async Task<IActionResult> Delete(int id)
    {
        var config = await _db.ImportMappingConfigs.FindAsync(id);
        if (config == null) return NotFound();
        _db.ImportMappingConfigs.Remove(config);
        await _db.SaveChangesAsync();
        return NoContent();
    }
}

/// <summary>导入映射配置请求</summary>
public record ImportMappingConfigRequest(string Name, string Mappings);
