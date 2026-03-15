using System.Text;
using Microsoft.CodeAnalysis.CSharp.Scripting;
using Microsoft.CodeAnalysis.Scripting;
using Microsoft.Playwright;

namespace AllIn.LowCodeKit.Backend.Services;

/// <summary>
/// 脚本执行的全局变量宿主，脚本中可直接使用 Page 和 Log()
/// </summary>
public class AutomationGlobals
{
    /// <summary>Playwright IPage 实例，已通过 CDP 连接到目标浏览器</summary>
    public IPage Page { get; set; } = null!;

    /// <summary>在日志中输出一行消息</summary>
    public Action<string> Log { get; set; } = _ => { };
}

/// <summary>脚本执行结果</summary>
public record ScriptRunResult(bool Success, string Output, string? Error = null);

/// <summary>
/// Roslyn C# 脚本执行引擎，通过 CDP 连接已有浏览器后注入 Playwright Page 执行用户脚本
/// </summary>
public class RoslynScriptEngine
{
    /// <summary>
    /// 执行 C# 脚本
    /// </summary>
    /// <param name="scriptCode">用户编写的 C# 脚本</param>
    /// <param name="cdpAddress">CDP 调试地址，如 http://127.0.0.1:9222</param>
    /// <param name="cancellationToken">取消令牌（超时控制）</param>
    public async Task<ScriptRunResult> RunAsync(
        string scriptCode,
        string cdpAddress,
        CancellationToken cancellationToken = default)
    {
        var output = new StringBuilder();
        output.AppendLine($"[{DateTime.Now:HH:mm:ss}] 开始执行...");

        IPlaywright? playwright = null;
        IBrowser? browser = null;

        try
        {
            // 连接到 CDP 浏览器
            output.AppendLine($"[{DateTime.Now:HH:mm:ss}] 连接浏览器 {cdpAddress}");
            playwright = await Playwright.CreateAsync();
            browser = await playwright.Chromium.ConnectOverCDPAsync(cdpAddress);

            // 获取当前已打开的页面（优先使用第一个，没有则新建）
            IPage? page = null;
            if (browser.Contexts.Count > 0 && browser.Contexts[0].Pages.Count > 0)
                page = browser.Contexts[0].Pages[0];
            else
            {
                output.AppendLine($"[{DateTime.Now:HH:mm:ss}] 未检测到已打开页面，创建新标签页");
                var ctx = browser.Contexts.Count > 0
                    ? browser.Contexts[0]
                    : await browser.NewContextAsync();
                page = await ctx.NewPageAsync();
            }

            output.AppendLine($"[{DateTime.Now:HH:mm:ss}] 浏览器连接成功，开始执行脚本");

            // 构建 Roslyn 脚本执行选项
            var scriptOptions = ScriptOptions.Default
                .AddReferences(
                    typeof(IPage).Assembly,          // Microsoft.Playwright
                    typeof(object).Assembly,         // System.Private.CoreLib
                    typeof(Console).Assembly,        // System.Console
                    typeof(Task).Assembly            // System.Threading.Tasks
                )
                .AddImports(
                    "Microsoft.Playwright",
                    "System",
                    "System.Collections.Generic",
                    "System.Linq",
                    "System.Threading.Tasks",
                    "System.Text",
                    "System.Text.RegularExpressions"
                );

            var globals = new AutomationGlobals
            {
                Page = page,
                Log = msg => output.AppendLine($"[{DateTime.Now:HH:mm:ss}] {msg}")
            };

            // 执行用户脚本
            await CSharpScript.RunAsync(scriptCode, scriptOptions, globals, cancellationToken: cancellationToken);

            output.AppendLine($"[{DateTime.Now:HH:mm:ss}] ✓ 脚本执行完成");
            return new ScriptRunResult(true, output.ToString());
        }
        catch (CompilationErrorException compileEx)
        {
            var errors = string.Join("\n", compileEx.Diagnostics.Select(d => d.ToString()));
            output.AppendLine($"[{DateTime.Now:HH:mm:ss}] ✗ 编译错误：");
            output.AppendLine(errors);
            return new ScriptRunResult(false, output.ToString(), $"编译错误：{errors}");
        }
        catch (OperationCanceledException)
        {
            output.AppendLine($"[{DateTime.Now:HH:mm:ss}] ✗ 执行超时或被取消");
            return new ScriptRunResult(false, output.ToString(), "执行超时或被取消");
        }
        catch (Exception ex)
        {
            output.AppendLine($"[{DateTime.Now:HH:mm:ss}] ✗ 运行时异常：{ex.Message}");
            if (ex.InnerException != null)
                output.AppendLine($"  原因：{ex.InnerException.Message}");
            return new ScriptRunResult(false, output.ToString(), ex.Message);
        }
        finally
        {
            // 只断开连接，不关闭用户的浏览器
            try { if (browser != null) await browser.CloseAsync(); } catch { }
            playwright?.Dispose();
        }
    }
}
