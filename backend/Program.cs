using AllIn.LowCodeKit.Backend.Data;
using Microsoft.EntityFrameworkCore;

var builder = WebApplication.CreateBuilder(args);

// 添加控制器
builder.Services.AddControllers();

// 配置SQLite数据库
var dbPath = Path.Combine(
    Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData),
    "AllIn.LowCodeKit",
    "app.db"
);
Directory.CreateDirectory(Path.GetDirectoryName(dbPath)!);
builder.Services.AddDbContext<AppDbContext>(options =>
    options.UseSqlite($"Data Source={dbPath}"));

// 配置CORS，允许Vue前端（Tauri内嵌WebView）访问
builder.Services.AddCors(options =>
{
    options.AddPolicy("TauriPolicy", policy =>
    {
        policy.WithOrigins(
                "http://localhost:1420",  // Tauri开发模式
                "tauri://localhost",       // Tauri生产模式
                "https://tauri.localhost"  // Tauri生产模式备用
            )
            .AllowAnyMethod()
            .AllowAnyHeader();
    });
});

var app = builder.Build();

// 自动执行数据库迁移
using (var scope = app.Services.CreateScope())
{
    var db = scope.ServiceProvider.GetRequiredService<AppDbContext>();
    db.Database.Migrate();
}

app.UseCors("TauriPolicy");
app.MapControllers();

// 监听本地固定端口
app.Run("http://localhost:5000");
