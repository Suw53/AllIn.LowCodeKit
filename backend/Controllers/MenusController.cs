using AllIn.LowCodeKit.Backend.Data;
using AllIn.LowCodeKit.Backend.Models;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

namespace AllIn.LowCodeKit.Backend.Controllers;

/// <summary>
/// 菜单管理接口
/// </summary>
[ApiController]
[Route("api/menus")]
public class MenusController : ControllerBase
{
    private readonly AppDbContext _db;

    public MenusController(AppDbContext db)
    {
        _db = db;
    }

    /// <summary>
    /// 获取完整菜单树
    /// GET /api/menus
    /// </summary>
    [HttpGet]
    public async Task<IActionResult> GetTree()
    {
        var menus = await _db.Menus
            .OrderBy(m => m.Sort)
            .ToListAsync();

        var roots = menus
            .Where(m => m.ParentId == null)
            .Select(m => BuildNode(m, menus))
            .ToList();

        return Ok(roots);
    }

    /// <summary>
    /// 新增菜单：parentId 为 null 则创建一级菜单，否则创建二级菜单
    /// POST /api/menus
    /// </summary>
    [HttpPost]
    public async Task<IActionResult> Create([FromBody] MenuCreateDto dto)
    {
        if (dto.ParentId.HasValue)
        {
            // 二级菜单：校验父菜单存在
            var parent = await _db.Menus.FindAsync(dto.ParentId.Value);
            if (parent == null) return NotFound(new { message = "父菜单不存在" });
        }

        var maxSort = await _db.Menus
            .Where(m => m.ParentId == dto.ParentId)
            .MaxAsync(m => (int?)m.Sort) ?? 0;

        var menu = new Menu
        {
            ParentId = dto.ParentId,
            Name = dto.Name,
            Icon = dto.Icon,
            Sort = maxSort + 10,
            IsSystem = false
        };
        _db.Menus.Add(menu);
        await _db.SaveChangesAsync();
        return Ok(menu);
    }

    /// <summary>
    /// 修改菜单名称/图标
    /// PUT /api/menus/{id}
    /// </summary>
    [HttpPut("{id:int}")]
    public async Task<IActionResult> Update(int id, [FromBody] MenuCreateDto dto)
    {
        var menu = await _db.Menus.FindAsync(id);
        if (menu == null) return NotFound();
        if (menu.IsSystem) return BadRequest(new { message = "系统内置菜单不可修改" });

        menu.Name = dto.Name;
        menu.Icon = dto.Icon;
        await _db.SaveChangesAsync();
        return Ok(menu);
    }

    /// <summary>
    /// 删除菜单（一级菜单级联删除所有子菜单）
    /// DELETE /api/menus/{id}
    /// </summary>
    [HttpDelete("{id:int}")]
    public async Task<IActionResult> Delete(int id)
    {
        var menu = await _db.Menus.FindAsync(id);
        if (menu == null) return NotFound();
        if (menu.IsSystem) return BadRequest(new { message = "系统内置菜单不可删除" });

        var children = await _db.Menus.Where(m => m.ParentId == id).ToListAsync();
        _db.Menus.RemoveRange(children);
        _db.Menus.Remove(menu);
        await _db.SaveChangesAsync();
        return NoContent();
    }

    /// <summary>
    /// 递归构建菜单树节点
    /// </summary>
    private static object BuildNode(Menu menu, List<Menu> all)
    {
        var children = all
            .Where(m => m.ParentId == menu.Id)
            .OrderBy(m => m.Sort)
            .Select(m => BuildNode(m, all))
            .ToList();

        return new
        {
            menu.Id,
            menu.ParentId,
            menu.Name,
            menu.Icon,
            menu.Sort,
            menu.IsSystem,
            children
        };
    }
}

/// <summary>
/// 菜单创建/修改DTO
/// </summary>
public class MenuCreateDto
{
    /// <summary>父菜单Id，null 表示一级菜单</summary>
    public int? ParentId { get; set; }
    public string Name { get; set; } = string.Empty;
    public string? Icon { get; set; }
}
