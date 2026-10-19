using System.Windows;
using System.Windows.Controls;
using System.Windows.Input;
using LibShop.Services;

namespace LibShop.Pages
{
    /// <summary>Страница авторизации с защитой от брутфорса (капча + блокировка).</summary>
    public partial class AuthPage : Page
    {
        private readonly MainWindow _window;
        private string _captchaCode = string.Empty;

        public AuthPage(MainWindow window)
        {
            _window = window;
            InitializeComponent();
        }

        private void Page_Loaded(object sender, RoutedEventArgs e) =>
            UpdateCaptchaVisibility();

        /// <summary>Показывает капчу, если количество неудачных попыток превысило порог.</summary>
        private void UpdateCaptchaVisibility()
        {
            string login = LoginTextBox.Text.Trim();
            bool required = login.Length > 0 && AuthService.RequiresCaptcha(login);

            CaptchaPanel.Visibility = required ? Visibility.Visible : Visibility.Collapsed;
            if (required && CaptchaImage.Source == null)
                RefreshCaptcha();
        }

        private void LoginTextBox_TextChanged(object sender, RoutedEventArgs e) =>
            UpdateCaptchaVisibility();

        private void RefreshCaptcha()
        {
            _captchaCode = CaptchaService.GenerateCode();
            CaptchaImage.Source = CaptchaService.RenderImage(_captchaCode);
            CaptchaTextBox.Clear();
        }

        private void RefreshCaptcha_Click(object sender, RoutedEventArgs e) =>
            RefreshCaptcha();

        private void Login_PreviewTextInput(object sender, TextCompositionEventArgs e) =>
            // Логин и код капчи: только латиница, цифры и «_».
            e.Handled = !System.Text.RegularExpressions.Regex.IsMatch(e.Text, "^[a-zA-Z0-9_]+$");

        private void PasswordBox_Changed(object sender, RoutedEventArgs e) =>
            UpdateCaptchaVisibility();

        private void LoginButton_Click(object sender, RoutedEventArgs e)
        {
            ErrorText.Text = string.Empty;

            string login = LoginTextBox.Text.Trim();
            string password = PasswordBox.Password;

            // --- Проверки ввода ---
            if (login.Length == 0)
            {
                ErrorText.Text = "Введите логин.";
                return;
            }
            if (password.Length == 0)
            {
                ErrorText.Text = "Введите пароль.";
                return;
            }

            // Проверка блокировки после многократных неудачных попыток.
            if (AuthService.GetLockoutRemaining(login) is { } remaining)
            {
                ErrorText.Text =
                    $"Слишком много неудачных попыток входа. " +
                    $"Повторите через {remaining.Minutes}:{remaining.Seconds:D2}.";
                return;
            }

            // Контроль капчи (после порога неудачных попыток).
            if (AuthService.RequiresCaptcha(login))
            {
                if (CaptchaImage.Source == null)
                    RefreshCaptcha();

                if (!CaptchaService.Verify(CaptchaTextBox.Text, _captchaCode))
                {
                    ErrorText.Text = "Капча введена неверно. Попробуйте ещё раз.";
                    RefreshCaptcha();
                    return;
                }
            }

            // --- Попытка входа ---
            (LoginResult result, var user) =
                AuthService.Login(login, password, CaptchaTextBox.Text, _captchaCode);

            switch (result)
            {
                case LoginResult.Success:
                    PasswordBox.Clear();
                    _window.OnLoginSuccess(user!);
                    break;

                case LoginResult.UserNotFound:
                case LoginResult.WrongPassword:
                    int attemptsLeft = Math.Max(0,
                        AuthService.LockoutThreshold - AuthService.GetFailCount(login));
                    ErrorText.Text = "Неверный логин или пароль." +
                        (attemptsLeft > 0
                            ? $" Осталось попыток до блокировки: {attemptsLeft}."
                            : " Вы заблокированы на 1 минуту.");
                    UpdateCaptchaVisibility();
                    break;

                case LoginResult.WrongCaptcha:
                    ErrorText.Text = "Капча введена неверно. Попробуйте ещё раз.";
                    RefreshCaptcha();
                    break;

                case LoginResult.CaptchaRequired:
                    ErrorText.Text = "Требуется ввод капчи.";
                    UpdateCaptchaVisibility();
                    break;

                case LoginResult.LockedOut:
                    ErrorText.Text = "Вход временно заблокирован. Повторите попытку позже.";
                    break;
            }
        }

        /// <summary>Переход на страницу регистрации.</summary>
        private void RegisterButton_Click(object sender, RoutedEventArgs e) =>
            NavigationService?.Navigate(new RegisterPage(_window));
    }
}