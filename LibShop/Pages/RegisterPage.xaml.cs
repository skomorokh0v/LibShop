using System.Text.RegularExpressions;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Input;
using LibShop.Services;

namespace LibShop.Pages
{
    /// <summary>Страница регистрации с проверкой вводимых данных.</summary>
    public partial class RegisterPage : Page
    {
        private readonly MainWindow _window;

        private static readonly Regex LoginRegex = new("^[a-zA-Z0-9_]{3,20}$");
        private static readonly Regex EmailRegex =
            new(@"^[^@\s]+@[^@\s]+\.[^@\s]+$");
        private static readonly Regex PhoneRegex = new(@"^\+?[0-9]{10,15}$");
        private static readonly Regex NameRegex = new(@"^[а-яёА-ЯЁa-zA-Z\- ]{2,100}$");
        private static readonly Regex PasswordRegex = new(@"^(?=.*[A-Za-zА-Яа-яЁё])(?=.*[0-9]).{6,}$");

        public RegisterPage(MainWindow window)
        {
            _window = window;
            InitializeComponent();
        }

        // --- Ограничения ввода (маски) ---

        private void LettersDigits_PreviewTextInput(object sender, TextCompositionEventArgs e) =>
            e.Handled = !Regex.IsMatch(e.Text, "^[a-zA-Z0-9_]+$");

        private void Name_PreviewTextInput(object sender, TextCompositionEventArgs e) =>
            e.Handled = !Regex.IsMatch(e.Text, @"^[а-яёА-ЯЁa-zA-Z\- ]$");

        private void Phone_PreviewTextInput(object sender, TextCompositionEventArgs e) =>
            e.Handled = !Regex.IsMatch(e.Text, @"^[0-9+]$");

        private void RegisterButton_Click(object sender, RoutedEventArgs e)
        {
            ErrorText.Text = string.Empty;

            string login = LoginTextBox.Text.Trim();
            string password = PasswordBox.Password;
            string confirm = ConfirmPasswordBox.Password;
            string fullName = FullNameTextBox.Text.Trim();
            string email = EmailTextBox.Text.Trim();
            string phone = PhoneTextBox.Text.Trim();

            // --- Проверка данных ---
            if (!LoginRegex.IsMatch(login))
            {
                ErrorText.Text = "Логин: 3–20 символов, латиница, цифры и «_».";
                return;
            }
            if (!PasswordRegex.IsMatch(password))
            {
                ErrorText.Text = "Пароль: минимум 6 символов, содержит хотя бы одну букву и одну цифру.";
                return;
            }
            if (password != confirm)
            {
                ErrorText.Text = "Пароли не совпадают.";
                return;
            }
            if (!NameRegex.IsMatch(fullName))
            {
                ErrorText.Text = "Укажите фамилию и имя (буквы, пробел и дефис, 2–100 символов).";
                return;
            }
            if (email.Length > 0 && !EmailRegex.IsMatch(email))
            {
                ErrorText.Text = "Электронная почта указана в неверном формате.";
                return;
            }
            if (phone.Length > 0 && !PhoneRegex.IsMatch(phone))
            {
                ErrorText.Text = "Телефон: только цифры и знак «+», 10–15 цифр.";
                return;
            }

            // --- Регистрация ---
            try
            {
                string? error = AuthService.Register(login, password, fullName,
                    email.Length == 0 ? null : email,
                    phone.Length == 0 ? null : phone);

                if (error != null)
                {
                    ErrorText.Text = error;
                    return;
                }

                MessageService.Info(
                    $"Пользователь \"{login}\" успешно зарегистрирован. Теперь вы можете войти.",
                    "Регистрация завершена");

                _window.NavigateToAuth();
            }
            catch (Exception ex)
            {
                MessageService.Error($"Ошибка при регистрации:\n{ex.Message}");
            }
        }

        private void BackButton_Click(object sender, RoutedEventArgs e) =>
            _window.NavigateToAuth();
    }
}