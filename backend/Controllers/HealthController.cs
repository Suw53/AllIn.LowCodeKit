using Microsoft.AspNetCore.Mvc;

namespace AllIn.LowCodeKit.Backend.Controllers;

/// <summary>
/// 健康检查控制器，供前端确认后端服务是否正常运行
/// </summary>
[ApiController]
[Route("api/[controller]")]
public class HealthController : ControllerBase
{
    /// <summary>
    /// 检查后端服务状态
    /// </summary>
    /// <returns>服务状态信息</returns>
    [HttpGet]
    public IActionResult Get()
    {
        return Ok(new
        {
            status = "ok",
            version = "0.1.0",
            timestamp = DateTime.UtcNow.ToString("o")
        });
    }
}
