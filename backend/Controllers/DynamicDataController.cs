using AllIn.LowCodeKit.Backend.Data;
using AllIn.LowCodeKit.Backend.Helpers;
using AllIn.LowCodeKit.Backend.Services;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using System.Text.Json;

namespace AllIn.LowCodeKit.Backend.Controllers;

/// <summary>
/// 动态数据管理接口：对每个功能模块的 DynamicData_{menuId} 表进行增删改查、导入、导出
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
    /// 分页查询模块数据，支持关键词搜索、字段级筛选、批次过滤
    /// GET /api/menus/{menuId}/data?batchId=latest
    /// </summary>
    [HttpGet("api/menus/{menuId:int}/data")]
    public async Task<IActionResult> Query(
        int menuId,
        [FromQuery] int page = 1,
        [FromQuery] int pageSize = 100,
        [FromQuery] string? keyword = null,
        [FromQuery] string? filters = null,
        [FromQuery] string? batchId = null)
    {
        var template = await _db.FormTemplates
            .Include(t => t.Fields)
            .FirstOrDefaultAsync(t => t.MenuId == menuId);
        if (template == null)
            return NotFound(new { message = "该菜单尚未配置表单模板" });

        var fields = template.Fields.OrderBy(f => f.ColumnOrder).ToList();
        await _dataService.EnsureTableAsync(menuId, fields);

        var filterList = DeserializeFilters(filters);
        var (total, items) = await _dataService.QueryAsync(
            menuId, page, pageSize, keyword, filterList, fields, batchId);

        return Ok(new { total, items });
    }

    /// <summary>
    /// 获取批次号列表（倒序），用于批次选择器
    /// GET /api/menus/{menuId}/data/batches
    /// </summary>
    [HttpGet("api/menus/{menuId:int}/data/batches")]
    public async Task<IActionResult> GetBatches(int menuId)
    {
        var batches = await _dataService.GetBatchIdsAsync(menuId);
        return Ok(batches);
    }

    /// <summary>
    /// 下载 Excel 导入模板
    /// GET /api/menus/{menuId}/data/template
    /// </summary>
    [HttpGet("api/menus/{menuId:int}/data/template")]
    public async Task<IActionResult> DownloadTemplate(int menuId)
    {
        var template = await _db.FormTemplates
            .Include(t => t.Fields)
            .FirstOrDefaultAsync(t => t.MenuId == menuId);
        if (template == null)
            return NotFound(new { message = "该菜单尚未配置表单模板" });

        var bytes = ExcelHelper.GenerateTemplate(template.Fields);
        var fileName = Uri.EscapeDataString($"{template.Name}-导入模板.xlsx");
        Response.Headers["Content-Disposition"] = $"attachment; filename*=UTF-8''{fileName}";
        return File(bytes, "application/vnd.openxmlformats-officedocument.spreadsheetml.sheet");
    }

    /// <summary>
    /// 导出当前筛选结果为 Excel
    /// GET /api/menus/{menuId}/data/export
    /// </summary>
    [HttpGet("api/menus/{menuId:int}/data/export")]
    public async Task<IActionResult> ExportExcel(
        int menuId,
        [FromQuery] string? keyword = null,
        [FromQuery] string? filters = null,
        [FromQuery] string? columns = null,
        [FromQuery] string? batchId = null)
    {
        var template = await _db.FormTemplates
            .Include(t => t.Fields)
            .FirstOrDefaultAsync(t => t.MenuId == menuId);
        if (template == null)
            return NotFound(new { message = "该菜单尚未配置表单模板" });

        var fields = template.Fields.OrderBy(f => f.ColumnOrder).ToList();
        var filterList = DeserializeFilters(filters);
        var rows = await _dataService.QueryAllAsync(menuId, keyword, filterList, fields, batchId);

        List<string>? selectedColumns = null;
        if (!string.IsNullOrWhiteSpace(columns))
        {
            try { selectedColumns = JsonSerializer.Deserialize<List<string>>(columns); }
            catch { }
        }

        var bytes = ExcelHelper.ExportData(fields, rows, selectedColumns);
        var fileName = Uri.EscapeDataString($"{template.Name}-{DateTime.Now:yyyyMMdd}.xlsx");
        Response.Headers["Content-Disposition"] = $"attachment; filename*=UTF-8''{fileName}";
        return File(bytes, "application/vnd.openxmlformats-officedocument.spreadsheetml.sheet");
    }

    /// <summary>
    /// 预览导入数据（解析+校验，不保存），返回每行的状态和错误信息
    /// POST /api/menus/{menuId}/data/import/preview
    /// </summary>
    [HttpPost("api/menus/{menuId:int}/data/import/preview")]
    public async Task<IActionResult> PreviewImport(int menuId, IFormFile file)
    {
        if (file == null || file.Length == 0)
            return BadRequest(new { message = "请选择 Excel 文件" });

        var template = await _db.FormTemplates
            .Include(t => t.Fields)
            .FirstOrDefaultAsync(t => t.MenuId == menuId);
        if (template == null)
            return NotFound(new { message = "该菜单尚未配置表单模板" });

        var fields = template.Fields.OrderBy(f => f.ColumnOrder).ToList();

        List<Dictionary<string, string?>> dataRows;
        try
        {
            await using var stream = file.OpenReadStream();
            dataRows = ExcelHelper.ParseImportData(stream, fields);
        }
        catch (Exception ex)
        {
            return BadRequest(new { message = $"文件解析失败：{ex.Message}" });
        }

        if (dataRows.Count == 0)
            return BadRequest(new { message = "文件中未读取到有效数据行" });

        var rows = new List<object>();
        int successCount = 0;
        int errorCount = 0;

        for (int i = 0; i < dataRows.Count; i++)
        {
            var row = dataRows[i];
            var errors = new List<string>();

            foreach (var f in fields.Where(f => f.IsRequired))
            {
                if (!row.TryGetValue(f.FieldName, out var val) || string.IsNullOrWhiteSpace(val))
                    errors.Add($"【{f.Label}】不能为空");
            }

            var status = errors.Count == 0 ? "ok" : "error";
            if (status == "ok") successCount++; else errorCount++;

            rows.Add(new
            {
                rowIndex = i + 2,   // Excel 行号（第1行是标题，数据从第2行开始）
                data = row,
                status,
                errors
            });
        }

        return Ok(new { rows, successCount, errorCount });
    }

    /// <summary>
    /// 确认导入数据（保存有效行到数据库，绑定批次号）
    /// POST /api/menus/{menuId}/data/import/confirm
    /// </summary>
    [HttpPost("api/menus/{menuId:int}/data/import/confirm")]
    public async Task<IActionResult> ConfirmImport(int menuId, [FromBody] ConfirmImportRequest req)
    {
        var template = await _db.FormTemplates
            .Include(t => t.Fields)
            .FirstOrDefaultAsync(t => t.MenuId == menuId);
        if (template == null)
            return NotFound(new { message = "该菜单尚未配置表单模板" });

        var fields = template.Fields.OrderBy(f => f.ColumnOrder).ToList();
        await _dataService.EnsureTableAsync(menuId, fields);

        int imported = 0;
        foreach (var row in req.Rows)
        {
            await _dataService.InsertAsync(menuId, row!, req.BatchId);
            imported++;
        }

        return Ok(new { imported, batchId = req.BatchId });
    }

    /// <summary>
    /// 新增一条数据记录（手动添加，_BatchId = null）
    /// POST /api/menus/{menuId}/data
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

    // ────────── 私有辅助 ──────────

    private static List<FilterCondition>? DeserializeFilters(string? filters)
    {
        if (string.IsNullOrWhiteSpace(filters)) return null;
        try
        {
            return JsonSerializer.Deserialize<List<FilterCondition>>(filters,
                new JsonSerializerOptions { PropertyNameCaseInsensitive = true });
        }
        catch { return null; }
    }
}

/// <summary>确认导入请求体</summary>
public record ConfirmImportRequest(
    string BatchId,
    List<Dictionary<string, string?>> Rows
);
