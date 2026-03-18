using System.Collections.Concurrent;
using Microsoft.CodeAnalysis.CSharp.Scripting;
using Microsoft.CodeAnalysis.Scripting;

namespace AllIn.LowCodeKit.Backend.Services;

/// <summary>
/// 导入转换脚本的全局变量宿主
/// 脚本中可直接使用 Value、SourceColumn、TargetField 三个变量
/// </summary>
public class ImportTransformGlobals
{
    /// <summary>当前单元格的原始值</summary>
    public string? Value { get; set; }

    /// <summary>Excel源列名</summary>
    public string SourceColumn { get; set; } = string.Empty;

    /// <summary>目标表单字段名</summary>
    public string TargetField { get; set; } = string.Empty;
}

/// <summary>
/// 轻量级 Roslyn 转换引擎，用于导入数据时对单元格值执行用户定义的 C# 转换脚本。
/// 不依赖 Playwright，仅使用基础 .NET 类型。
/// 注册为 Singleton，内部缓存已编译的脚本避免重复编译。
/// </summary>
public class ImportTransformService
{
    /// <summary>脚本缓存：脚本代码 → 已编译的 ScriptRunner</summary>
    private readonly ConcurrentDictionary<string, ScriptRunner<string?>> _cache = new();

    /// <summary>脚本编译选项（全局共享）</summary>
    private static readonly ScriptOptions _scriptOptions = ScriptOptions.Default
        .AddReferences(
            typeof(object).Assembly,         // System.Private.CoreLib
            typeof(Enumerable).Assembly,     // System.Linq
            typeof(System.Text.RegularExpressions.Regex).Assembly // System.Text.RegularExpressions
        )
        .AddImports(
            "System",
            "System.Collections.Generic",
            "System.Linq",
            "System.Text",
            "System.Text.RegularExpressions"
        );

    /// <summary>
    /// 执行转换脚本，将原始值转换为新值
    /// </summary>
    /// <param name="script">C#脚本代码，最后一行表达式为返回值（如 return Value?.Trim();）</param>
    /// <param name="value">原始单元格值</param>
    /// <param name="sourceColumn">Excel源列名</param>
    /// <param name="targetField">目标字段名</param>
    /// <returns>转换后的值</returns>
    /// <exception cref="TimeoutException">单次转换超时（5秒）</exception>
    public async Task<string?> TransformAsync(
        string script, string? value, string sourceColumn, string targetField)
    {
        var runner = _cache.GetOrAdd(script, code =>
        {
            var compiled = CSharpScript.Create<string?>(
                code, _scriptOptions, typeof(ImportTransformGlobals));
            compiled.Compile();
            return compiled.CreateDelegate();
        });

        var globals = new ImportTransformGlobals
        {
            Value = value,
            SourceColumn = sourceColumn,
            TargetField = targetField
        };

        // 5秒超时保护
        using var cts = new CancellationTokenSource(TimeSpan.FromSeconds(5));
        try
        {
            var result = await Task.Run(() => runner(globals, cts.Token), cts.Token);
            return result;
        }
        catch (OperationCanceledException)
        {
            throw new TimeoutException($"转换脚本执行超时（5秒），源列: {sourceColumn}");
        }
    }
}
