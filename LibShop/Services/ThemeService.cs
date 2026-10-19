using System.IO;
using System.Windows;
using MaterialDesignThemes.Wpf;

namespace LibShop.Services;

/// <summary>Переключение темы приложения (светлая/тёмная) с сохранением выбора.</summary>
public static class ThemeService
{
    private static string SettingsPath
    {
        get
        {
            string dir = Path.Combine(
                Environment.GetFolderPath(Environment.SpecialFolder.ApplicationData), "LibShop");
            Directory.CreateDirectory(dir);
            return Path.Combine(dir, "theme.txt");
        }
    }

    /// <summary>Текущая тема.</summary>
    public static BaseTheme CurrentTheme { get; private set; } = BaseTheme.Light;

    /// <summary>Применяет тему к ресурсам приложения и сохраняет выбор в файл.</summary>
    public static void Apply(BaseTheme theme, bool persist = true)
    {
        CurrentTheme = theme;

        if (Application.Current.Resources.MergedDictionaries
                .OfType<BundledTheme>().FirstOrDefault() is { } bundled)
        {
            bundled.BaseTheme = theme;
        }

        if (persist)
            Save();
    }

    /// <summary>Переключает на противоположную тему.</summary>
    public static void Toggle() =>
        Apply(CurrentTheme == BaseTheme.Light ? BaseTheme.Dark : BaseTheme.Light);

    /// <summary>Сохраняет выбранную тему (вызывается и при выходе из системы).</summary>
    public static void Save()
    {
        try
        {
            File.WriteAllText(SettingsPath, CurrentTheme.ToString());
        }
        catch
        {
            // Файл настроек может быть недоступен — смена темы не должна падать.
        }
    }

    /// <summary>Загружает сохранённую тему при запуске приложения.</summary>
    public static void Load()
    {
        try
        {
            if (File.Exists(SettingsPath) &&
                Enum.TryParse(File.ReadAllText(SettingsPath).Trim(), out BaseTheme theme))
            {
                Apply(theme, persist: false);
            }
        }
        catch
        {
            // Остаться на светлой теме по умолчанию.
        }
    }
}
