using DocumentFormat.OpenXml;
using DocumentFormat.OpenXml.Packaging;
using DocumentFormat.OpenXml.Spreadsheet;

namespace LibShop.Services;

/// <summary>Экспорт отчёта в формат Excel (.xlsx) через OpenXML SDK.</summary>
public static partial class ExportService
{
    private static void ExportToXlsx(string filePath, string title,
        IReadOnlyList<string> headers, IReadOnlyList<IReadOnlyList<string>> rows)
    {
        using SpreadsheetDocument document = SpreadsheetDocument.Create(
            filePath, SpreadsheetDocumentType.Workbook);

        WorkbookPart workbookPart = document.AddWorkbookPart();
        workbookPart.Workbook = new Workbook();

        WorksheetPart worksheetPart = workbookPart.AddNewPart<WorksheetPart>();
        var sheetData = new SheetData();
        worksheetPart.Worksheet = new Worksheet(sheetData);

        WorkbookStylesPart stylesPart = workbookPart.AddNewPart<WorkbookStylesPart>();
        stylesPart.Stylesheet = CreateXlsxStyles();
        stylesPart.Stylesheet.Save();

        workbookPart.Workbook.AppendChild(new Sheets()).AppendChild(new Sheet
        {
            Id = workbookPart.GetIdOfPart(worksheetPart),
            SheetId = 1,
            Name = "Отчёт"
        });

        // Заголовок отчёта (строка 1).
        var titleRow = new Row { RowIndex = 1 };
        titleRow.AppendChild(new Cell
        {
            CellReference = "A1",
            DataType = CellValues.InlineString,
            InlineString = new InlineString(new Text(title)),
            StyleIndex = 1
        });
        sheetData.AppendChild(titleRow);

        // Шапка таблицы (строка 3).
        var headerRow = new Row { RowIndex = 3 };
        for (int i = 0; i < headers.Count; i++)
        {
            headerRow.AppendChild(new Cell
            {
                CellReference = GetCellReference(i, 3),
                DataType = CellValues.InlineString,
                InlineString = new InlineString(new Text(headers[i])),
                StyleIndex = 2
            });
        }
        sheetData.AppendChild(headerRow);

        // Данные (начиная со строки 4).
        for (int r = 0; r < rows.Count; r++)
        {
            int rowIndex = r + 4;
            var row = new Row { RowIndex = (uint)rowIndex };
            for (int c = 0; c < rows[r].Count; c++)
            {
                row.AppendChild(new Cell
                {
                    CellReference = GetCellReference(c, rowIndex),
                    DataType = CellValues.InlineString,
                    InlineString = new InlineString(new Text(rows[r][c]))
                });
            }
            sheetData.AppendChild(row);
        }

        // Ширины колонок.
        var columnDefinitions = new Columns();
        for (int i = 0; i < headers.Count; i++)
        {
            columnDefinitions.AppendChild(new Column
            {
                Min = (uint)(i + 1),
                Max = (uint)(i + 1),
                Width = 20,
                CustomWidth = true
            });
        }
        worksheetPart.Worksheet.InsertBefore(columnDefinitions, sheetData);

        workbookPart.Workbook.Save();
    }

    private static Stylesheet CreateXlsxStyles()
    {
        var boldFont = new Font(
            new Bold(), new FontSize { Val = 12 }, new FontName { Val = "Calibri" });
        var normalFont = new Font(
            new FontSize { Val = 11 }, new FontName { Val = "Calibri" });

        return new Stylesheet(
            new Fonts(boldFont, normalFont),
            new Fills(
                new Fill(new PatternFill { PatternType = PatternValues.None }),
                new Fill(new PatternFill { PatternType = PatternValues.Gray125 })),
            new Borders(new Border()),
            new CellStyleFormats(new CellFormat()),
            new CellFormats(
                new CellFormat(),
                new CellFormat { FontId = 0, ApplyFont = true },
                new CellFormat { FontId = 1, ApplyFont = true }));
    }

    private static string GetCellReference(int column, int row)
    {
        string name = string.Empty;
        int c = column;
        do
        {
            name = (char)('A' + c % 26) + name;
            c = c / 26 - 1;
        } while (c >= 0);
        return $"{name}{row}";
    }
}
