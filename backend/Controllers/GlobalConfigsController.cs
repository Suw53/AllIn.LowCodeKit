using AllIn.LowCodeKit.Backend.Data;
using AllIn.LowCodeKit.Backend.Models;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

namespace AllIn.LowCodeKit.Backend.Controllers;

/// <summary>
/// 全局配置接口：登录方案、Playwright路径、主题颜色等
/// </summary>
[ApiController]
public class GlobalConfigsController(AppDbContext db) : ControllerBase
{
    /// <summary>
    /// 获取全局配置列表，可按 category 过滤
    /// GET /api/global-configs?category=login
    /// </summary>
    [HttpGet("api/global-configs")]
    public async Task<IEnumerable<GlobalConfig>> GetAll([FromQuery] string? category)
    {
        var query = db.GlobalConfigs.AsQueryable();
        if (!string.IsNullOrEmpty(category))
            query = query.Where(g => g.Category == category);
        return await query.ToListAsync();
    }

    /// <summary>
    /// 设置单条全局配置（upsert）
    /// PUT /api/global-configs/{category}/{key}
    /// </summary>
    [HttpPut("api/global-configs/{category}/{key}")]
    public async Task<GlobalConfig> Upsert(string category, string key, [FromBody] UpsertConfigRequest req)
    {
        var config = await db.GlobalConfigs
            .FirstOrDefaultAsync(g => g.Category == category && g.Key == key);

        if (config == null)
        {
            config = new GlobalConfig { Category = category, Key = key };
            db.GlobalConfigs.Add(config);
        }

        config.Value = req.Value;
        config.Description = req.Description;
        await db.SaveChangesAsync();
        return config;
    }

    /// <summary>
    /// 删除全局配置项
    /// DELETE /api/global-configs/{category}/{key}
    /// </summary>
    [HttpDelete("api/global-configs/{category}/{key}")]
    public async Task<IActionResult> Delete(string category, string key)
    {
        var config = await db.GlobalConfigs
            .FirstOrDefaultAsync(g => g.Category == category && g.Key == key);
        if (config == null) return NotFound();
        db.GlobalConfigs.Remove(config);
        await db.SaveChangesAsync();
        return NoContent();
    }
}

/// <summary>设置配置请求体</summary>
public record UpsertConfigRequest(string? Value, string? Description);
