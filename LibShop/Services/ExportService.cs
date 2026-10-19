namespace LibShop.Services;

/// <summary>Экспорт отчётов в .xlsx, .docx и .pdf (реализации — в partial-файлах).</summary>
public static partial class ExportService
{
    /// <summary>Сохраняет таблицу данных в файл; формат — по расширению файла.</summary>
    public static void Export(string filePath, string reportTitle,
        IReadOnlyList<string> headers, IReadOnlyList<IReadOnlyList<string>> rows)
    {
        switch (System.IO.Path.GetExtension(filePath).ToLowerInvariant())
        {
            case ".xlsx":
                ExportToXlsx(filePath, reportTitle, headers, rows);
                break;
            case ".docx":
                ExportToDocx(filePath, reportTitle, headers, rows);
                break;
            case ".pdf":
                ExportToPdf(filePath, reportTitle, headers, rows);
                break;
            default:
                throw new NotSupportedException(
                    $"Формат \"{System.IO.Path.GetExtension(filePath)}\" не поддерживается.");
        }
    }

    /// <summary>Диалог сохранения отчёта (xlsx/docx/pdf). Возвращает false при отмене.</summary>
    public static bool ExportWithDialog(string reportTitle,
        IReadOnlyList<string> headers, IReadOnlyList<IReadOnlyList<string>> rows)
    {
        var dialog = new Microsoft.Win32.SaveFileDialog
        {
            Title = "Сохранить отчёт",
            Filter = "Таблица Excel (*.xlsx)|*.xlsx|Документ Word (*.docx)|*.docx|Документ PDF (*.pdf)|*.pdf",
            FileName = reportTitle,
            DefaultExt = ".xlsx"
        };

        if (dialog.ShowDialog() != true)
            return false;

        try
        {
            Export(dialog.FileName, reportTitle, headers, rows);
            MessageService.Info($"Отчёт сохранён:\n{dialog.FileName}");
            return true;
        }
        catch (Exception ex)
        {
            MessageService.Error($"Не удалось сохранить отчёт:\n{ex.Message}");
            return false;
        }
    }
}
