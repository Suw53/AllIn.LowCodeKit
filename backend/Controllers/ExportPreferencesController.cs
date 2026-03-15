using AllIn.LowCodeKit.Backend.Data;
using AllIn.LowCodeKit.Backend.Models;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

namespace AllIn.LowCodeKit.Backend.Controllers;

/// <summary>
/// 导出列偏好接口：记忆用户每个模块最后一次勾选的导出列
/// </summary>
[ApiController]
public class ExportPreferencesController : ControllerBase
{
    private readonly AppDbContext _db;

    public ExportPreferencesController(AppDbContext db) => _db = db;

    /// <summary>
    /// 获取指定菜单的导出列偏好（不存在返回 null）
    /// GET /api/menus/{menuId}/export-preference
    /// </summary>
    [HttpGet("api/menus/{menuId:int}/export-preference")]
    public async Task<IActionResult> Get(int menuId)
    {
        var pref = await _db.ExportPreferences
            .FirstOrDefaultAsync(p => p.MenuId == menuId);
        return Ok(pref);
    }

    /// <summary>
    /// 保存或更新导出列偏好（upsert）
    /// PUT /api/menus/{menuId}/export-preference
    /// </summary>
    [HttpPut("api/menus/{menuId:int}/export-preference")]
    public async Task<IActionResult> Upsert(int menuId, [FromBody] ExportPreferenceRequest req)
    {
        var pref = await _db.ExportPreferences
            .FirstOrDefaultAsync(p => p.MenuId == menuId);

        if (pref == null)
        {
            pref = new ExportPreference { MenuId = menuId };
            _db.ExportPreferences.Add(pref);
        }

        pref.SelectedColumns = req.SelectedColumns;
        pref.UpdatedAt = DateTime.Now;
        await _db.SaveChangesAsync();
        return Ok(pref);
    }
}

/// <summary>保存导出列偏好请求</summary>
public record ExportPreferenceRequest(string SelectedColumns);
