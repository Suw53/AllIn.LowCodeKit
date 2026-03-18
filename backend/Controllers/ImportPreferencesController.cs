using AllIn.LowCodeKit.Backend.Data;
using AllIn.LowCodeKit.Backend.Models;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

namespace AllIn.LowCodeKit.Backend.Controllers;

/// <summary>
/// 导入偏好接口：记忆用户每个模块的导入模式和上次映射方案
/// </summary>
[ApiController]
public class ImportPreferencesController : ControllerBase
{
    private readonly AppDbContext _db;

    public ImportPreferencesController(AppDbContext db) => _db = db;

    /// <summary>
    /// 获取指定菜单的导入偏好（不存在返回 null）
    /// GET /api/menus/{menuId}/import-preference
    /// </summary>
    [HttpGet("api/menus/{menuId:int}/import-preference")]
    public async Task<IActionResult> Get(int menuId)
    {
        var pref = await _db.ImportPreferences
            .FirstOrDefaultAsync(p => p.MenuId == menuId);
        return Ok(pref);
    }

    /// <summary>
    /// 保存或更新导入偏好（upsert）
    /// PUT /api/menus/{menuId}/import-preference
    /// </summary>
    [HttpPut("api/menus/{menuId:int}/import-preference")]
    public async Task<IActionResult> Upsert(int menuId, [FromBody] ImportPreferenceRequest req)
    {
        var pref = await _db.ImportPreferences
            .FirstOrDefaultAsync(p => p.MenuId == menuId);

        if (pref == null)
        {
            pref = new ImportPreference { MenuId = menuId };
            _db.ImportPreferences.Add(pref);
        }

        pref.UseMappingEnabled = req.UseMappingEnabled;
        pref.LastMappingConfigId = req.LastMappingConfigId;
        pref.UpdatedAt = DateTime.Now;
        await _db.SaveChangesAsync();
        return Ok(pref);
    }
}

/// <summary>保存导入偏好请求</summary>
public record ImportPreferenceRequest(bool UseMappingEnabled, int? LastMappingConfigId);
