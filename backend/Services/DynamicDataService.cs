using AllIn.LowCodeKit.Backend.Models;
using Microsoft.Data.Sqlite;

namespace AllIn.LowCodeKit.Backend.Services;

/// <summary>
/// 动态数据表服务：按菜单Id动态创建 DynamicData_{menuId} 表，并提供原生SQL的增删改查
/// </summary>
public class DynamicDataService
{
    private readonly string _connectionString;

    public DynamicDataService(IConfiguration configuration)
    {
        // 从连接字符串中取出数据库路径
        var dbPath = Path.Combine(
            Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData),
            "AllIn.LowCodeKit",
            "app.db"
        );
        _connectionString = $"Data Source={dbPath}";
    }

    /// <summary>
    /// 确保动态数据表存在，若字段新增则同步 ALTER TABLE 加列
    /// </summary>
    public async Task EnsureTableAsync(int menuId, IEnumerable<FormField> fields)
    {
        var tableName = TableName(menuId);
        var fieldList = fields.ToList();

        await using var conn = new SqliteConnection(_connectionString);
        await conn.OpenAsync();

        // 建表（不存在则建）
        var columnDefs = fieldList
            .Select(f => $"    \"{f.FieldName}\" TEXT")
            .ToList();
        var createSql = $"""
            CREATE TABLE IF NOT EXISTS "{tableName}" (
                "Id" INTEGER PRIMARY KEY AUTOINCREMENT,
                "CreatedAt" TEXT NOT NULL DEFAULT (datetime('now','localtime')),
                "UpdatedAt" TEXT NOT NULL DEFAULT (datetime('now','localtime')){(columnDefs.Count > 0 ? "," : "")}
                {string.Join(",\n                ", columnDefs)}
            )
            """;
        await using (var cmd = conn.CreateCommand())
        {
            cmd.CommandText = createSql;
            await cmd.ExecuteNonQueryAsync();
        }

        // 同步新增列（字段扩展场景）
        var existing = await GetExistingColumnsAsync(conn, tableName);
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
    /// 分页查询数据列表，支持关键词搜索和字段级筛选
    /// </summary>
    public async Task<(int Total, List<Dictionary<string, object?>> Items)> QueryAsync(
        int menuId,
        int page,
        int pageSize,
        string? keyword,
        List<FilterCondition>? filters,
        IEnumerable<FormField> fields)
    {
        var tableName = TableName(menuId);
        var fieldNames = fields.Select(f => f.FieldName).ToList();
        var (whereSql, parameters) = BuildWhere(keyword, filters, fieldNames);

        await using var conn = new SqliteConnection(_connectionString);
        await conn.OpenAsync();

        // 查总数
        int total;
        await using (var cmd = conn.CreateCommand())
        {
            cmd.CommandText = $"SELECT COUNT(*) FROM \"{tableName}\"{whereSql}";
            AddParameters(cmd, parameters);
            total = Convert.ToInt32(await cmd.ExecuteScalarAsync() ?? 0);
        }

        // 查分页数据
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

    /// <summary>查询全部匹配数据（不分页，用于导出）</summary>
    public async Task<List<Dictionary<string, object?>>> QueryAllAsync(
        int menuId,
        string? keyword,
        List<FilterCondition>? filters,
        IEnumerable<FormField> fields)
    {
        var tableName = TableName(menuId);
        var fieldNames = fields.Select(f => f.FieldName).ToList();
        var (whereSql, parameters) = BuildWhere(keyword, filters, fieldNames);

        await using var conn = new SqliteConnection(_connectionString);
        await conn.OpenAsync();

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

    /// <summary>插入一条记录，返回新行 Id</summary>
    public async Task<long> InsertAsync(int menuId, Dictionary<string, string?> data)
    {
        var tableName = TableName(menuId);
        // 过滤掉系统列
        var safeData = data.Where(kv => !IsSystemColumn(kv.Key)).ToList();

        var cols = safeData.Select(kv => $"\"{kv.Key}\"").ToList();
        var paramNames = safeData.Select((_, i) => $"@v{i}").ToList();

        var sql = $"""
            INSERT INTO "{tableName}" ({string.Join(", ", cols)}, "UpdatedAt")
            VALUES ({string.Join(", ", paramNames)}, datetime('now','localtime'));
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
        name.Equals("UpdatedAt", StringComparison.OrdinalIgnoreCase);

    private static async Task<HashSet<string>> GetExistingColumnsAsync(SqliteConnection conn, string tableName)
    {
        var set = new HashSet<string>(StringComparer.OrdinalIgnoreCase);
        await using var cmd = conn.CreateCommand();
        cmd.CommandText = $"PRAGMA table_info(\"{tableName}\")";
        await using var reader = await cmd.ExecuteReaderAsync();
        while (await reader.ReadAsync())
            set.Add(reader.GetString(1)); // name 列
        return set;
    }

    private static (string Sql, Dictionary<string, object> Parameters) BuildWhere(
        string? keyword,
        List<FilterCondition>? filters,
        List<string> fieldNames)
    {
        var conditions = new List<string>();
        var parameters = new Dictionary<string, object>();
        int idx = 0;

        // 关键词：对所有字段做 LIKE 搜索
        if (!string.IsNullOrWhiteSpace(keyword) && fieldNames.Count > 0)
        {
            var clauses = fieldNames.Select(f => $"\"{f}\" LIKE @kw").ToList();
            conditions.Add($"({string.Join(" OR ", clauses)})");
            parameters["@kw"] = $"%{keyword}%";
        }

        // 字段级筛选
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
                else // eq
                {
                    conditions.Add($"\"{f.Field}\" = {pName}");
                    parameters[pName] = f.Value;
                }
            }
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
    /// <summary>字段名（FormField.FieldName）</summary>
    public string Field { get; set; } = string.Empty;

    /// <summary>操作符：eq（精确匹配）/ contains（模糊匹配）</summary>
    public string Op { get; set; } = "contains";

    /// <summary>筛选值</summary>
    public string Value { get; set; } = string.Empty;
}
