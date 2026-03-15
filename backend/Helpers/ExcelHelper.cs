using AllIn.LowCodeKit.Backend.Models;
using NPOI.SS.UserModel;
using NPOI.SS.Util;
using NPOI.XSSF.UserModel;

namespace AllIn.LowCodeKit.Backend.Helpers;

/// <summary>
/// Excel 辅助类：生成导入模板、导出数据、解析导入文件
/// </summary>
public static class ExcelHelper
{
    /// <summary>
    /// 根据表单字段生成 Excel 导入模板：
    /// - 必填列黄色背景
    /// - 下拉框列生成数据验证
    /// - 批注显示在列头
    /// </summary>
    public static byte[] GenerateTemplate(IEnumerable<FormField> fields)
    {
        var wb = new XSSFWorkbook();
        var sheet = wb.CreateSheet("导入数据");
        var drawing = (XSSFDrawing)sheet.CreateDrawingPatriarch();

        // 必填列样式：黄色背景 + 粗体
        var requiredStyle = wb.CreateCellStyle();
        requiredStyle.FillForegroundColor = IndexedColors.Yellow.Index;
        requiredStyle.FillPattern = FillPattern.SolidForeground;
        var boldFont = wb.CreateFont();
        boldFont.IsBold = true;
        requiredStyle.SetFont(boldFont);

        var fieldList = fields.OrderBy(f => f.ColumnOrder).ToList();
        var headerRow = sheet.CreateRow(0);

        for (int i = 0; i < fieldList.Count; i++)
        {
            var field = fieldList[i];
            var cell = headerRow.CreateCell(i);
            cell.SetCellValue(field.Label);
            sheet.SetColumnWidth(i, 22 * 256);

            if (field.IsRequired)
                cell.CellStyle = requiredStyle;

            // 批注（Remark → 列头注释）
            if (!string.IsNullOrWhiteSpace(field.Remark))
            {
                var anchor = (XSSFClientAnchor)wb.GetCreationHelper().CreateClientAnchor();
                anchor.Col1 = i; anchor.Row1 = 0;
                anchor.Col2 = i + 4; anchor.Row2 = 4;
                var comment = drawing.CreateCellComment(anchor);
                comment.String = wb.GetCreationHelper().CreateRichTextString(field.Remark);
                comment.Author = "System";
                cell.CellComment = comment;
            }

            // 下拉验证（Select 类型）
            if (field.FieldType == "Select" && !string.IsNullOrWhiteSpace(field.Options))
            {
                try
                {
                    var options = System.Text.Json.JsonSerializer.Deserialize<string[]>(field.Options);
                    if (options?.Length > 0)
                    {
                        var xssfSheet = (NPOI.XSSF.UserModel.XSSFSheet)sheet;
                        var dvHelper = new NPOI.XSSF.UserModel.XSSFDataValidationHelper(xssfSheet);
                        var constraint = dvHelper.CreateExplicitListConstraint(options);
                        var addressList = new CellRangeAddressList(1, 10000, i, i);
                        var validation = dvHelper.CreateValidation(constraint, addressList);
                        validation.ShowErrorBox = true;
                        validation.CreateErrorBox("输入错误", $"请从下拉列表中选择「{field.Label}」的值");
                        sheet.AddValidationData(validation);
                    }
                }
                catch { /* 选项解析失败时跳过验证 */ }
            }
        }

        using var ms = new MemoryStream();
        wb.Write(ms, leaveOpen: true);
        return ms.ToArray();
    }

    /// <summary>
    /// 将数据行导出为 Excel 文件，仅包含用户选中的列
    /// </summary>
    public static byte[] ExportData(
        IEnumerable<FormField> fields,
        IEnumerable<Dictionary<string, object?>> rows,
        IEnumerable<string>? selectedColumns = null)
    {
        var wb = new XSSFWorkbook();
        var sheet = wb.CreateSheet("导出数据");

        // 列头样式
        var headerStyle = wb.CreateCellStyle();
        headerStyle.FillForegroundColor = IndexedColors.Grey25Percent.Index;
        headerStyle.FillPattern = FillPattern.SolidForeground;
        var boldFont = wb.CreateFont();
        boldFont.IsBold = true;
        headerStyle.SetFont(boldFont);

        // 若未指定导出列，则导出全部
        var allFields = fields.OrderBy(f => f.ColumnOrder).ToList();
        var exportFields = selectedColumns?.Any() == true
            ? allFields.Where(f => selectedColumns.Contains(f.FieldName)).ToList()
            : allFields;

        // 写列头
        var headerRow = sheet.CreateRow(0);
        for (int i = 0; i < exportFields.Count; i++)
        {
            var cell = headerRow.CreateCell(i);
            cell.SetCellValue(exportFields[i].Label);
            cell.CellStyle = headerStyle;
            sheet.SetColumnWidth(i, 22 * 256);
        }

        // 写数据行
        int rowIdx = 1;
        foreach (var row in rows)
        {
            var excelRow = sheet.CreateRow(rowIdx++);
            for (int i = 0; i < exportFields.Count; i++)
            {
                var v = row.TryGetValue(exportFields[i].FieldName, out var val) ? val?.ToString() : null;
                excelRow.CreateCell(i).SetCellValue(v ?? string.Empty);
            }
        }

        using var ms = new MemoryStream();
        wb.Write(ms, leaveOpen: true);
        return ms.ToArray();
    }

    /// <summary>
    /// 解析上传的 Excel 文件，按列头标签映射到字段名，返回数据行列表
    /// </summary>
    public static List<Dictionary<string, string?>> ParseImportData(
        Stream stream, IEnumerable<FormField> fields)
    {
        var wb = WorkbookFactory.Create(stream);
        var sheet = wb.GetSheetAt(0);
        if (sheet == null) return [];

        var headerRow = sheet.GetRow(0);
        if (headerRow == null) return [];

        var fieldList = fields.OrderBy(f => f.ColumnOrder).ToList();

        // 按列头标签映射列索引 → 字段名
        var columnMap = new Dictionary<int, string>();
        for (int i = 0; i < headerRow.LastCellNum; i++)
        {
            var label = headerRow.GetCell(i)?.StringCellValue?.Trim();
            if (string.IsNullOrEmpty(label)) continue;
            var field = fieldList.FirstOrDefault(f => f.Label == label);
            if (field != null) columnMap[i] = field.FieldName;
        }

        var result = new List<Dictionary<string, string?>>();
        for (int r = 1; r <= sheet.LastRowNum; r++)
        {
            var row = sheet.GetRow(r);
            if (row == null) continue;

            var data = new Dictionary<string, string?>();
            bool hasValue = false;

            foreach (var (colIdx, fieldName) in columnMap)
            {
                var v = GetCellValue(row.GetCell(colIdx));
                data[fieldName] = v;
                if (!string.IsNullOrWhiteSpace(v)) hasValue = true;
            }

            if (hasValue) result.Add(data);
        }

        return result;
    }

    private static string? GetCellValue(ICell? cell)
    {
        if (cell == null || cell.CellType == CellType.Blank) return null;
        return cell.CellType switch
        {
            CellType.String => cell.StringCellValue?.Trim(),
            CellType.Numeric => DateUtil.IsCellDateFormatted(cell)
                ? cell.DateCellValue?.ToString("yyyy-MM-dd")
                : cell.NumericCellValue.ToString("G"),
            CellType.Boolean => cell.BooleanCellValue.ToString(),
            CellType.Formula => cell.CachedFormulaResultType == CellType.Numeric
                ? cell.NumericCellValue.ToString("G")
                : cell.StringCellValue?.Trim(),
            _ => null
        };
    }
}
