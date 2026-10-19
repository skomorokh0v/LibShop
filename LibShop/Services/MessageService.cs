using System.Windows;

namespace LibShop.Services;

/// <summary>Вывод сообщений пользователю (отдельный метод на каждый тип).</summary>
public static class MessageService
{
    private static Window? Owner => Application.Current.MainWindow;

    /// <summary>Информационное сообщение.</summary>
    public static void Info(string text, string title = "Информация") =>
        Show(text, title, MessageBoxImage.Information);

    /// <summary>Сообщение об ошибке.</summary>
    public static void Error(string text, string title = "Ошибка") =>
        Show(text, title, MessageBoxImage.Error);

    /// <summary>Предупреждение.</summary>
    public static void Warning(string text, string title = "Предупреждение") =>
        Show(text, title, MessageBoxImage.Warning);

    /// <summary>Запрос подтверждения действия (Да/Нет).</summary>
    public static bool Confirm(string text, string title = "Подтверждение") =>
        MessageBox.Show(Owner, text, title, MessageBoxButton.YesNo,
            MessageBoxImage.Question, MessageBoxResult.No) == MessageBoxResult.Yes;

    private static void Show(string text, string title, MessageBoxImage image) =>
        MessageBox.Show(Owner, text, title, MessageBoxButton.OK, image);
}
