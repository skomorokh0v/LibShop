using DocumentFormat.OpenXml;
using DocumentFormat.OpenXml.Packaging;
using DocumentFormat.OpenXml.Wordprocessing;

namespace LibShop.Services;

/// <summary>Экспорт отчёта в формат Word (.docx) через OpenXML SDK.</summary>
public static partial class ExportService
{
    private static void ExportToDocx(string filePath, string title,
        IReadOnlyList<string> headers, IReadOnlyList<IReadOnlyList<string>> rows)
    {
        using var document = WordprocessingDocument.Create(
            filePath, WordprocessingDocumentType.Document);

        var mainPart = document.AddMainDocumentPart();
        mainPart.Document = new DocumentFormat.OpenXml.Wordprocessing.Document(
            new Body());
        Body body = mainPart.Document.Body!;

        body.AppendChild(new Paragraph(
            new Run(new Text(title)))
        {
            ParagraphProperties = new ParagraphProperties(
                new ParagraphStyleId { Val = "Title" },
                new RunProperties(new Bold(), new FontSize { Val = "32" }))
        });

        body.AppendChild(new Paragraph(new Run(new Text(
            $"Сформирован: {DateTime.Now:dd.MM.yyyy HH:mm}"))));

        var table = new Table(
            new TableProperties(
                new TableBorders(
                    new TopBorder { Val = BorderValues.Single },
                    new BottomBorder { Val = BorderValues.Single },
                    new LeftBorder { Val = BorderValues.Single },
                    new RightBorder { Val = BorderValues.Single },
                    new InsideHorizontalBorder { Val = BorderValues.Single },
                    new InsideVerticalBorder { Val = BorderValues.Single }),
                new TableWidth { Width = "5000", Type = TableWidthUnitValues.Pct }));

        table.AppendChild(new TableRow(
            headers.Select(h => new TableCell(
                new Paragraph(new Run(
                    new Text(h),
                    new RunProperties(new Bold())))))));

        foreach (IReadOnlyList<string> row in rows)
        {
            table.AppendChild(new TableRow(
                row.Select(cell => new TableCell(
                    new Paragraph(new Run(new Text(cell)))))));
        }

        body.AppendChild(table);
        mainPart.Document.Save();
    }
}
