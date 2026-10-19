using System.Windows;
using LibShop.Data;
using LibShop.Services;

namespace LibShop
{
    /// <summary>
    /// Interaction logic for App.xaml
    /// </summary>
    public partial class App : Application
    {
        protected override void OnStartup(StartupEventArgs e)
        {
            base.OnStartup(e);

            // Восстановление сохранённой темы приложения.
            ThemeService.Load();

            // Создание базы данных при первом запуске и заполнение тестовыми данными.
            try
            {
                DbInitializer.Initialize();
            }
            catch (Exception ex)
            {
                MessageBox.Show(
                    "Не удалось подключиться к базе данных Microsoft SQL Server (HOME-PC).\n\n" +
                    $"Подробности: {ex.Message}",
                    "Ошибка подключения к БД",
                    MessageBoxButton.OK,
                    MessageBoxImage.Error);
                Shutdown(-1);
            }
        }
    }
}
