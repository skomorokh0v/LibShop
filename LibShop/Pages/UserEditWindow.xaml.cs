using System.Text.RegularExpressions;
using System.Windows;
using System.Windows.Input;
using LibShop.Models;

namespace LibShop.Pages
{
    /// <summary>Окно добавления/редактирования пользователя.</summary>
    public partial class UserEditWindow : Window
    {
        private static readonly Regex LoginRegex = new("^[a-zA-Z0-9_]{3,20}$");
        private static readonly Regex PasswordRegex =
            new(@"^(?=.*[A-Za-zА-Яа-яЁё])(?=.*[0-9]).{6,}$");
        private static readonly Regex NameRegex = new(@"^[а-яёА-ЯЁa-zA-Z\- ]{2,100}$");
        private static readonly Regex EmailRegex = new(@"^[^@\s]+@[^@\s]+\.[^@\s]+$");
        private static readonly Regex PhoneRegex = new(@"^\+?[0-9]{10,15}$");

        private readonly bool _isEdit;

        /// <summary>Данные, введённые администратором (null — отмена).</summary>
        public string? Login { get; private set; }
        public string? Password { get; private set; }
        public string? FullName { get; private set; }
        public string? Email { get; private set; }
        public string? Phone { get; private set; }
        public UserRole Role { get; private set; } = UserRole.User;

        public UserEditWindow(User? user = null)
        {
            InitializeComponent();

            RoleComboBox.Items.Add("Пользователь");
            RoleComboBox.Items.Add("Администратор");
            RoleComboBox.SelectedIndex = 0;

            if (user != null)
            {
                _isEdit = true;
                TitleLabel.Text = "Редактирование пользователя";
                Title = "Редактирование пользователя";
                LoginTextBox.Text = user.Login;
                FullNameTextBox.Text = user.FullName;
                EmailTextBox.Text = user.Email ?? string.Empty;
                PhoneTextBox.Text = user.Phone ?? string.Empty;
                RoleComboBox.SelectedIndex = user.Role == UserRole.Admin ? 1 : 0;
            }
        }

        private void LettersDigits_PreviewTextInput(object sender, TextCompositionEventArgs e) =>
            e.Handled = !Regex.IsMatch(e.Text, "^[a-zA-Z0-9_]+$");

        private void Name_PreviewTextInput(object sender, TextCompositionEventArgs e) =>
            e.Handled = !Regex.IsMatch(e.Text, @"^[а-яёА-ЯЁa-zA-Z\- ]$");

        private void Phone_PreviewTextInput(object sender, TextCompositionEventArgs e) =>
            e.Handled = !Regex.IsMatch(e.Text, @"^[0-9+]$");

        private void SaveButton_Click(object sender, RoutedEventArgs e)
        {
            ErrorText.Text = string.Empty;

            string login = LoginTextBox.Text.Trim();
            string password = PasswordBox.Password;
            string fullName = FullNameTextBox.Text.Trim();
            string email = EmailTextBox.Text.Trim();
            string phone = PhoneTextBox.Text.Trim();

            if (!LoginRegex.IsMatch(login))
            {
                ErrorText.Text = "Логин: 3–20 символов, латиница, цифры и «_».";
                return;
            }
            // При редактировании пароль можно не менять.
            if (!_isEdit && !PasswordRegex.IsMatch(password))
            {
                ErrorText.Text = "Пароль: минимум 6 символов, содержит хотя бы одну букву и одну цифру.";
                return;
            }
            if (_isEdit && password.Length > 0 && !PasswordRegex.IsMatch(password))
            {
                ErrorText.Text = "Новый пароль: минимум 6 символов, содержит букву и цифру.";
                return;
            }
            if (!NameRegex.IsMatch(fullName))
            {
                ErrorText.Text = "Укажите фамилию и имя (буквы, пробел и дефен).";
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

            Login = login;
            Password = password;
            FullName = fullName;
            Email = email.Length == 0 ? null : email;
            Phone = phone.Length == 0 ? null : phone;
            Role = RoleComboBox.SelectedIndex == 1 ? UserRole.Admin : UserRole.User;

            DialogResult = true;
        }

        private void CancelButton_Click(object sender, RoutedEventArgs e) =>
            DialogResult = false;
    }
}