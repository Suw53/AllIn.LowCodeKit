using AllIn.LowCodeKit.Backend.Data;
using AllIn.LowCodeKit.Backend.Models;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

namespace AllIn.LowCodeKit.Backend.Controllers;

/// <summary>
/// 菜单管理接口
/// </summary>
[ApiController]
[Route("api/[controller]")]
public class MenusController : ControllerBase
{
    private readonly AppDbContext _db;

    public MenusController(AppDbContext db)
    {
        _db = db;
    }

    /// <summary>
    /// 获取完整菜单树
    /// </summary>
    [HttpGet("tree")]
    public async Task<IActionResult> GetTree()
    {
        var menus = await _db.Menus
            .OrderBy(m => m.Sort)
            .ToListAsync();

        // 构建树形结构
        var roots = menus
            .Where(m => m.ParentId == null)
            .Select(m => BuildNode(m, menus))
            .ToList();

        return Ok(roots);
    }

    /// <summary>
    /// 新增一级菜单
    /// </summary>
    [HttpPost("level1")]
    public async Task<IActionResult> AddLevel1([FromBody] MenuCreateDto dto)
    {
        var maxSort = await _db.Menus
            .Where(m => m.ParentId == null)
            .MaxAsync(m => (int?)m.Sort) ?? 0;

        var menu = new Menu
        {
            ParentId = null,
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
    /// 新增二级菜单
    /// </summary>
    [HttpPost("level2")]
    public async Task<IActionResult> AddLevel2([FromBody] MenuCreateDto dto)
    {
        if (dto.ParentId == null)
            return BadRequest("必须指定父级菜单Id");

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
    /// 修改菜单名称
    /// </summary>
    [HttpPut("{id}")]
    public async Task<IActionResult> Update(int id, [FromBody] MenuCreateDto dto)
    {
        var menu = await _db.Menus.FindAsync(id);
        if (menu == null) return NotFound();
        if (menu.IsSystem) return BadRequest("系统内置菜单不可修改");

        menu.Name = dto.Name;
        menu.Icon = dto.Icon;
        await _db.SaveChangesAsync();
        return Ok(menu);
    }

    /// <summary>
    /// 删除菜单（一级菜单会级联删除所有子菜单）
    /// </summary>
    [HttpDelete("{id}")]
    public async Task<IActionResult> Delete(int id)
    {
        var menu = await _db.Menus.FindAsync(id);
        if (menu == null) return NotFound();
        if (menu.IsSystem) return BadRequest("系统内置菜单不可删除");

        // 递归删除子菜单
        var children = await _db.Menus.Where(m => m.ParentId == id).ToListAsync();
        _db.Menus.RemoveRange(children);
        _db.Menus.Remove(menu);
        await _db.SaveChangesAsync();
        return Ok();
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
    public int? ParentId { get; set; }
    public string Name { get; set; } = string.Empty;
    public string? Icon { get; set; }
}
