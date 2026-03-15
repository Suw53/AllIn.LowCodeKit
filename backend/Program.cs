using AllIn.LowCodeKit.Backend.Data;
using AllIn.LowCodeKit.Backend.Services;
using Microsoft.EntityFrameworkCore;
using System.Text.Json.Serialization;

var builder = WebApplication.CreateBuilder(args);

// 添加控制器，配置 JSON 序列化：驼峰命名 + 忽略循环引用
builder.Services.AddControllers()
    .AddJsonOptions(options =>
    {
        options.JsonSerializerOptions.PropertyNamingPolicy = System.Text.Json.JsonNamingPolicy.CamelCase;
        options.JsonSerializerOptions.ReferenceHandler = ReferenceHandler.IgnoreCycles;
        options.JsonSerializerOptions.DefaultIgnoreCondition = JsonIgnoreCondition.WhenWritingNull;
    });

// 注册动态数据服务
builder.Services.AddSingleton<DynamicDataService>();

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
