using AllIn.LowCodeKit.Backend.Data;
using AllIn.LowCodeKit.Backend.Models;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

namespace AllIn.LowCodeKit.Backend.Controllers;

/// <summary>
/// 高级筛选方案接口：保存和管理每个功能模块的筛选条件方案
/// </summary>
[ApiController]
public class FilterSchemesController : ControllerBase
{
    private readonly AppDbContext _db;

    public FilterSchemesController(AppDbContext db) => _db = db;

    /// <summary>
    /// 获取指定菜单的所有筛选方案
    /// GET /api/menus/{menuId}/filter-schemes
    /// </summary>
    [HttpGet("api/menus/{menuId:int}/filter-schemes")]
    public async Task<IActionResult> GetAll(int menuId)
    {
        var schemes = await _db.FilterSchemes
            .Where(s => s.MenuId == menuId)
            .OrderByDescending(s => s.CreatedAt)
            .ToListAsync();
        return Ok(schemes);
    }

    /// <summary>
    /// 保存一个筛选方案
    /// POST /api/menus/{menuId}/filter-schemes
    /// </summary>
    [HttpPost("api/menus/{menuId:int}/filter-schemes")]
    public async Task<IActionResult> Create(int menuId, [FromBody] FilterSchemeRequest req)
    {
        var scheme = new FilterScheme
        {
            MenuId = menuId,
            Name = req.Name,
            Config = req.Config
        };
        _db.FilterSchemes.Add(scheme);
        await _db.SaveChangesAsync();
        return Ok(scheme);
    }

    /// <summary>
    /// 更新筛选方案（名称和条件）
    /// PUT /api/filter-schemes/{id}
    /// </summary>
    [HttpPut("api/filter-schemes/{id:int}")]
    public async Task<IActionResult> Update(int id, [FromBody] FilterSchemeRequest req)
    {
        var scheme = await _db.FilterSchemes.FindAsync(id);
        if (scheme == null) return NotFound();
        scheme.Name = req.Name;
        scheme.Config = req.Config;
        await _db.SaveChangesAsync();
        return Ok(scheme);
    }

    /// <summary>
    /// 删除筛选方案
    /// DELETE /api/filter-schemes/{id}
    /// </summary>
    [HttpDelete("api/filter-schemes/{id:int}")]
    public async Task<IActionResult> Delete(int id)
    {
        var scheme = await _db.FilterSchemes.FindAsync(id);
        if (scheme == null) return NotFound();
        _db.FilterSchemes.Remove(scheme);
        await _db.SaveChangesAsync();
        return NoContent();
    }
}

/// <summary>创建筛选方案请求</summary>
public record FilterSchemeRequest(string Name, string Config);
