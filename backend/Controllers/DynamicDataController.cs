using AllIn.LowCodeKit.Backend.Data;
using AllIn.LowCodeKit.Backend.Services;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using System.Text.Json;

namespace AllIn.LowCodeKit.Backend.Controllers;

/// <summary>
/// 动态数据管理接口：对每个功能模块的 DynamicData_{menuId} 表进行增删改查
/// </summary>
[ApiController]
public class DynamicDataController : ControllerBase
{
    private readonly AppDbContext _db;
    private readonly DynamicDataService _dataService;

    public DynamicDataController(AppDbContext db, DynamicDataService dataService)
    {
        _db = db;
        _dataService = dataService;
    }

    /// <summary>
    /// 分页查询模块数据，支持关键词搜索和字段级筛选
    /// GET /api/menus/{menuId}/data?page=1&amp;pageSize=100&amp;keyword=xxx&amp;filters=[{...}]
    /// </summary>
    [HttpGet("api/menus/{menuId:int}/data")]
    public async Task<IActionResult> Query(
        int menuId,
        [FromQuery] int page = 1,
        [FromQuery] int pageSize = 100,
        [FromQuery] string? keyword = null,
        [FromQuery] string? filters = null)
    {
        var template = await _db.FormTemplates
            .Include(t => t.Fields)
            .FirstOrDefaultAsync(t => t.MenuId == menuId);
        if (template == null)
            return NotFound(new { message = "该菜单尚未配置表单模板" });

        var fields = template.Fields.OrderBy(f => f.ColumnOrder).ToList();
        await _dataService.EnsureTableAsync(menuId, fields);

        List<FilterCondition>? filterList = null;
        if (!string.IsNullOrWhiteSpace(filters))
        {
            try
            {
                filterList = JsonSerializer.Deserialize<List<FilterCondition>>(filters,
                    new JsonSerializerOptions { PropertyNameCaseInsensitive = true });
            }
            catch { /* 格式错误忽略，当无筛选处理 */ }
        }

        var (total, items) = await _dataService.QueryAsync(
            menuId, page, pageSize, keyword, filterList, fields);

        return Ok(new { total, items });
    }

    /// <summary>
    /// 新增一条数据记录
    /// POST /api/menus/{menuId}/data
    /// Body：{ "FieldName1": "value1", "FieldName2": "value2" }
    /// </summary>
    [HttpPost("api/menus/{menuId:int}/data")]
    public async Task<IActionResult> Create(int menuId, [FromBody] Dictionary<string, string?> data)
    {
        var template = await _db.FormTemplates
            .Include(t => t.Fields)
            .FirstOrDefaultAsync(t => t.MenuId == menuId);
        if (template == null)
            return NotFound(new { message = "该菜单尚未配置表单模板" });

        await _dataService.EnsureTableAsync(menuId, template.Fields);
        var id = await _dataService.InsertAsync(menuId, data);
        return Ok(new { id });
    }

    /// <summary>
    /// 更新一条数据记录
    /// PUT /api/menus/{menuId}/data/{rowId}
    /// </summary>
    [HttpPut("api/menus/{menuId:int}/data/{rowId:long}")]
    public async Task<IActionResult> Update(
        int menuId, long rowId, [FromBody] Dictionary<string, string?> data)
    {
        var success = await _dataService.UpdateAsync(menuId, rowId, data);
        if (!success) return NotFound(new { message = "记录不存在" });
        return Ok();
    }

    /// <summary>
    /// 删除一条数据记录
    /// DELETE /api/menus/{menuId}/data/{rowId}
    /// </summary>
    [HttpDelete("api/menus/{menuId:int}/data/{rowId:long}")]
    public async Task<IActionResult> Delete(int menuId, long rowId)
    {
        var success = await _dataService.DeleteAsync(menuId, rowId);
        if (!success) return NotFound(new { message = "记录不存在" });
        return NoContent();
    }
}
