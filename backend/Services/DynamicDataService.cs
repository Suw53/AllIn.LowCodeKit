using AllIn.LowCodeKit.Backend.Models;
using Microsoft.Data.Sqlite;

namespace AllIn.LowCodeKit.Backend.Services;

/// <summary>
/// 动态数据表服务：按菜单Id动态创建 DynamicData_{menuId} 表，并提供原生SQL的增删改查
/// 系统列：Id, CreatedAt, UpdatedAt, _BatchId
/// </summary>
public class DynamicDataService
{
    private readonly string _connectionString;

    public DynamicDataService(IConfiguration configuration)
    {
        var dbPath = Path.Combine(
            Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData),
            "AllIn.LowCodeKit",
            "app.db"
        );
        _connectionString = $"Data Source={dbPath}";
    }

    /// <summary>
    /// 确保动态数据表存在，若字段新增则同步 ALTER TABLE 加列。
    /// 系统列：Id, CreatedAt, UpdatedAt, _BatchId（批次号，导入时赋值，手动添加为null）
    /// </summary>
    public async Task EnsureTableAsync(int menuId, IEnumerable<FormField> fields)
    {
        var tableName = TableName(menuId);
        var fieldList = fields.ToList();

        await using var conn = new SqliteConnection(_connectionString);
        await conn.OpenAsync();

        var columnDefs = fieldList
            .Select(f => $"    \"{f.FieldName}\" TEXT")
            .ToList();

        var createSql = $"""
            CREATE TABLE IF NOT EXISTS "{tableName}" (
                "Id" INTEGER PRIMARY KEY AUTOINCREMENT,
                "CreatedAt" TEXT NOT NULL DEFAULT (datetime('now','localtime')),
                "UpdatedAt" TEXT NOT NULL DEFAULT (datetime('now','localtime')),
                "_BatchId" TEXT NULL{(columnDefs.Count > 0 ? "," : "")}
                {string.Join(",\n                ", columnDefs)}
            )
            """;

        await using (var cmd = conn.CreateCommand())
        {
            cmd.CommandText = createSql;
            await cmd.ExecuteNonQueryAsync();
        }

        // 同步新增列（字段扩展 + 旧表缺少 _BatchId 的情况）
        var existing = await GetExistingColumnsAsync(conn, tableName);

        // 确保 _BatchId 列存在（升级旧表）
        if (!existing.Contains("_BatchId"))
        {
            await using var cmd = conn.CreateCommand();
            cmd.CommandText = $"ALTER TABLE \"{tableName}\" ADD COLUMN \"_BatchId\" TEXT NULL";
            await cmd.ExecuteNonQueryAsync();
        }

        foreach (var f in fieldList)
        {
            if (!existing.Contains(f.FieldName))
            {
                await using var cmd = conn.CreateCommand();
                cmd.CommandText = $"ALTER TABLE \"{tableName}\" ADD COLUMN \"{f.FieldName}\" TEXT";
                await cmd.ExecuteNonQueryAsync();
            }
        }
    }

    /// <summary>
    /// 分页查询数据列表，支持关键词搜索、字段级筛选、批次过滤
    /// batchId = null → 全部数据；batchId = "latest" → 仅最新批次；其他 → 精确匹配
    /// </summary>
    public async Task<(int Total, List<Dictionary<string, object?>> Items)> QueryAsync(
        int menuId,
        int page,
        int pageSize,
        string? keyword,
        List<FilterCondition>? filters,
        IEnumerable<FormField> fields,
        string? batchId = null)
    {
        await using var conn = new SqliteConnection(_connectionString);
        await conn.OpenAsync();

        var resolvedBatchId = await ResolveBatchIdAsync(conn, menuId, batchId);

        var tableName = TableName(menuId);
        var fieldNames = fields.Select(f => f.FieldName).ToList();
        var (whereSql, parameters) = BuildWhere(keyword, filters, fieldNames, resolvedBatchId);

        int total;
        await using (var cmd = conn.CreateCommand())
        {
            cmd.CommandText = $"SELECT COUNT(*) FROM \"{tableName}\"{whereSql}";
            AddParameters(cmd, parameters);
            total = Convert.ToInt32(await cmd.ExecuteScalarAsync() ?? 0);
        }

        var items = new List<Dictionary<string, object?>>();
        var offset = (page - 1) * pageSize;
        await using (var cmd = conn.CreateCommand())
        {
            cmd.CommandText = $"SELECT * FROM \"{tableName}\"{whereSql} ORDER BY Id DESC LIMIT {pageSize} OFFSET {offset}";
            AddParameters(cmd, parameters);
            await using var reader = await cmd.ExecuteReaderAsync();
            while (await reader.ReadAsync())
            {
                var row = new Dictionary<string, object?>();
                for (int i = 0; i < reader.FieldCount; i++)
                    row[reader.GetName(i)] = reader.IsDBNull(i) ? null : reader.GetValue(i);
                items.Add(row);
            }
        }

        return (total, items);
    }

    /// <summary>查询全部匹配数据（不分页，用于导出），支持批次过滤</summary>
    public async Task<List<Dictionary<string, object?>>> QueryAllAsync(
        int menuId,
        string? keyword,
        List<FilterCondition>? filters,
        IEnumerable<FormField> fields,
        string? batchId = null)
    {
        await using var conn = new SqliteConnection(_connectionString);
        await conn.OpenAsync();

        var resolvedBatchId = await ResolveBatchIdAsync(conn, menuId, batchId);

        var tableName = TableName(menuId);
        var fieldNames = fields.Select(f => f.FieldName).ToList();
        var (whereSql, parameters) = BuildWhere(keyword, filters, fieldNames, resolvedBatchId);

        var items = new List<Dictionary<string, object?>>();
        await using var cmd = conn.CreateCommand();
        cmd.CommandText = $"SELECT * FROM \"{tableName}\"{whereSql} ORDER BY Id DESC";
        AddParameters(cmd, parameters);
        await using var reader = await cmd.ExecuteReaderAsync();
        while (await reader.ReadAsync())
        {
            var row = new Dictionary<string, object?>();
            for (int i = 0; i < reader.FieldCount; i++)
                row[reader.GetName(i)] = reader.IsDBNull(i) ? null : reader.GetValue(i);
            items.Add(row);
        }

        return items;
    }

    /// <summary>获取所有批次号列表（按时间倒序），用于批次选择器</summary>
    public async Task<List<string>> GetBatchIdsAsync(int menuId)
    {
        var tableName = TableName(menuId);
        var result = new List<string>();

        await using var conn = new SqliteConnection(_connectionString);
        await conn.OpenAsync();

        // 表不存在时直接返回空列表
        var tableExists = await TableExistsAsync(conn, tableName);
        if (!tableExists) return result;

        await using var cmd = conn.CreateCommand();
        cmd.CommandText = $"SELECT DISTINCT \"_BatchId\" FROM \"{tableName}\" WHERE \"_BatchId\" IS NOT NULL ORDER BY \"_BatchId\" DESC";
        await using var reader = await cmd.ExecuteReaderAsync();
        while (await reader.ReadAsync())
            result.Add(reader.GetString(0));

        return result;
    }

    /// <summary>插入一条记录，支持指定批次号（null表示手动添加）</summary>
    public async Task<long> InsertAsync(int menuId, Dictionary<string, string?> data, string? batchId = null)
    {
        var tableName = TableName(menuId);
        var safeData = data.Where(kv => !IsSystemColumn(kv.Key)).ToList();

        var cols = safeData.Select(kv => $"\"{kv.Key}\"").ToList();
        var paramNames = safeData.Select((_, i) => $"@v{i}").ToList();

        // 追加 _BatchId 和 UpdatedAt
        cols.Add("\"_BatchId\"");
        cols.Add("\"UpdatedAt\"");
        paramNames.Add("@batchId");
        paramNames.Add("datetime('now','localtime')");

        var sql = $"""
            INSERT INTO "{tableName}" ({string.Join(", ", cols)})
            VALUES ({string.Join(", ", paramNames)});
            SELECT last_insert_rowid();
            """;

        await using var conn = new SqliteConnection(_connectionString);
        await conn.OpenAsync();
        await using var cmd = conn.CreateCommand();
        cmd.CommandText = sql;
        for (int i = 0; i < safeData.Count; i++)
        {
            var p = cmd.CreateParameter();
            p.ParameterName = $"@v{i}";
            p.Value = (object?)safeData[i].Value ?? DBNull.Value;
            cmd.Parameters.Add(p);
        }
        var bp = cmd.CreateParameter();
        bp.ParameterName = "@batchId";
        bp.Value = (object?)batchId ?? DBNull.Value;
        cmd.Parameters.Add(bp);

        return Convert.ToInt64(await cmd.ExecuteScalarAsync() ?? 0);
    }

    /// <summary>更新一条记录</summary>
    public async Task<bool> UpdateAsync(int menuId, long rowId, Dictionary<string, string?> data)
    {
        var tableName = TableName(menuId);
        var safeData = data.Where(kv => !IsSystemColumn(kv.Key)).ToList();
        if (safeData.Count == 0) return true;

        var setClauses = safeData.Select((kv, i) => $"\"{kv.Key}\" = @v{i}").ToList();
        setClauses.Add("\"UpdatedAt\" = datetime('now','localtime')");

        var sql = $"UPDATE \"{tableName}\" SET {string.Join(", ", setClauses)} WHERE \"Id\" = @id";

        await using var conn = new SqliteConnection(_connectionString);
        await conn.OpenAsync();
        await using var cmd = conn.CreateCommand();
        cmd.CommandText = sql;
        for (int i = 0; i < safeData.Count; i++)
        {
            var p = cmd.CreateParameter();
            p.ParameterName = $"@v{i}";
            p.Value = (object?)safeData[i].Value ?? DBNull.Value;
            cmd.Parameters.Add(p);
        }
        var idParam = cmd.CreateParameter();
        idParam.ParameterName = "@id";
        idParam.Value = rowId;
        cmd.Parameters.Add(idParam);

        return await cmd.ExecuteNonQueryAsync() > 0;
    }

    /// <summary>删除一条记录</summary>
    public async Task<bool> DeleteAsync(int menuId, long rowId)
    {
        var tableName = TableName(menuId);

        await using var conn = new SqliteConnection(_connectionString);
        await conn.OpenAsync();
        await using var cmd = conn.CreateCommand();
        cmd.CommandText = $"DELETE FROM \"{tableName}\" WHERE \"Id\" = @id";
        var p = cmd.CreateParameter();
        p.ParameterName = "@id";
        p.Value = rowId;
        cmd.Parameters.Add(p);

        return await cmd.ExecuteNonQueryAsync() > 0;
    }

    // ────────── 私有辅助 ──────────

    private static string TableName(int menuId) => $"DynamicData_{menuId}";

    private static bool IsSystemColumn(string name) =>
        name.Equals("Id", StringComparison.OrdinalIgnoreCase) ||
        name.Equals("CreatedAt", StringComparison.OrdinalIgnoreCase) ||
        name.Equals("UpdatedAt", StringComparison.OrdinalIgnoreCase) ||
        name.Equals("_BatchId", StringComparison.OrdinalIgnoreCase);

    /// <summary>
    /// 解析批次参数：null→null（无过滤），"latest"→最新批次ID（若无则null），其他→原值
    /// </summary>
    private static async Task<string?> ResolveBatchIdAsync(SqliteConnection conn, int menuId, string? batchId)
    {
        if (string.IsNullOrEmpty(batchId) || batchId != "latest") return batchId;

        var tableName = TableName(menuId);
        if (!await TableExistsAsync(conn, tableName)) return null;

        await using var cmd = conn.CreateCommand();
        cmd.CommandText = $"SELECT MAX(\"_BatchId\") FROM \"{tableName}\" WHERE \"_BatchId\" IS NOT NULL";
        var result = await cmd.ExecuteScalarAsync();
        return result == DBNull.Value || result == null ? null : result.ToString();
    }

    private static async Task<bool> TableExistsAsync(SqliteConnection conn, string tableName)
    {
        await using var cmd = conn.CreateCommand();
        cmd.CommandText = $"SELECT COUNT(*) FROM sqlite_master WHERE type='table' AND name=@name";
        var p = cmd.CreateParameter();
        p.ParameterName = "@name";
        p.Value = tableName;
        cmd.Parameters.Add(p);
        return Convert.ToInt32(await cmd.ExecuteScalarAsync()) > 0;
    }

    private static async Task<HashSet<string>> GetExistingColumnsAsync(SqliteConnection conn, string tableName)
    {
        var set = new HashSet<string>(StringComparer.OrdinalIgnoreCase);
        await using var cmd = conn.CreateCommand();
        cmd.CommandText = $"PRAGMA table_info(\"{tableName}\")";
        await using var reader = await cmd.ExecuteReaderAsync();
        while (await reader.ReadAsync())
            set.Add(reader.GetString(1));
        return set;
    }

    private static (string Sql, Dictionary<string, object> Parameters) BuildWhere(
        string? keyword,
        List<FilterCondition>? filters,
        List<string> fieldNames,
        string? batchId)
    {
        var conditions = new List<string>();
        var parameters = new Dictionary<string, object>();
        int idx = 0;

        if (!string.IsNullOrWhiteSpace(keyword) && fieldNames.Count > 0)
        {
            var clauses = fieldNames.Select(f => $"\"{f}\" LIKE @kw").ToList();
            conditions.Add($"({string.Join(" OR ", clauses)})");
            parameters["@kw"] = $"%{keyword}%";
        }

        if (filters?.Count > 0)
        {
            foreach (var f in filters)
            {
                if (string.IsNullOrWhiteSpace(f.Field) || string.IsNullOrWhiteSpace(f.Value))
                    continue;
                var pName = $"@f{idx++}";
                if (f.Op == "contains")
                {
                    conditions.Add($"\"{f.Field}\" LIKE {pName}");
                    parameters[pName] = $"%{f.Value}%";
                }
                else
                {
                    conditions.Add($"\"{f.Field}\" = {pName}");
                    parameters[pName] = f.Value;
                }
            }
        }

        // 批次过滤（已由 ResolveBatchIdAsync 解析 "latest"）
        if (!string.IsNullOrEmpty(batchId))
        {
            conditions.Add("\"_BatchId\" = @batchId");
            parameters["@batchId"] = batchId;
        }

        var sql = conditions.Count > 0 ? " WHERE " + string.Join(" AND ", conditions) : "";
        return (sql, parameters);
    }

    private static void AddParameters(SqliteCommand cmd, Dictionary<string, object> parameters)
    {
        foreach (var kv in parameters)
        {
            var p = cmd.CreateParameter();
            p.ParameterName = kv.Key;
            p.Value = kv.Value;
            cmd.Parameters.Add(p);
        }
    }
}

/// <summary>字段级筛选条件</summary>
public class FilterCondition
{
    public string Field { get; set; } = string.Empty;
    public string Op { get; set; } = "contains";
    public string Value { get; set; } = string.Empty;
}
