using QuestPDF.Fluent;
using QuestPDF.Helpers;
using QuestPDF.Infrastructure;

namespace LibShop.Services;

/// <summary>Экспорт отчёта в формат PDF через QuestPDF.</summary>
public static partial class ExportService
{
    private static void ExportToPdf(string filePath, string title,
        IReadOnlyList<string> headers, IReadOnlyList<IReadOnlyList<string>> rows)
    {
        QuestPDF.Settings.License = LicenseType.Community;

        // С версии QuestPDF 2026.9.0 по умолчанию используются только встроенные шрифты (Lato),
        // в которых нет знака рубля (₽, U+20BD) из заголовков отчёта — генерация падала бы
        // с DocumentDrawingException. Системные шрифты Windows (Segoe UI и др.) содержат и ₽, и кириллицу.
        QuestPDF.Settings.UseSystemFonts = true;

        Document.Create(container =>
        {
            container.Page(page =>
            {
                page.Size(PageSizes.A4.Landscape());
                page.Margin(20);
                page.Header().AlignCenter().Text(title).FontSize(16).Bold();
                page.Content().Table(table =>
                {
                    table.ColumnsDefinition(columns =>
                    {
                        foreach (string _ in headers)
                            columns.RelativeColumn(1);
                    });

                    table.Header(header =>
                    {
                        foreach (string h in headers)
                            header.Cell().Background(Colors.Grey.Lighten3)
                                .Padding(5).Text(h).Bold();
                    });

                    foreach (IReadOnlyList<string> row in rows)
                    {
                        foreach (string cell in row)
                            table.Cell().Padding(5).Text(cell);
                    }
                });

                page.Footer().AlignCenter().Text(text =>
                {
                    text.Span("Страница ");
                    text.CurrentPageNumber();
                    text.Span(" из ");
                    text.TotalPages();
                });
            });
        }).GeneratePdf(filePath);
    }
}
