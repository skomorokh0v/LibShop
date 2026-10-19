using System.Text.RegularExpressions;
using System.Windows;
using System.Windows.Controls;
using LibShop.Services;

namespace LibShop.Pages
{
    /// <summary>Страница смены пароля текущего пользователя.</summary>
    public partial class ChangePasswordPage : Page
    {
        private readonly MainWindow _window;

        private static readonly Regex PasswordRegex =
            new(@"^(?=.*[A-Za-zА-Яа-яЁё])(?=.*[0-9]).{6,}$");

        public ChangePasswordPage(MainWindow window)
        {
            _window = window;
            InitializeComponent();
        }

        private void ChangeButton_Click(object sender, RoutedEventArgs e)
        {
            ErrorText.Text = string.Empty;

            string current = CurrentPasswordBox.Password;
            string next = NewPasswordBox.Password;
            string confirm = ConfirmPasswordBox.Password;

            if (current.Length == 0)
            {
                ErrorText.Text = "Введите текущий пароль.";
                return;
            }
            if (!PasswordRegex.IsMatch(next))
            {
                ErrorText.Text = "Новый пароль: минимум 6 символов, содержит хотя бы одну букву и одну цифру.";
                return;
            }
            if (next != confirm)
            {
                ErrorText.Text = "Новые пароли не совпадают.";
                return;
            }
            if (next == current)
            {
                ErrorText.Text = "Новый пароль должен отличаться от текущего.";
                return;
            }

            try
            {
                string? error = AuthService.ChangePassword(
                    _window.CurrentUser!, current, next);

                if (error != null)
                {
                    ErrorText.Text = error;
                    return;
                }

                MessageService.Info("Пароль успешно изменён.", "Смена пароля");
                BackButton_Click(sender, e);
            }
            catch (Exception ex)
            {
                MessageService.Error($"Не удалось сменить пароль:\n{ex.Message}");
            }
        }

        private void BackButton_Click(object sender, RoutedEventArgs e)
        {
            // Возврат на страницу по роли пользователя.
            if (_window.CurrentUser != null)
                _window.OnLoginSuccess(_window.CurrentUser);
        }
    }
}