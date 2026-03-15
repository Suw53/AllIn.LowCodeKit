using AllIn.LowCodeKit.Backend.Data;
using AllIn.LowCodeKit.Backend.Models;
using AllIn.LowCodeKit.Backend.Services;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using System.Text.Json;

namespace AllIn.LowCodeKit.Backend.Controllers;

/// <summary>
/// 自动化配置接口：保存/读取C#脚本，执行自动化流程
/// </summary>
[ApiController]
public class AutomationConfigsController(AppDbContext db, RoslynScriptEngine engine) : ControllerBase
{
    /// <summary>
    /// 获取指定菜单的自动化配置
    /// GET /api/menus/{menuId}/automation
    /// </summary>
    [HttpGet("api/menus/{menuId}/automation")]
    public async Task<ActionResult<AutomationConfigDto>> GetByMenu(int menuId)
    {
        var config = await db.AutomationConfigs.FirstOrDefaultAsync(a => a.MenuId == menuId);
        if (config == null) return NotFound();
        return new AutomationConfigDto(config.Id, config.MenuId, config.Name, config.ScriptCode, config.LoginConfigId);
    }

    /// <summary>
    /// 保存自动化配置（upsert）
    /// PUT /api/menus/{menuId}/automation
    /// </summary>
    [HttpPut("api/menus/{menuId}/automation")]
    public async Task<AutomationConfigDto> Upsert(int menuId, [FromBody] SaveAutomationRequest req)
    {
        var config = await db.AutomationConfigs.FirstOrDefaultAsync(a => a.MenuId == menuId);
        if (config == null)
        {
            config = new AutomationConfig { MenuId = menuId };
            db.AutomationConfigs.Add(config);
        }
        config.Name = req.Name;
        config.ScriptCode = req.ScriptCode;
        config.LoginConfigId = req.LoginConfigId;
        config.UpdatedAt = DateTime.Now;
        await db.SaveChangesAsync();
        return new AutomationConfigDto(config.Id, config.MenuId, config.Name, config.ScriptCode, config.LoginConfigId);
    }

    /// <summary>
    /// 执行自动化脚本
    /// POST /api/menus/{menuId}/automation/run
    /// 请求体：{ scriptCode, cdpAddress }
    /// </summary>
    [HttpPost("api/menus/{menuId}/automation/run")]
    public async Task<RunResult> Run(int menuId, [FromBody] RunRequest req)
    {
        // 超时5分钟
        using var cts = new CancellationTokenSource(TimeSpan.FromMinutes(5));

        string cdpAddress = req.CdpAddress;

        // 若指定了 loginConfigId，从全局配置中读取 CDP 地址
        if (req.LoginConfigId.HasValue)
        {
            var schemes = await db.GlobalConfigs
                .FirstOrDefaultAsync(g => g.Category == "login" && g.Key == "schemes");
            if (schemes?.Value != null)
            {
                try
                {
                    var list = JsonSerializer.Deserialize<List<LoginSchemeJson>>(schemes.Value)
                              ?? [];
                    var found = list.FirstOrDefault(s => s.Id == req.LoginConfigId.Value.ToString());
                    if (found != null) cdpAddress = found.CdpAddress;
                }
                catch { }
            }
        }

        if (string.IsNullOrWhiteSpace(cdpAddress))
            return new RunResult(false, "未配置 CDP 地址，请在全局配置中添加登录方案", null);

        var result = await engine.RunAsync(req.ScriptCode, cdpAddress, cts.Token);
        return new RunResult(result.Success, result.Output, result.Error);
    }

    // JSON 反序列化用的内部类
    private record LoginSchemeJson(string Id, string Name, string CdpAddress);
}

/// <summary>自动化配置 DTO</summary>
public record AutomationConfigDto(int Id, int MenuId, string Name, string ScriptCode, int? LoginConfigId);

/// <summary>保存请求体</summary>
public record SaveAutomationRequest(string Name, string ScriptCode, int? LoginConfigId);

/// <summary>执行请求体</summary>
public record RunRequest(string ScriptCode, string CdpAddress, int? LoginConfigId);

/// <summary>执行结果</summary>
public record RunResult(bool Success, string Output, string? Error);
