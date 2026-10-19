using System.ComponentModel;
using System.Windows;
using LibShop.Models;
using LibShop.Pages;
using LibShop.Services;
using MaterialDesignThemes.Wpf;

namespace LibShop
{
    /// <summary>
    /// Главное окно приложения: шапка с динамическим заголовком и Frame со страницами.
    /// </summary>
    public partial class MainWindow : Window
    {
        /// <summary>Текущий вошедший пользователь (null — не авторизован).</summary>
        public User? CurrentUser { get; private set; }

        public MainWindow()
        {
            InitializeComponent();
            UpdateThemeIcon();
            HideUserControls();
            MainFrame.Navigate(new AuthPage(this));
        }

        /// <summary>Переход на страницу авторизации.</summary>
        public void NavigateToAuth()
        {
            CurrentUser = null;
            HideUserControls();
            SetPageTitle("Авторизация");
            MainFrame.Navigate(new AuthPage(this));
        }

        /// <summary>Успешный вход: переход на страницу по роли пользователя.</summary>
        public void OnLoginSuccess(User user)
        {
            CurrentUser = user;
            UserText.Text = $"{user.FullName} ({(user.Role == UserRole.Admin ? "Администратор" : "Пользователь")})";
            ChangePasswordButton.Visibility = LogoutButton.Visibility = Visibility.Visible;

            if (user.Role == UserRole.Admin)
            {
                SetPageTitle("Панель администратора");
                MainFrame.Navigate(new AdminPage(this));
            }
            else
            {
                SetPageTitle("Каталог книг");
                MainFrame.Navigate(new UserPage(this));
            }
        }

        /// <summary>Смена динамического заголовка страницы.</summary>
        public void SetPageTitle(string title) => PageTitleText.Text = title;

        private void HideUserControls()
        {
            UserText.Text = string.Empty;
            ChangePasswordButton.Visibility = Visibility.Collapsed;
            LogoutButton.Visibility = Visibility.Collapsed;
        }

        private void UpdateThemeIcon() =>
            ThemeIcon.Kind = ThemeService.CurrentTheme == BaseTheme.Light
                ? PackIconKind.WeatherNight
                : PackIconKind.WeatherSunny;

        private void ThemeButton_Click(object sender, RoutedEventArgs e)
        {
            ThemeService.Toggle();
            UpdateThemeIcon();
        }

        private void ChangePasswordButton_Click(object sender, RoutedEventArgs e)
        {
            if (CurrentUser == null)
                return;

            SetPageTitle("Смена пароля");
            MainFrame.Navigate(new ChangePasswordPage(this));
        }

        private void LogoutButton_Click(object sender, RoutedEventArgs e)
        {
            if (!MessageService.Confirm(
                    "Вы действительно хотите выйти из системы?", "Выход из системы"))
                return;

            // Сохранение выбранной темы при выходе пользователя из системы.
            ThemeService.Save();
            NavigateToAuth();
        }

        private void Window_Closing(object? sender, CancelEventArgs e)
        {
            if (!MessageService.Confirm(
                    "Вы действительно хотите выйти из приложения?", "Выход из приложения"))
            {
                e.Cancel = true;
                return;
            }

            // Сохранение выбранной темы при выходе из приложения.
            ThemeService.Save();
        }
    }
}